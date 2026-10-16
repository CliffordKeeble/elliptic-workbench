using System.Globalization;
using System.Numerics;

namespace Elliptic.Numerics;

/// <summary>
/// Arbitrary-precision binary floating point: value = Mantissa * 2^Exponent.
/// BCL-only (System.Numerics.BigInteger). Truncating arithmetic with guard bits;
/// not IEEE-correctly-rounded. Working precision is set via <see cref="Precision"/>
/// (bits) before any computation; the Pi cache in <see cref="Reals"/> is keyed on it.
/// </summary>
public readonly struct BigFloat : IComparable<BigFloat>, IEquatable<BigFloat>
{
    /// <summary>Working precision in bits (~0.301 * bits decimal digits). Default 320 bits ≈ 96 digits.</summary>
    /// <remarks>
    /// One process-wide setting, by design: set it once, before use. No caller computes at two
    /// precisions on two threads at once; a per-thread or per-call precision would change the
    /// class's contract; and the class has a twin in another repository (Icosian.Numerics) that
    /// must stay in step with this one.
    /// <para>
    /// Must be in [64, 1,048,576] bits, else
    /// <see cref="ArgumentOutOfRangeException"/>. The floor: below about 64 bits
    /// <see cref="ToDecimalString"/> and the guard-bit arithmetic stop meaning anything. The
    /// ceiling: <c>Precision + Guard</c>, and <c>2L · (Precision + Guard)</c> in
    /// <see cref="Sqrt"/> and the addition cut-off, must not overflow.
    /// </para>
    /// </remarks>
    public static int Precision
    {
        get => _precision;
        set
        {
            if (value < MinPrecision || value > MaxPrecision)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    $"Precision must be in [{MinPrecision}, {MaxPrecision:N0}] bits.");
            _precision = value;
        }
    }

    private const int MinPrecision = 64;
    private const int MaxPrecision = 1_048_576;   // 2^20

    private static int _precision = 320;

    private const int Guard = 32;

    // With |x| < 1/4 a Taylor term shrinks by about 2^(2 + log2 n) per step, so this cap covers a
    // working precision of about 3 x 10^5 bits. Past that Exp throws instead of returning short.
    private const int MaxExpTerms = 20000;

    public BigInteger Mantissa { get; }
    public int Exponent { get; }

    private BigFloat(BigInteger mantissa, int exponent)
    {
        Mantissa = mantissa;
        Exponent = exponent;
    }

    public static readonly BigFloat Zero = new(BigInteger.Zero, 0);
    public static readonly BigFloat One = new(BigInteger.One, 0);

    public int Sign => Mantissa.Sign;
    public bool IsZero => Mantissa.IsZero;

    /// <summary>Approximate magnitude: floor(log2 |x|) + 1. long.MinValue for zero.</summary>
    public long Mag => IsZero ? long.MinValue : Exponent + MagBits(Mantissa);

    private static long MagBits(BigInteger m) => m.IsZero ? 0 : BigInteger.Abs(m).GetBitLength();

    /// <summary>Sign-magnitude right shift (BigInteger &gt;&gt; floors negatives; we truncate toward zero).</summary>
    private static BigInteger Shr(BigInteger m, int k)
    {
        if (k <= 0) return m << (-k);
        var a = BigInteger.Abs(m) >> k;
        return m.Sign < 0 ? -a : a;
    }

    public static BigFloat Normalize(BigInteger m, long e)
    {
        if (m.IsZero) return Zero;
        long excess = MagBits(m) - (Precision + Guard);
        if (excess > 0)
        {
            m = Shr(m, (int)excess);
            e += excess;
        }
        if (e > int.MaxValue || e < int.MinValue) throw new OverflowException("BigFloat exponent out of range.");
        return new BigFloat(m, (int)e);
    }

    public static BigFloat From(BigInteger v) => Normalize(v, 0);
    public static BigFloat From(long v) => Normalize(v, 0);
    public static BigFloat FromRatio(BigInteger num, BigInteger den) => From(num) / From(den);

    public static BigFloat operator -(BigFloat a) => new(-a.Mantissa, a.Exponent);

    public static BigFloat operator +(BigFloat a, BigFloat b)
    {
        if (a.IsZero) return b;
        if (b.IsZero) return a;
        if (a.Exponent < b.Exponent) (a, b) = (b, a);   // a now has the larger exponent
        long diff = (long)a.Exponent - b.Exponent;
        // Normalized mantissas carry <= Precision+Guard bits, so an exponent gap this large
        // puts b entirely below a's last guard bit.
        if (diff > 2L * (Precision + Guard) + 8) return a;
        var m = (a.Mantissa << (int)diff) + b.Mantissa;
        return Normalize(m, b.Exponent);
    }

    public static BigFloat operator -(BigFloat a, BigFloat b) => a + (-b);

    public static BigFloat operator *(BigFloat a, BigFloat b)
        => Normalize(a.Mantissa * b.Mantissa, (long)a.Exponent + b.Exponent);

    public static BigFloat operator *(BigFloat a, long n)
        => Normalize(a.Mantissa * n, a.Exponent);

    public static BigFloat operator /(BigFloat a, BigFloat b)
    {
        if (b.IsZero) throw new DivideByZeroException("BigFloat division by zero.");
        if (a.IsZero) return Zero;
        int shift = Precision + Guard + (int)Math.Max(0, MagBits(b.Mantissa) - MagBits(a.Mantissa)) + 8;
        var q = BigInteger.Divide(a.Mantissa << shift, b.Mantissa);
        return Normalize(q, (long)a.Exponent - b.Exponent - shift);
    }

    // Through the BigFloat divide, whose shift grows to cover the numerator's width. A fixed
    // shift left a short numerator with ~100 bits of quotient at any Precision.
    public static BigFloat operator /(BigFloat a, long n) => a / From(n);

    /// <summary>Exact halving / doubling (exponent shift only).</summary>
    public BigFloat Half() => IsZero ? this : new BigFloat(Mantissa, Exponent - 1);
    public BigFloat Twice() => IsZero ? this : new BigFloat(Mantissa, Exponent + 1);
    public BigFloat Abs() => Sign < 0 ? -this : this;

    public int CompareTo(BigFloat other) => (this - other).Sign;

    // Equality is ordering's equality. Mantissa/Exponent is not canonical ((2, 0) and (1, 1) are
    // one value), so the default struct equality, which compares the fields, disagreed with
    // CompareTo. The hash uses the canonical form: trailing zero bits moved into the exponent.
    public bool Equals(BigFloat other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is BigFloat other && Equals(other);
    public static bool operator ==(BigFloat a, BigFloat b) => a.Equals(b);
    public static bool operator !=(BigFloat a, BigFloat b) => !a.Equals(b);

    public override int GetHashCode()
    {
        if (IsZero) return 0;
        int tz = (int)BigInteger.TrailingZeroCount(Mantissa);
        return HashCode.Combine(Mantissa >> tz, (long)Exponent + tz);
    }
    public static bool operator <(BigFloat a, BigFloat b) => (a - b).Sign < 0;
    public static bool operator >(BigFloat a, BigFloat b) => (a - b).Sign > 0;
    public static bool operator <=(BigFloat a, BigFloat b) => (a - b).Sign <= 0;
    public static bool operator >=(BigFloat a, BigFloat b) => (a - b).Sign >= 0;

    private static BigInteger ISqrt(BigInteger n)
    {
        if (n.Sign < 0) throw new ArithmeticException("ISqrt of negative.");
        if (n < 2) return n;
        var x = BigInteger.One << (int)((n.GetBitLength() + 2) / 2);
        while (true)
        {
            var y = (x + n / x) >> 1;
            if (y >= x) return x;
            x = y;
        }
    }

    public static BigFloat Sqrt(BigFloat a)
    {
        if (a.Sign < 0) throw new ArithmeticException("Sqrt of negative BigFloat.");
        if (a.IsZero) return Zero;
        long targetBits = 2L * (Precision + Guard) + 4;
        long t = targetBits - MagBits(a.Mantissa);
        long e = (long)a.Exponent - t;
        if ((e & 1) != 0) { t++; e--; }
        BigInteger n = t >= 0 ? a.Mantissa << (int)t : Shr(a.Mantissa, (int)(-t));
        return Normalize(ISqrt(n), e / 2);
    }

    /// <summary>exp(x) via range reduction (repeated halving to |x| &lt; 1/4) + Taylor + squaring.</summary>
    public static BigFloat Exp(BigFloat x)
    {
        if (x.IsZero) return One;
        int s = 0;
        while (x.Mag > -2)                                   // |x| >= 1/4  (Mag ≈ floor(log2)+1)
        {
            x = x.Half();
            s++;
            if (s > 200) throw new ArithmeticException("Exp: argument magnitude too large.");
        }
        var sum = One;
        var term = One;
        bool converged = false;
        for (long n = 1; n <= MaxExpTerms; n++)
        {
            term = term * x / n;
            if (term.IsZero) { converged = true; break; }
            sum += term;
            if (sum.Mag - term.Mag > Precision + Guard) { converged = true; break; }
        }
        if (!converged) throw new ArithmeticException("Exp: Taylor series did not converge within the term cap.");
        for (int i = 0; i < s; i++) sum *= sum;
        return sum;
    }

    public double ToDouble()
    {
        if (IsZero) return 0.0;
        var m = BigInteger.Abs(Mantissa);
        int k = (int)Math.Max(0, m.GetBitLength() - 53);
        double top = (double)(long)(m >> k);
        return Math.ScaleB(Sign < 0 ? -top : top, (int)(Exponent + (long)k));
    }

    /// <summary>Fixed-point decimal rendering with <paramref name="fracDigits"/> digits after the point (round half up).</summary>
    public string ToDecimalString(int fracDigits)
    {
        if (IsZero) return "0." + new string('0', fracDigits);
        var pow = BigInteger.Pow(10, fracDigits);
        var scaled = BigInteger.Abs(Mantissa) * pow;
        BigInteger i = Exponent >= 0
            ? scaled << Exponent
            : (scaled + (BigInteger.One << (-Exponent - 1))) >> (-Exponent);
        var ip = i / pow;
        var fp = i % pow;
        return (Sign < 0 ? "-" : "") + ip + "." + fp.ToString(CultureInfo.InvariantCulture).PadLeft(fracDigits, '0');
    }

    public override string ToString() => ToDecimalString(Math.Max(1, (int)(Precision * 0.30103) - 4));
}
