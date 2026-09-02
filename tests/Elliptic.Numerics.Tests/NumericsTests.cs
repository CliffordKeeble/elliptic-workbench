// Ported verbatim from Icosian.Numerics.Tests/NumericsTests.cs (CliffordKeeble/Icosian,
// 2 Sep 2026), namespace apart. Icosian holds the twin; the two suites test
// byte-identical copies of the same class and are expected to stay in step.
using System.Numerics;
using Elliptic.Numerics;
using Xunit;

// BigFloat.Precision is a process-global static; keep the suite single-threaded so
// the one precision-excursion test (PrecisionContract) cannot race the others.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Elliptic.Numerics.Tests;

// Direct port of the v1.0.2 Icosian.Numerics console battery into xUnit. Every check
// is preserved 1:1. Reference constants were generated fresh from mpmath (mp.dps=110,
// 3 Jul 2026) — cross-method by construction, not memory-typed:
//   python -c "from mpmath import mp,mpf,sqrt,exp,pi,nstr; mp.dps=110; ..."
public class NumericsTests
{
    public NumericsTests() => BigFloat.Precision = 320;   // reset the global before each test

    private const string Pi90 =
        "3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628035";

    static BigFloat Scale(BigFloat x, int k) => k >= 0
        ? x * BigFloat.From(BigInteger.One << k)
        : x / BigFloat.From(BigInteger.One << -k);

    // Close = relative error below 2^-(Precision - slack).
    static void AssertClose(BigFloat got, BigFloat want, int slackBits = 16)
    {
        var diff = got - want;
        bool ok = diff.IsZero || (want.Mag - diff.Mag) >= BigFloat.Precision - slackBits;
        Assert.True(ok, ok ? "" : $"rel error ~2^{diff.Mag - want.Mag}");
    }

    // ── 1. Exactness and structure ───────────────────────────────────────────
    [Fact] public void ZeroPlusX_IsExact() => Assert.True((BigFloat.Zero + BigFloat.FromRatio(22, 7) - BigFloat.FromRatio(22, 7)).IsZero);
    [Fact] public void XTimesZero_IsZero() => Assert.True((BigFloat.FromRatio(22, 7) * BigFloat.Zero).IsZero);
    [Fact] public void XPlusNegX_IsZero() => Assert.True((BigFloat.FromRatio(22, 7) + (-BigFloat.FromRatio(22, 7))).IsZero);
    [Fact] public void HalfTwice_IsExact() => Assert.True((BigFloat.FromRatio(22, 7).Half().Twice() - BigFloat.FromRatio(22, 7)).IsZero);
    [Fact] public void SqrtZero_IsZero() => Assert.True(BigFloat.Sqrt(BigFloat.Zero).IsZero);
    [Fact] public void ExpZero_IsOne() => Assert.True((BigFloat.Exp(BigFloat.Zero) - BigFloat.One).IsZero);
    [Fact] public void ZeroRendersAsZero() => Assert.Equal("0.00000", BigFloat.Zero.ToDecimalString(5));

    // ── 2. Algebraic identities across magnitude ranges ──────────────────────
    // Fixed-precision law: for an exponent gap g, (a+b)-b recovers a to ~(Precision-g) bits.
    [Fact] public void AddSub_Gap2p200_Up() { var pi = Reals.Pi(); AssertClose((pi + Scale(pi, 200)) - Scale(pi, 200), pi, slackBits: 200 + 24); }
    [Fact] public void AddSub_Gap2p200_Down() { var pi = Reals.Pi(); AssertClose((pi + Scale(pi, -200)) - Scale(pi, -200), pi); }
    [Fact] public void MulInverse_At2p100() { var a = Scale(BigFloat.FromRatio(7, 3), 100); AssertClose(a * (BigFloat.One / a), BigFloat.One); }
    [Fact] public void SqrtSquared_At2m97() { var x = Scale(BigFloat.FromRatio(3, 7), -97); AssertClose(BigFloat.Sqrt(x) * BigFloat.Sqrt(x), x); }
    [Fact] public void ExpReciprocal() { var e = BigFloat.FromRatio(7, 3); AssertClose(BigFloat.Exp(e) * BigFloat.Exp(-e), BigFloat.One); }
    [Fact] public void ExpAdditivity() { var a = BigFloat.FromRatio(1, 3); var b = -BigFloat.FromRatio(5, 7); AssertClose(BigFloat.Exp(a + b), BigFloat.Exp(a) * BigFloat.Exp(b)); }
    [Fact] public void Agm_XX_IsX() { var g = BigFloat.FromRatio(5, 3); AssertClose(Reals.Agm(g, g), g); }
    [Fact] public void Agm_Symmetry() { var a = BigFloat.One; var b = BigFloat.FromRatio(7, 5); AssertClose(Reals.Agm(a, b), Reals.Agm(b, a)); }
    [Fact] public void Agm_Scaling() { var a = BigFloat.One; var b = BigFloat.FromRatio(7, 5); AssertClose(Reals.Agm(a.Twice(), b.Twice()), Reals.Agm(a, b).Twice()); }

