using System.Numerics;
using Elliptic.Bsd;
using Elliptic.Numerics;
using Xunit;

// BigFloat.Precision is a process-global static; keep the suite single-threaded.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Elliptic.Bsd.Tests;

// Direct port of the v1.0.2 Elliptic.Bsd console battery into xUnit. Component-level
// rules, distinct from the end-to-end acceptance suite: a_p vs an in-process brute
// counter (cross-method inside C#), Hecke identities from the recursion's own output,
// every constructor guard with a witness curve, and the open torsion-bound defect asserted
// explicitly (tagged [Trait("OpenDefect","TorsionBound")]) so v1.1 flips it deliberately.
public class BsdTests
{
    public BsdTests() => BigFloat.Precision = 320;

    static readonly EllipticCurve E11 = new(0, -1, 1, -10, -20, 11, new long[] { 11 });
    static readonly EllipticCurve E30 = new(1, 0, 1, 1, 2, 30, new long[] { 2, 3, 5 });
    static readonly EllipticCurve E37 = new(0, 0, 1, -1, 0, 37, new long[] { 37 });
    static readonly EllipticCurve E233 = new(1, 3, 0, -1, 0, 233, new long[] { 233 });
    static readonly EllipticCurve E27606 = new(1, 0, 0, -10289707, 12703497719, 27606, new long[] { 2, 3, 43, 107 });
    static readonly EllipticCurve E389 = new(0, 1, 1, -2, 0, 389, new long[] { 389 });

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

    // ── 3b. The constructor refuses what it cannot check ──
    // Four ways the engine returned, hung or threw the wrong
    // exception on input it had no business accepting. Each case below was RED on
    // 64dfb8f before the constructor's validation was written.

    // T1. The u = 2 rescaling of 11a1 declaring only { 11 }: the u¹² test runs
    // at declared primes only, so p = 2 was never looked at and the engine returned
    // halved Ω and |Ш| = 2.348. Δ = −2¹²·11⁵, so 2 is a bad prime and undeclared.
    [Fact]
    public void Guard_UndeclaredDeltaPrime_U2ModelWithOnly11()
        => Assert.Throws<ArgumentException>(
            () => new EllipticCurve(0, -4, 8, -160, -1280, 11, new long[] { 11 }));

    // T2. The conductor was accepted unchecked; the engine is semistable-only,
    // so N must be the product of the declared primes.
    [Fact]
    public void Guard_ConductorMustEqualProductOfBadPrimes()
        => Assert.Throws<ArgumentException>(
            () => new EllipticCurve(0, -1, 1, -10, -20, 12, new long[] { 11 }));

    // T3 (the hang). p = 1 divides everything, so Ord looped for ever. Run the
    // constructor off-thread so a regression cannot hang the suite.
    [Fact]
    public void Guard_BadPrimeOne_ThrowsAndDoesNotHang()
    {
        var t = Task.Run(() => new EllipticCurve(0, -1, 1, -10, -20, 11, new long[] { 1, 11 }));
        // xUnit1031 suppressed deliberately: the bounded Wait IS the assertion. An awaited
        // task would hang the run for ever on a regression, which is the defect under test.
        // Wait rethrows a faulted task's exception as AggregateException rather than
        // returning true, so catching it is how "finished inside the window" is read; a
        // genuine hang still returns false and trips the assertion below.
        bool finished;
#pragma warning disable xUnit1031
        try { finished = t.Wait(TimeSpan.FromSeconds(5)); }
#pragma warning restore xUnit1031
        catch (AggregateException) { finished = true; }
        Assert.True(finished, "constructor did not return within 5 s (Ord hang).");
        Assert.True(t.IsFaulted, "constructor accepted p = 1 (returned without throwing).");
        Assert.IsAssignableFrom<ArgumentException>(t.Exception!.InnerException);
    }

