namespace Elliptic.Numerics;

/// <summary>
/// Real-analytic building blocks over <see cref="BigFloat"/>: π (Brent–Salamin, cached per
/// precision) and the arithmetic-geometric mean. Both converge quadratically. BCL-only.
/// </summary>
public static class Reals
{
    private static readonly Dictionary<int, BigFloat> PiCache = new();

    /// <summary>π at the current working precision, via the Brent–Salamin (Gauss–Legendre) iteration.</summary>
    public static BigFloat Pi()
    {
        int p = BigFloat.Precision;
        if (PiCache.TryGetValue(p, out var cached)) return cached;

        var a = BigFloat.One;
        var b = BigFloat.One / BigFloat.Sqrt(BigFloat.From(2));
        var t = BigFloat.One.Half().Half();          // 1/4
        var pw = BigFloat.One;

        int iters = (int)Math.Ceiling(Math.Log2(p)) + 3;
        for (int i = 0; i < iters; i++)
        {
            var an = (a + b).Half();
            var bn = BigFloat.Sqrt(a * b);
            var d = a - an;
            t -= pw * (d * d);
            pw = pw.Twice();
            a = an;
            b = bn;
        }
        var s = a + b;
        var pi = (s * s) / (t * 4L);
        PiCache[p] = pi;
        return pi;
    }

    /// <summary>AGM(a, b) for positive a, b.</summary>
    public static BigFloat Agm(BigFloat a, BigFloat b)
    {
        if (a.Sign <= 0 || b.Sign <= 0) throw new ArithmeticException("Agm requires positive arguments.");
        for (int i = 0; i < 300; i++)
        {
            var an = (a + b).Half();
            var bn = BigFloat.Sqrt(a * b);
            a = an;
            b = bn;
            var d = a - b;
            if (d.IsZero || a.Mag - d.Mag > BigFloat.Precision) return (a + b).Half();
        }
        return (a + b).Half();
    }
}
