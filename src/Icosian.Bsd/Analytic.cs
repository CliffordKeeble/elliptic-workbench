using Icosian.Numerics;

namespace Icosian.Bsd;

/// <summary>
/// The analytic stages of the compiler.
///
/// L-value (rank 0, ε = +1):  L(E,1) = 2 Σ (a_n/n) e^(−2πn/√N)  — an exact identity, not an
/// approximation; the only errors are truncation (controlled by the digit target) and roundoff.
/// The kernel is computed with a single Exp call and advanced by one multiplication per term.
///
/// Real period, via the AGM (both discriminant signs; no quadrature, no spike, no e₁-sign trap):
///   Δ &gt; 0, roots e₁ &gt; e₂ &gt; e₃ of X³ + AX + B:   Ω = 2π / AGM(√(e₁−e₃), √(e₁−e₂))
///   Δ &lt; 0, real root e₁, c = 3e₁, d = 3e₁² + A:  Ω = π / AGM(√(2√d + c)/2, d^(1/4))
/// Both validated to 30 digits against 11a1, 37a1, 389a1, 27606c1 (3 July 2026 bench),
/// including the near-degenerate 27606c1 case that defeats naive quadrature (Paper 144 §3.4).
/// Roots are found by full-precision bisection inside Cauchy bounds — no seeds, no branches.
/// </summary>
public static class Analytic
{
    /// <summary>Number of series terms for the given decimal-digit target.</summary>
    public static int TermsFor(long conductor, int digits)
        => (int)Math.Ceiling(digits * Math.Log(10) * Math.Sqrt(conductor) / (2 * Math.PI)) + 64;

    /// <summary>L(E, 1) for ε = +1 via the exact smoothed series, to roughly <paramref name="digits"/> digits.</summary>
    public static BigFloat LValueRankZero(EllipticCurve e, int digits, out Dictionary<long, long> ap)
    {
        int nmax = TermsFor(e.Conductor, digits);
        var an = e.An(nmax, out ap);

        var pi = Reals.Pi();
        var sqrtN = BigFloat.Sqrt(BigFloat.From(e.Conductor));
        var q = BigFloat.Exp(-(pi.Twice() / sqrtN));    // e^(−2π/√N), once

        var qn = BigFloat.One;
        var sum = BigFloat.Zero;
        for (int n = 1; n <= nmax; n++)
        {
            qn *= q;
            if (an[n] == 0) continue;
            sum += (qn * an[n]) / n;
        }
        return sum.Twice();
    }

    /// <summary>The real period Ω of the minimal model (LMFDB normalisation), via the AGM.</summary>
    public static BigFloat RealPeriod(EllipticCurve e)
    {
        var a = BigFloat.FromRatio(-e.C4, 48);          // short form X³ + AX + B
        var b = BigFloat.FromRatio(-e.C6, 864);
        var pi = Reals.Pi();

        if (e.Delta.Sign > 0)
        {
            // Three real roots; e1 is the largest, bracketed in [√(−A/3), 1 + max(|A|,|B|)].
            var lo = BigFloat.Sqrt((-a) / 3L);
            var hi = CauchyBound(a, b);
            var e1 = Bisect(a, b, lo, hi);
            // Deflate: X² + e1 X + (e1² + A); disc = −3e1² − 4A > 0 for three distinct roots.
            var disc = -(e1 * e1 * 3L) - a * 4L;
            var s = BigFloat.Sqrt(disc);
            var e2 = (-e1 + s).Half();
            var e3 = (-e1 - s).Half();
            return pi.Twice() / Reals.Agm(BigFloat.Sqrt(e1 - e3), BigFloat.Sqrt(e1 - e2));
        }
        else
        {
            // One real root, bracketed in [−M, M].
            var m = CauchyBound(a, b);
            var e1 = Bisect(a, b, -m, m);
            var c = e1 * 3L;
            var d = e1 * e1 * 3L + a;
            var sd = BigFloat.Sqrt(d);
            var arg1 = BigFloat.Sqrt(sd.Twice() + c).Half();
            var arg2 = BigFloat.Sqrt(sd);               // d^(1/4)
            return pi / Reals.Agm(arg1, arg2);
        }
    }

    private static BigFloat CauchyBound(BigFloat a, BigFloat b)
    {
        var m = a.Abs();
        if (b.Abs() > m) m = b.Abs();
        return m + BigFloat.From(2);
    }

    private static BigFloat Eval(BigFloat a, BigFloat b, BigFloat x) => x * x * x + a * x + b;

    /// <summary>Bisection for the root where f crosses − → + (the largest / only real root). Full precision.</summary>
    private static BigFloat Bisect(BigFloat a, BigFloat b, BigFloat lo, BigFloat hi)
    {
        int iters = BigFloat.Precision + 48;
        for (int i = 0; i < iters; i++)
        {
            var mid = (lo + hi).Half();
            if (Eval(a, b, mid).Sign < 0) lo = mid; else hi = mid;
        }
        return (lo + hi).Half();
    }
}
