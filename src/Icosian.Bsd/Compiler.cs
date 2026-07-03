using Icosian.Numerics;

namespace Icosian.Bsd;

public sealed record RankZeroReport(
    long Conductor,
    int RootNumber,
    long TamagawaProduct,
    long TorsionBound,
    BigFloat LValue,
    BigFloat Omega,
    BigFloat ShaEstimate);

/// <summary>
/// v1 of the BSD compiler: the rank-0 channel (ε = +1, semistable, minimal model).
/// Five Weierstrass coefficients in, the BSD quotient out; no external data anywhere in the
/// pipeline (LMFDB and the frozen 3 July 2026 bench values appear only in the acceptance
/// suite, as oracles-after — the discipline of Paper 144 §4.3).
/// v2 (derivative engine: Γ*, η(s), Richardson — Papers 140/142) and v3 (full Tate at p = 2, 3)
/// extend this class; see the Mr Code brief.
/// </summary>
public static class BsdCompiler
{
    public static RankZeroReport RunRankZero(EllipticCurve e, int digits = 30)
    {
        var l = Analytic.LValueRankZero(e, digits, out var ap);
        var eps = e.RootNumber(ap);
        if (eps != +1)
            throw new NotSupportedException(
                $"ε = {eps}: odd analytic rank forced; the rank-0 channel does not apply (v2 scope).");

        var omega = Analytic.RealPeriod(e);
        var tam = e.TamagawaProduct(ap);
        var tor = e.TorsionBound();

        // Rank 0: R = 1.  |Sha| estimate = L · |tor|² / (Ω · ∏c_p).
        var sha = (l * (tor * tor)) / (omega * BigFloat.From(tam));
        return new RankZeroReport(e.Conductor, eps, tam, tor, l, omega, sha);
    }
}
