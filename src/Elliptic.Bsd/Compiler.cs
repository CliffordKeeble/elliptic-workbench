using System.Globalization;
using Elliptic.Numerics;

namespace Elliptic.Bsd;

/// <summary>
/// ⚠ v1 limitation (recorded 3 Jul 2026): <c>TorsionBound</c> is a gcd upper bound, not a
/// certified order, and it enters <c>ShaEstimate</c> squared — an overshoot by k inflates the
/// estimate by k² (witness: 30a1 reports 4 where the truth is 1, and 4 is itself a plausible
/// square). Treat ShaEstimate as exact only for curves whose torsion is independently known
/// until v1.1 lands Lutz–Nagell certification.
/// <para>
/// <c>SeriesTailBound</c> is the bound on what the L-series truncation dropped, at the term count
/// actually used (<see cref="Analytic.TailBound"/>). It is not always below 10^−digits: see the
/// remarks on <see cref="Analytic.TermsFor"/>.
/// </para>
/// </summary>
public sealed record RankZeroReport(
    long Conductor,
    int RootNumber,
    long TamagawaProduct,
    long TorsionBound,
    BigFloat LValue,
    BigFloat Omega,
    BigFloat ShaEstimate,
    BigFloat SeriesTailBound);

/// <summary>
/// v1 of the BSD compiler: the rank-0 channel (ε = +1, semistable, minimal model).
/// Five Weierstrass coefficients, the conductor and the bad primes in, the BSD quotient out;
/// the constructor refuses any triple it cannot check (see <see cref="EllipticCurve"/>). No
/// external data anywhere in the pipeline (LMFDB published values appear only in the
/// acceptance suite, as oracles-after —
/// the discipline of papers/paper_144_genuine_phantoms/ §4.3; the frozen 3 July 2026 bench values they replaced in
/// the LMFDB conversion are recorded in app/Elliptic.Bsd.Acceptance/acceptance-constants.md).
/// v2 (derivative engine: Γ*, η(s), Richardson — Papers 140/142) and v3 (full Tate at p = 2, 3)
/// extend this class.
/// </summary>
public static class BsdCompiler
{
    public static RankZeroReport RunRankZero(EllipticCurve e, int digits = 30)
    {
        // LValueRankZero refuses ε ≠ +1, so every report from here has ε = +1.
        var l = Analytic.LValueRankZero(e, digits, out var ap);
        const int eps = +1;

        // Even analytic rank >= 2 (389a1: ε = +1, rank 2) gives L(E,1) = 0, and the quotient
        // below would be |Ш| ~ 0. The root number cannot see it; |L| against the series tail
        // does: if |L| is no larger than what the truncation may have dropped, L(E,1) is not
        // certified non-zero and the rank-0 channel does not apply.
        var terms = Analytic.TermsFor(e.Conductor, digits);
        var tail = Analytic.TailBound(e.Conductor, terms);
        if (l.Abs() <= tail)
            throw new NotSupportedException(
                "L(E,1) is not certified non-zero (|L| is within the series tail): analytic rank ≥ 2 " +
                "is outside the rank-0 channel. " +
                $"|L| = {l.Abs().ToDouble().ToString("E3", CultureInfo.InvariantCulture)}, " +
                $"tail bound = {tail.ToDouble().ToString("E3", CultureInfo.InvariantCulture)}, " +
                $"{terms} terms, N = {e.Conductor}, {digits} digits.");

        var omega = Analytic.RealPeriod(e);
        var tam = e.TamagawaProduct(ap);
        var tor = e.TorsionBound();

        // Rank 0: R = 1.  |Sha| estimate = L · |tor|² / (Ω · ∏c_p).
        var sha = (l * (tor * tor)) / (omega * BigFloat.From(tam));
        return new RankZeroReport(e.Conductor, eps, tam, tor, l, omega, sha, tail);
    }
}
