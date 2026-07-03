using System.Numerics;
using Icosian.Bsd;
using Icosian.Numerics;
using Xunit;

// BigFloat.Precision is a process-global static; keep the suite single-threaded.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Icosian.Bsd.Tests;

// Direct port of the v1.0.2 Icosian.Bsd console battery into xUnit. Component-level
// rules, distinct from the end-to-end acceptance suite: a_p vs an in-process brute
// counter (cross-method inside C#), Hecke identities from the recursion's own output,
// every constructor guard with a witness curve, and the open Finding 2 state asserted
// explicitly (tagged [Trait("Finding","2")]) so v1.1 flips it deliberately.
public class BsdTests
{
    public BsdTests() => BigFloat.Precision = 320;

    static readonly EllipticCurve E11 = new(0, -1, 1, -10, -20, 11, new long[] { 11 });
    static readonly EllipticCurve E30 = new(1, 0, 1, 1, 2, 30, new long[] { 2, 3, 5 });
    static readonly EllipticCurve E37 = new(0, 0, 1, -1, 0, 37, new long[] { 37 });
    static readonly EllipticCurve E233 = new(1, 3, 0, -1, 0, 233, new long[] { 233 });
    static readonly EllipticCurve E27606 = new(1, 0, 0, -10289707, 12703497719, 27606, new long[] { 2, 3, 43, 107 });

    static long M(BigInteger a, long p) { var r = (long)(a % p); return r < 0 ? r + p : r; }

    // Naive O(p^2) point count sharing no code with EllipticCurve.Ap.
    static long BruteAp(EllipticCurve e, long p)
    {
        long a1 = M(e.A1, p), a2 = M(e.A2, p), a3 = M(e.A3, p), a4 = M(e.A4, p), a6 = M(e.A6, p);
        long count = 1; // infinity
        for (long x = 0; x < p; x++)
            for (long y = 0; y < p; y++)
            {
                long lhs = (y * y % p + a1 * x % p * y % p + a3 * y % p) % p;
                long rhs = (x * x % p * x % p + a2 * x % p * x % p + a4 * x % p + a6) % p;
                if (lhs == rhs) count++;
            }
        return p + 1 - count;
    }

    static void AssertApMatchesBrute(EllipticCurve e)
    {
        foreach (var p in EllipticCurve.Primes(60))
            Assert.Equal(BruteAp(e, p), e.Ap(p));
    }

    // ── 1. a_p against brute force (includes bad primes) ──────────────────────
    [Fact] public void Ap_11a1_MatchesBrute() => AssertApMatchesBrute(E11);
    [Fact] public void Ap_30a1_MatchesBrute() => AssertApMatchesBrute(E30);
    [Fact] public void Ap_27606c1_MatchesBrute() => AssertApMatchesBrute(E27606);

    // ── 2. Hecke rebuild obeys its own contract ───────────────────────────────
    [Fact] public void Hecke_Mult_a6() { var an = E11.An(200, out _); Assert.Equal(an[2] * an[3], an[6]); }
    [Fact] public void Hecke_Mult_a15() { var an = E11.An(200, out _); Assert.Equal(an[3] * an[5], an[15]); }
    [Fact] public void Hecke_Mult_a77() { var an = E11.An(200, out _); Assert.Equal(an[7] * an[11], an[77]); }
    [Fact] public void Hecke_GoodRecursion_a4() { var an = E11.An(200, out _); Assert.Equal(an[2] * an[2] - 2, an[4]); }
    [Fact] public void Hecke_GoodRecursion_a9() { var an = E11.An(200, out _); Assert.Equal(an[3] * an[3] - 3, an[9]); }
    [Fact] public void Hecke_BadPrimeRule_a121() { var an = E11.An(200, out _); Assert.Equal(an[11] * an[11], an[121]); }

    // ── 3. Constructor guards, one witness each ───────────────────────────────
    [Fact]
    public void Guard_SingularCurve()
        => Assert.Contains("Singular", Assert.Throws<ArgumentException>(
            () => new EllipticCurve(0, 0, 0, 0, 0, 0, Array.Empty<long>())).Message);

