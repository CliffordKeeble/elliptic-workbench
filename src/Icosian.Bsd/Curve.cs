using System.Numerics;

namespace Icosian.Bsd;

/// <summary>
/// A minimal Weierstrass model y² + a₁xy + a₃y = x³ + a₂x² + a₄x + a₆ over ℚ, with its exact
/// integer invariants and the local (per-prime) arithmetic of the BSD compiler:
/// point counting (lexical analysis), the Hecke rebuild (parsing), Tamagawa numbers
/// (exception handling, semistable only in v1), a torsion upper bound, and the root number.
///
/// v1 preconditions, enforced or documented:
///  - the model must be minimal (a necessary u-test is run: no prime with p¹²|Δ, p⁴|c₄, p⁶|c₆);
///  - all bad reduction must be multiplicative (enforced: ord_p(c₄) = 0 at every bad prime);
///  - the conductor and its prime factorisation are supplied by the caller and checked against Δ.
/// Papers: 141 (rank-0 grammar), 144 (|Sha| = 4), 145 (consolidation).
/// </summary>
public sealed class EllipticCurve
{
    public readonly BigInteger A1, A2, A3, A4, A6;
    public readonly BigInteger B2, B4, B6, B8, C4, C6, Delta;
    public readonly long Conductor;
    public readonly long[] BadPrimes;

    public EllipticCurve(BigInteger a1, BigInteger a2, BigInteger a3, BigInteger a4, BigInteger a6,
                         long conductor, long[] badPrimes)
    {
        (A1, A2, A3, A4, A6) = (a1, a2, a3, a4, a6);
        B2 = a1 * a1 + 4 * a2;
        B4 = a1 * a3 + 2 * a4;
        B6 = a3 * a3 + 4 * a6;
        B8 = a1 * a1 * a6 + 4 * a2 * a6 - a1 * a3 * a4 + a2 * a3 * a3 - a4 * a4;
        C4 = B2 * B2 - 24 * B4;
        C6 = -B2 * B2 * B2 + 36 * B2 * B4 - 216 * B6;
        Delta = -B2 * B2 * B8 - 8 * B4 * B4 * B4 - 27 * B6 * B6 + 9 * B2 * B4 * B6;

        if (Delta.IsZero) throw new ArgumentException("Singular curve: Δ = 0.");
        Conductor = conductor;
        BadPrimes = badPrimes;

        foreach (var p in badPrimes)
        {
            if (Delta % p != 0)
                throw new ArgumentException($"Prime {p} declared bad but does not divide Δ.");
            if (C4 % p == 0)
                throw new NotSupportedException(
                    $"Additive reduction at p = {p} (ord_p(c₄) > 0): full Tate algorithm is v3 scope.");
            // Necessary (not sufficient) minimality test at this prime.
            if (Ord(Delta, p) >= 12 && Ord(C4, p) >= 4 && Ord(C6, p) >= 6)
                throw new NotSupportedException($"Model appears non-minimal at p = {p} (u¹²-test).");
        }
    }

    public static int Ord(BigInteger n, long p)
    {
        n = BigInteger.Abs(n);
        int k = 0;
        while (!n.IsZero && n % p == 0) { n /= p; k++; }
        return k;
    }

    // ── Stage 1: point counting ──────────────────────────────────────────────

    /// <summary>
    /// a_p = p + 1 − #E(𝔽_p) by direct counting. Valid as the L-series coefficient for good
    /// primes and, via the naive count that includes the singular point, for multiplicative
    /// and additive reduction as well (Paper 144 review, Turn 2).
    /// </summary>
    public long Ap(long p)
    {
        if (p == 2)
        {
            long count = 1; // point at infinity
            for (long x = 0; x < 2; x++)
                for (long y = 0; y < 2; y++)
                {
                    var lhs = (y * y + (long)Mod(A1, 2) * x * y + (long)Mod(A3, 2) * y) % 2;
                    var rhs = (x * x * x + (long)Mod(A2, 2) * x * x + (long)Mod(A4, 2) * x + (long)Mod(A6, 2)) % 2;
                    if (lhs == rhs) count++;
                }
            return 2 + 1 - count;
        }

        long a1 = Mod(A1, p), a2 = Mod(A2, p), a3 = Mod(A3, p), a4 = Mod(A4, p), a6 = Mod(A6, p);
        long half = (p - 1) / 2;
        long total = 0;
        for (long x = 0; x < p; x++)
        {
            long b = (a1 * x + a3) % p;
            long f = (((x * x % p) * x % p) + a2 * x % p * x % p + a4 * x % p + a6) % p;
            long d = (b * b % p + 4 * f % p) % p;
            if (d != 0)
            {
                long ls = PowMod(d, half, p);
                total += ls == 1 ? 1 : -1;
            }
        }
        return -total;
    }