    // T4. Duplicates, zero, negatives and composites in badPrimes. The message
    // must name the offending value, so the caller can see which entry was rejected.
    [Theory]
    [InlineData(0, -1, 1, -10, -20, 11, new long[] { 11, 11 }, "11")]   // declared twice
    [InlineData(0, -1, 1, -10, -20, 11, new long[] { 0, 11 }, "0")]     // DivideByZero from inside the guard
    [InlineData(0, -1, 1, -10, -20, 11, new long[] { -11 }, "-11")]     // nonsense ε
    [InlineData(1, 0, 1, 1, 2, 30, new long[] { 6, 5 }, "6")]           // composite, and it divides Δ
    public void Guard_BadPrimesMustBeDistinctPrimes(
        long a1, long a2, long a3, long a4, long a6, long conductor, long[] badPrimes, string named)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new EllipticCurve(a1, a2, a3, a4, a6, conductor, badPrimes));
        Assert.Contains(named, ex.Message);
    }

    // ── 4. Tamagawa semistable rule in isolation ──────────────────────────────
    [Fact] public void Tamagawa_11a1_Split() => Assert.Equal(5, E11.TamagawaProduct(new Dictionary<long, long> { [11] = 1 }));
    [Fact] public void Tamagawa_11a1_NonSplit() => Assert.Equal(1, E11.TamagawaProduct(new Dictionary<long, long> { [11] = -1 }));
    [Fact] public void Tamagawa_30a1_AllSplit() => Assert.Equal(12, E30.TamagawaProduct(new Dictionary<long, long> { [2] = 1, [3] = 1, [5] = 1 }));
    [Fact] public void Tamagawa_30a1_AllNonSplit() => Assert.Equal(2, E30.TamagawaProduct(new Dictionary<long, long> { [2] = -1, [3] = -1, [5] = -1 }));

    // ── 5. Root numbers from real a_p (233 exercises the bad-prime seed of a_p) ───────
    [Fact] public void RootNumber_11a1_Plus1() { E11.An(50, out var ap); Assert.Equal(+1, E11.RootNumber(ap)); }
    [Fact] public void RootNumber_37a1_Minus1() { E37.An(50, out var ap); Assert.Equal(-1, E37.RootNumber(ap)); }
    [Fact] public void RootNumber_30a1_Plus1() { E30.An(50, out var ap); Assert.Equal(+1, E30.RootNumber(ap)); }
    [Fact] public void RootNumber_233_Plus1_ViaSeededBadPrime() { E233.An(50, out var ap); Assert.Equal(+1, E233.RootNumber(ap)); }

    // ── 6. Series sizing: the conductor-above-series-bound boundary, frozen ──────────────────────
    [Fact] public void TermsFor_233_30_IsOneShortOfConductor() => Assert.Equal(232, Analytic.TermsFor(233, 30));
    [Fact] public void TermsFor_233_40_ReachesConductor() => Assert.True(Analytic.TermsFor(233, 40) >= 233);

    // ── 7. Torsion state: certified values pass; open defect documented ───────
    [Fact] public void TorsionBound_11a1_Is5() => Assert.Equal(5, E11.TorsionBound());
    [Fact] public void TorsionBound_27606c1_Is1() => Assert.Equal(1, E27606.TorsionBound());
    [Fact] public void TorsionBound_233_Is2() => Assert.Equal(2, E233.TorsionBound());

    // Open torsion-bound defect: 30a1's gcd bound is 12 (true order 6) and its |Sha| is a
    // plausible wrong square 4.0. These assert the CURRENT wrong values on purpose,
    // so v1.1's exact-torsion certification must flip them visibly, never silently.
    [Fact]
    [Trait("OpenDefect", "TorsionBound")]
    public void TorsionBound_30a1_Is12_DocumentsOpenTorsionBoundDefect() => Assert.Equal(12, E30.TorsionBound());

    [Fact]
    [Trait("OpenDefect", "TorsionBound")]
    public void Sha_30a1_Reports4_DocumentsOpenTorsionBoundDefect()
        => Assert.StartsWith("4.00000000", BsdCompiler.RunRankZero(E30, digits: 20).ShaEstimate.ToDecimalString(10));

    // ── 8. The library refuses what it cannot answer ──

    // 8a. Even analytic rank >= 2: 389a1 has epsilon = +1 and rank 2, so L(E,1) = 0 and the
    // rank-0 quotient is meaningless. It used to come back as |Sha| ~ 0. The root number alone
    // cannot catch it; the gate is |L| against the series tail.
    [Fact]
    public void RunRankZero_389a1_Rank2_IsRefused()
        => Assert.Contains("not certified", Assert.Throws<NotSupportedException>(
            () => BsdCompiler.RunRankZero(E389)).Message);

    // The spec says epsilon = -1 is refused; nothing tested it.
    [Fact]
    public void RunRankZero_37a1_EpsilonMinus1_IsRefused()
        => Assert.Contains("ε = -1", Assert.Throws<NotSupportedException>(
            () => BsdCompiler.RunRankZero(E37)).Message);

    // 8b. digits must fit what the working precision carries: 1 <= digits <= floor(0.30103 * 320) - 10 = 86.
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(87)]
    [InlineData(200)]
    public void LValue_DigitsOutOfRange_Throws(int digits)
        => Assert.Throws<ArgumentOutOfRangeException>(() => Analytic.LValueRankZero(E11, digits, out _));

    [Fact]
    public void RunRankZero_DigitsZero_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => BsdCompiler.RunRankZero(E11, digits: 0));

    [Fact]
    public void LValue_DigitsAtTheBound_IsAccepted()
        => Analytic.LValueRankZero(E11, 86, out _);

    // The term count is capped at 10,000,000 (An allocates a long table that long). 10^12 and
    // long.MaxValue both overshoot it; the second also wrapped the int cast.
    [Theory]
    [InlineData(1_000_000_000_000L, 30)]
    [InlineData(long.MaxValue, 30)]
    public void TermsFor_BeyondTheCap_Throws(long conductor, int digits)
        => Assert.Throws<ArgumentOutOfRangeException>(() => Analytic.TermsFor(conductor, digits));

    [Fact]
    public void TermsFor_WellInsideTheCap_IsAccepted()
        => Assert.InRange(Analytic.TermsFor(100_000_000_000L, 30), 3_000_000, 4_000_000);

    // 8c. The validated input stays validated.
    [Fact]
    public void BadPrimes_CannotBeCastBackAndMutated()
    {
        var e = new EllipticCurve(0, -1, 1, -10, -20, 11, new long[] { 11 });
        Assert.IsNotType<long[]>(e.BadPrimes);
        Assert.Throws<NotSupportedException>(() => ((IList<long>)e.BadPrimes)[0] = 7);
        Assert.Equal(11L, e.BadPrimes[0]);
    }

    // Green from the start: the constructor copies, so the caller's array is not shared.
    [Fact]
    public void BadPrimes_IsACopyOfTheCallersArray()
    {
        var mine = new long[] { 11 };
        var e = new EllipticCurve(0, -1, 1, -10, -20, 11, mine);
        mine[0] = 7;
        Assert.Equal(11L, e.BadPrimes[0]);
    }

    // 8d. The public helpers refuse what the constructor refuses.
    // Ord(n, 1) looped for ever (n % 1 == 0, n / 1 == n): off-thread, as T3.
    [Fact]
    public void Ord_PrimeOne_ThrowsAndDoesNotHang()
    {
        var t = Task.Run(() => EllipticCurve.Ord(12, 1));
        bool finished;
#pragma warning disable xUnit1031   // the bounded Wait IS the assertion; see T3 above.
        try { finished = t.Wait(TimeSpan.FromSeconds(5)); }
#pragma warning restore xUnit1031
        catch (AggregateException) { finished = true; }
        Assert.True(finished, "Ord(12, 1) did not return within 5 s (the p = 1 hang).");
        Assert.True(t.IsFaulted, "Ord(12, 1) returned without throwing.");
        Assert.IsType<ArgumentOutOfRangeException>(t.Exception!.InnerException);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-5L)]
    public void Ord_PrimeBelowTwo_Throws(long p)
        => Assert.Throws<ArgumentOutOfRangeException>(() => EllipticCurve.Ord(12, p));

    // The valuation of 0 is infinite. Not a throw: the constructor's u^12 test evaluates
    // Ord(c4, p) and c4 = 0 is a legal model (y^2 = x^3 + 1).
    [Fact]
    public void Ord_Zero_IsInfinite() => Assert.Equal(int.MaxValue, EllipticCurve.Ord(0, 7));

    [Fact]
    public void Ord_OrdinaryValues() { Assert.Equal(3, EllipticCurve.Ord(8, 2)); Assert.Equal(2, EllipticCurve.Ord(-18, 3)); Assert.Equal(0, EllipticCurve.Ord(10, 3)); }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-3L)]
    public void Ap_PrimeBelowTwo_Throws(long p)
        => Assert.Throws<ArgumentOutOfRangeException>(() => E11.Ap(p));

    [Theory]
    [InlineData(4L)]
    [InlineData(9L)]
    [InlineData(15L)]
    public void Ap_Composite_Throws(long p)
        => Assert.Throws<ArgumentException>(() => E11.Ap(p));

    [Fact]
    public void TorsionPrimes_AreTheOddPrimesTo37()
    {
        Assert.Equal(new long[] { 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 }, EllipticCurve.TorsionPrimes.ToArray());
        Assert.DoesNotContain(2L, EllipticCurve.TorsionPrimes);
    }

    // 8e. The spec promises L(E,1) and the real period, and a planted fault in L used to
    // turn one xUnit test red, and that one asserted a known-wrong value. 11a1 against the
    // archived strings, copied as literals from
    //   app/Elliptic.Bsd.Acceptance/lmfdb/ec_mwbsd_11.a2.json  (fields real_period, special_value)
    // Both are compared at 28 dp, +-1/2 ulp: special_value displays 38 dp but is
    // self-consistent only to about 28 (acceptance-constants.md).
    private const string Omega11Archived = "1.2692093042795534216887946168";
    private const string L11Archived = "0.25384186085591068433775892335043887465";

    static BigFloat Decimal(string s)
    {
        int dot = s.IndexOf('.');
        return BigFloat.FromRatio(BigInteger.Parse(s.Remove(dot, 1)), BigInteger.Pow(10, s.Length - dot - 1));
    }

    static void AssertAgreesTo28dp(BigFloat got, string archived)
    {
        var halfUlp = BigFloat.FromRatio(1, 2 * BigInteger.Pow(10, 28));
        var diff = (got - Decimal(archived)).Abs();
        Assert.True(diff <= halfUlp, $"{got.ToDecimalString(32)} vs archived {archived}");
    }

    [Fact]
    public void Omega_11a1_MatchesTheArchive_28dp()
        => AssertAgreesTo28dp(Analytic.RealPeriod(E11), Omega11Archived);

    [Fact]
    public void LValue_11a1_MatchesTheArchive_28dp()
        => AssertAgreesTo28dp(Analytic.LValueRankZero(E11, 34, out _), L11Archived);
}