    [Fact]
    public void Guard_BadPrimeMustDivideDelta()
        => Assert.Contains("does not divide", Assert.Throws<ArgumentException>(
            () => new EllipticCurve(0, -1, 1, -10, -20, 11, new long[] { 7 })).Message);

    [Fact]
    public void Guard_AdditiveReductionRefused()
        => Assert.Contains("Additive", Assert.Throws<NotSupportedException>(
            () => new EllipticCurve(0, 0, 0, 0, 1, 36, new long[] { 2, 3 })).Message);

    // u = 2 scaling of 11a1: non-minimal, and the diagnostic must SAY so (v1.0.2 reorder).
    [Fact]
    public void Guard_NonMinimalNamedAsSuch()
        => Assert.Contains("non-minimal", Assert.Throws<NotSupportedException>(
            () => new EllipticCurve(0, -4, 8, -160, -1280, 11, new long[] { 2, 11 })).Message);

    // ── 4. Tamagawa semistable rule in isolation ──────────────────────────────
    [Fact] public void Tamagawa_11a1_Split() => Assert.Equal(5, E11.TamagawaProduct(new Dictionary<long, long> { [11] = 1 }));
    [Fact] public void Tamagawa_11a1_NonSplit() => Assert.Equal(1, E11.TamagawaProduct(new Dictionary<long, long> { [11] = -1 }));
    [Fact] public void Tamagawa_30a1_AllSplit() => Assert.Equal(12, E30.TamagawaProduct(new Dictionary<long, long> { [2] = 1, [3] = 1, [5] = 1 }));
    [Fact] public void Tamagawa_30a1_AllNonSplit() => Assert.Equal(2, E30.TamagawaProduct(new Dictionary<long, long> { [2] = -1, [3] = -1, [5] = -1 }));

    // ── 5. Root numbers from real a_p (233 exercises the Finding-1 seed) ───────
    [Fact] public void RootNumber_11a1_Plus1() { E11.An(50, out var ap); Assert.Equal(+1, E11.RootNumber(ap)); }
    [Fact] public void RootNumber_37a1_Minus1() { E37.An(50, out var ap); Assert.Equal(-1, E37.RootNumber(ap)); }
    [Fact] public void RootNumber_30a1_Plus1() { E30.An(50, out var ap); Assert.Equal(+1, E30.RootNumber(ap)); }
    [Fact] public void RootNumber_233_Plus1_ViaSeededBadPrime() { E233.An(50, out var ap); Assert.Equal(+1, E233.RootNumber(ap)); }

    // ── 6. Series sizing: the Finding-1 boundary, frozen ──────────────────────
    [Fact] public void TermsFor_233_30_IsOneShortOfConductor() => Assert.Equal(232, Analytic.TermsFor(233, 30));
    [Fact] public void TermsFor_233_40_ReachesConductor() => Assert.True(Analytic.TermsFor(233, 40) >= 233);

    // ── 7. Torsion state: certified values pass; open defect documented ───────
    [Fact] public void TorsionBound_11a1_Is5() => Assert.Equal(5, E11.TorsionBound());
    [Fact] public void TorsionBound_27606c1_Is1() => Assert.Equal(1, E27606.TorsionBound());
    [Fact] public void TorsionBound_233_Is2() => Assert.Equal(2, E233.TorsionBound());

    // Finding 2 (OPEN): 30a1's gcd bound is 12 (true order 6) and its |Sha| is a
    // plausible wrong square 4.0. These assert the CURRENT wrong values on purpose,
    // so v1.1's exact-torsion certification must flip them visibly, never silently.
    [Fact]
    [Trait("Finding", "2")]
    public void TorsionBound_30a1_Is12_DocumentsOpenFinding2() => Assert.Equal(12, E30.TorsionBound());

    [Fact]
    [Trait("Finding", "2")]
    public void Sha_30a1_Reports4_DocumentsOpenFinding2()
        => Assert.StartsWith("4.00000000", BsdCompiler.RunRankZero(E30, digits: 20).ShaEstimate.ToDecimalString(10));
}