    private static long Mod(BigInteger a, long p)
    {
        var r = (long)(a % p);
        return r < 0 ? r + p : r;
    }

    private static long PowMod(long b, long e, long m)
    {
        // m < 2^31 in v1 usage, so 128-bit-safe via ulong products.
        ulong result = 1, bb = (ulong)(b % m);
        ulong mm = (ulong)m;
        while (e > 0)
        {
            if ((e & 1) == 1) result = result * bb % mm;
            bb = bb * bb % mm;
            e >>= 1;
        }
        return (long)result;
    }

    public static long[] Primes(long limit)
    {
        var sieve = new bool[limit + 1];
        var list = new List<long>();
        for (long i = 2; i <= limit; i++)
        {
            if (!sieve[i])
            {
                list.Add(i);
                for (long j = i * i; j <= limit; j += i) sieve[j] = true;
            }
        }
        return list.ToArray();
    }

    // ── Stage 1½: the Hecke rebuild (parsing) ────────────────────────────────

    /// <summary>Full multiplicative sequence a_n for n ≤ bound, rebuilt from a_p via Hecke recursion.</summary>
    public long[] An(int bound, out Dictionary<long, long> ap)
    {
        var primes = Primes(bound);
        ap = new Dictionary<long, long>();
        foreach (var p in primes) ap[p] = Ap(p);
        // v1.0.1 (Finding 1, Mr Code adversarial review 3 Jul 2026): conductor primes can
        // exceed the series bound (e.g. N = 233 at digits = 30 gives nmax = 232), and the
        // Tamagawa/root-number stages index them unconditionally. Their prime powers never
        // enter the truncated series, so seeding the dictionary is safe and complete.
        foreach (var p in BadPrimes)
            if (!ap.ContainsKey(p)) ap[p] = Ap(p);

        var badSet = new HashSet<long>(BadPrimes);
        var an = new long[bound + 1];
        an[1] = 1;
        foreach (var p in primes)
        {
            an[p] = ap[p];
            long pk = p;
            while (pk * p <= bound)
            {
                long prev = pk;
                pk *= p;
                an[pk] = badSet.Contains(p)
                    ? ap[p] * an[prev]
                    : ap[p] * an[prev] - p * an[prev / p];
            }
        }
        for (long n = 2; n <= bound; n++)
        {
            if (an[n] != 0) continue;
            long t = n, val = 1;
            foreach (var p in primes)
            {
                if (p * p > t) break;
                if (t % p == 0)
                {
                    long pk = 1;
                    while (t % p == 0) { t /= p; pk *= p; }
                    val *= an[pk];
                }
            }
            if (t > 1) val *= an[t];
            an[n] = val;
        }
        return an;
    }

    // ── Local invariants ─────────────────────────────────────────────────────

    /// <summary>∏c_p for semistable curves: split I_n gives n; non-split gives 1 (n odd) or 2 (n even).</summary>
    public long TamagawaProduct(Dictionary<long, long> ap)
    {
        long prod = 1;
        foreach (var p in BadPrimes)
        {
            int n = Ord(Delta, p);
            bool split = ap[p] == 1;
            prod *= split ? n : (n % 2 == 1 ? 1 : 2);
        }
        return prod;
    }

    /// <summary>Upper bound on |E(ℚ)_tors| via gcd of #E(𝔽_p) over small good odd primes.</summary>
    public long TorsionBound()
    {
        long g = 0;
        foreach (var p in new long[] { 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 })
        {
            if (Delta % p == 0) continue;
            long np = p + 1 - Ap(p);
            g = g == 0 ? np : Gcd(g, np);
        }
        return g;
    }

    private static long Gcd(long a, long b) { while (b != 0) (a, b) = (b, a % b); return a; }

    /// <summary>Global root number for semistable curves: ε = w_∞ · ∏ w_p = (−1) · ∏(−a_p) over bad primes.</summary>
    public int RootNumber(Dictionary<long, long> ap)
    {
        long eps = -1;
        foreach (var p in BadPrimes) eps *= -ap[p];
        return (int)eps;
    }
}