    // ── 3. Constants vs mpmath (cross-method by construction) ─────────────────
    [Fact] public void Pi_BrentSalamin() => Assert.StartsWith(Pi90, Reals.Pi().ToDecimalString(90));
    [Fact] public void Sqrt2() => Assert.StartsWith(
        "1.414213562373095048801688724209698078569671875376948073176679737990732478462107038850387",
        BigFloat.Sqrt(BigFloat.From(2)).ToDecimalString(90));
    [Fact] public void ExpOne_IsE() => Assert.StartsWith(
        "2.718281828459045235360287471352662497757247093699959574966967627724076630353547594571382",
        BigFloat.Exp(BigFloat.One).ToDecimalString(90));
    [Fact] public void ExpNegOne_IsReciprocalE() => Assert.StartsWith(
        "0.367879441171442321595523770161460867445811131031767834507836801697461495744899803357147",
        BigFloat.Exp(-BigFloat.One).ToDecimalString(90));
    [Fact] public void ExpNeg7over3_RangeReductionPath() => Assert.StartsWith(
        "0.096971967864405062809906659298370731480720858924804393653047104108325424087779603534469",
        BigFloat.Exp(-BigFloat.FromRatio(7, 3)).ToDecimalString(90));

    // ── 4. Precision contract and the Pi cache ────────────────────────────────
    [Fact]
    public void Pi_At128Bits_Correct30Digits()
    {
        BigFloat.Precision = 128;
        Assert.StartsWith("3.141592653589793238462643383279", Reals.Pi().ToDecimalString(34));
    }

    [Fact]
    public void PiCache_IsPerPrecision()
    {
        BigFloat.Precision = 128; var pi128 = Reals.Pi();
        BigFloat.Precision = 320; var pi320 = Reals.Pi();
        Assert.False((pi320 - pi128).IsZero);
    }

    [Fact]
    public void Pi_At320_UnharmedByExcursion()
    {
        BigFloat.Precision = 128; _ = Reals.Pi();
        BigFloat.Precision = 320;
        Assert.StartsWith(Pi90, Reals.Pi().ToDecimalString(90));
    }

    // ── 5. Representation edges ────────────────────────────────────────────────
    [Fact] public void AddCliff_BeyondPrecision_Absorbs() => Assert.True(((BigFloat.One + Scale(BigFloat.One, -713)) - BigFloat.One).IsZero);
    [Fact] public void Add_WithinPrecision_Survives() => Assert.True(((BigFloat.One + Scale(BigFloat.One, -300)) - BigFloat.One - Scale(BigFloat.One, -300)).IsZero);
    [Fact] public void RoundHalfUp_OneEighth() => Assert.Equal("0.13", (BigFloat.One / 8L).ToDecimalString(2));
    [Fact] public void RoundHalfUp_OneQuarter() => Assert.Equal("0.3", (BigFloat.One / 4L).ToDecimalString(1));
    [Fact] public void NegativeRendering() => Assert.Equal("-0.13", (-(BigFloat.One / 8L)).ToDecimalString(2));
    [Fact] public void ToDouble_ExactOnThreeQuarters() => Assert.Equal(0.75, (BigFloat.From(3) / 4L).ToDouble());
    [Fact] public void ToDouble_Pi_Within1Ulp() => Assert.True(Math.Abs(Reals.Pi().ToDouble() - Math.PI) <= Math.PI * Math.Pow(2, -52));

    // ── 6. Error contracts ─────────────────────────────────────────────────────
    [Fact] public void DivByZeroBigFloat_Throws() => Assert.Throws<DivideByZeroException>(() => { var _ = BigFloat.One / BigFloat.Zero; });
    [Fact] public void DivByZeroLong_Throws() => Assert.Throws<DivideByZeroException>(() => { var _ = BigFloat.One / 0L; });
    [Fact] public void SqrtNegative_Throws() => Assert.Throws<ArithmeticException>(() => BigFloat.Sqrt(BigFloat.From(-2)));
    [Fact] public void AgmNonPositive_Throws() => Assert.Throws<ArithmeticException>(() => Reals.Agm(BigFloat.Zero, BigFloat.One));
    [Fact] public void ExpRangeGuard_Throws() => Assert.Throws<ArithmeticException>(() => BigFloat.Exp(Scale(BigFloat.One, 250)));
}
