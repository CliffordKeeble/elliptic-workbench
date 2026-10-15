using System.Collections.Concurrent;

namespace Elliptic.Numerics;

/// <summary>
/// Real-analytic building blocks over <see cref="BigFloat"/>: π (Brent–Salamin, cached per
/// precision) and the arithmetic-geometric mean. Both converge quadratically. BCL-only.
/// </summary>
public static class Reals
{
    // Safe to call from several threads. BigFloat.Precision itself stays one process-wide setting.
    private static readonly ConcurrentDictionary<int, BigFloat> PiCache = new();

    // AGM converges quadratically: about log2(Precision) rounds. 300 is far past any precision a
    // BigFloat can carry, so reaching it means the iteration is not converging.
    private const int MaxAgmRounds = 300;

    /// <summary>π at the current working precision, via the Brent–Salamin (Gauss–Legendre) iteration.</summary>
    public static BigFloat Pi() => PiCache.GetOrAdd(BigFloat.Precision, ComputePi);

    private static BigFloat ComputePi(int p)
    {
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
        return (s * s) / (t * 4L);
    }

    /// <summary>AGM(a, b) for positive a, b.</summary>
    public static BigFloat Agm(BigFloat a, BigFloat b)
    {
        if (a.Sign <= 0 || b.Sign <= 0) throw new ArithmeticException("Agm requires positive arguments.");
        for (int i = 0; i < MaxAgmRounds; i++)
        {
            var an = (a + b).Half();
            var bn = BigFloat.Sqrt(a * b);
            a = an;
            b = bn;
            var d = a - b;
            if (d.IsZero || a.Mag - d.Mag > BigFloat.Precision) return (a + b).Half();
        }
        throw new ArithmeticException("Agm: did not converge within the round cap.");
    }
}
