using System.Numerics;
using Elliptic.Bsd;
using Elliptic.Numerics;

// ─────────────────────────────────────────────────────────────────────────────
// Elliptic.Bsd acceptance suite (v1, rank-0 channel + period cross-checks).
//
// Reference constants are PUBLISHED VALUES from LMFDB, accessed 2026-07-30 via the
// LMFDB API (ec_curvedata + ec_mwbsd). Each curve cites its LMFDB label; the
// citation is the curve page https://www.lmfdb.org/EllipticCurve/Q/<Cremona>/.
// Provenance and per-constant deltas: app/Elliptic.Bsd.Acceptance/acceptance-constants.md.
//
// Tolerance rule (pinned, acceptance-constants.md §"Tolerance rule"):
//   integers exact; decimals at LMFDB's published precision, ½ unit in the last
//   place, compared in decimal (scaled BigInteger, no binary-double round trip).
//   Root number = sign of functional equation = (−1)^analytic_rank.
//   Torsion: engine bound compared to published ORDER (tight ⇒ equal on these five).
// ─────────────────────────────────────────────────────────────────────────────

BigFloat.Precision = 384;
int pass = 0, fail = 0;

void CheckLong(string name, long got, long want, string src)
{
    bool ok = got == want;
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}: got {got}, want {want}  [{src}]");
    if (ok) pass++; else fail++;
}

// Decimal comparison per the tolerance rule: engine value rendered to the published
// number of places, compared to LMFDB's decimal string as scaled integers, ½ ulp.
void CheckDecimal(string name, BigFloat got, string lmfdb, string src)
{
    int dot = lmfdb.IndexOf('.');
    int d = dot >= 0 ? lmfdb.Length - dot - 1 : 0;         // published fractional places
    int hi = d + 12;                                       // extra guard from the engine
    BigInteger pub = Scaled(lmfdb, hi);
    BigInteger eng = Scaled(got.ToDecimalString(hi), hi);
    BigInteger halfUlp = 5 * BigInteger.Pow(10, hi - d - 1);
    BigInteger diff = BigInteger.Abs(eng - pub);
    bool ok = diff <= halfUlp;
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}  (@ {d} dp, ±½ulp)  [{src}]");
    Console.WriteLine($"         got  {got.ToDecimalString(d + 2)}");
    Console.WriteLine($"         want {lmfdb}");
    if (!ok) Console.WriteLine($"         DIFF {diff} × 10^-{hi}  exceeds ½ulp {halfUlp} × 10^-{hi}");
    if (ok) pass++; else fail++;
}

static BigInteger Scaled(string s, int frac)
{
    bool neg = s.StartsWith("-"); if (neg) s = s.Substring(1);
    int dot = s.IndexOf('.');
    string ip = dot < 0 ? s : s.Substring(0, dot);
    string fp = dot < 0 ? "" : s.Substring(dot + 1);
    fp = fp.Length >= frac ? fp.Substring(0, frac) : fp.PadRight(frac, '0');
    var v = BigInteger.Parse((ip.Length == 0 ? "0" : ip) + fp);
    return neg ? -v : v;
}

Console.WriteLine("Elliptic.Bsd v1 acceptance — rank-0 channel, constants validated against LMFDB\n");

// ── 11a1 = LMFDB 11.a2 = [0,−1,1,−10,−20], N = 11 ───────────────────────────
Console.WriteLine("11a1  (LMFDB 11.a2)  y² + y = x³ − x² − 10x − 20");
var e11 = new EllipticCurve(0, -1, 1, -10, -20, 11, new long[] { 11 });
var r11 = BsdCompiler.RunRankZero(e11, digits: 34);
CheckLong("root number ε (sign = (−1)^analytic_rank, ar=0)", r11.RootNumber, +1, "LMFDB 11.a2");
CheckLong("∏c_p (tamagawa_product)", r11.TamagawaProduct, 5, "LMFDB 11.a2");
CheckLong("torsion — bound = published order 5 (tight)", r11.TorsionBound, 5, "LMFDB 11.a2 order");
CheckDecimal("Ω (real_period)", r11.Omega, "1.2692093042795534216887946168", "LMFDB 11.a2");
CheckDecimal("L(E,1) (special_value)", r11.LValue, "0.25384186085591068433775892335043887465", "LMFDB 11.a2");
CheckDecimal("|Sha| analytic order (sha_an), estimate rounds to", r11.ShaEstimate, "1", "LMFDB 11.a2");
Console.WriteLine();

// ── 27606c1 = LMFDB 27606.c1 = [1,0,0,−10289707,12703497719] ─────────────────
Console.WriteLine("27606c1  (LMFDB 27606.c1)  y² + xy = x³ − 10289707x + 12703497719");
var e27 = new EllipticCurve(1, 0, 0, -10289707, 12703497719, 27606, new long[] { 2, 3, 43, 107 });
var r27 = BsdCompiler.RunRankZero(e27, digits: 34);
CheckLong("root number ε (ar=0)", r27.RootNumber, +1, "LMFDB 27606.c1");
CheckLong("∏c_p (tamagawa_product)", r27.TamagawaProduct, 3, "LMFDB 27606.c1");
CheckLong("torsion — bound = published order 1 (tight)", r27.TorsionBound, 1, "LMFDB 27606.c1 order");
CheckDecimal("Ω (real_period)", r27.Omega, "0.53808589097967547733393545140", "LMFDB 27606.c1");
CheckDecimal("L(E,1) (special_value)", r27.LValue, "6.4570306917561057280072254168078748568", "LMFDB 27606.c1");
CheckDecimal("|Sha| analytic order (sha_an), estimate rounds to", r27.ShaEstimate, "4", "LMFDB 27606.c1");
Console.WriteLine();

// ── Period-only cross-checks, Δ > 0 branch (rank ≥ 1) ───────────────────────
Console.WriteLine("Δ > 0 period branch (rank ≥ 1; L-machinery is rank-0 only)");
var e37 = new EllipticCurve(0, 0, 1, -1, 0, 37, new long[] { 37 });
CheckDecimal("Ω(37a1) (LMFDB 37.a1 real_period)", Analytic.RealPeriod(e37), "5.9869172924639192596640199589", "LMFDB 37.a1");
var e389 = new EllipticCurve(0, 1, 1, -2, 0, 389, new long[] { 389 });
CheckDecimal("Ω(389a1) (LMFDB 389.a1 real_period)", Analytic.RealPeriod(e389), "4.9804251217101101506427155839", "LMFDB 389.a1");
Console.WriteLine();

// ── n233 = LMFDB 233.a1 (Cremona 233a2); workbench model [1,3,0,−1,0] ────────
// Same curve as LMFDB's minimal [1,0,1,−5,3] by c4=217, c6=−3133, Δ=233 (invariant-level,
// two independent computations; direct a-invariant comparison n/a). Regression: bad prime = Δ.
Console.WriteLine("n233  (LMFDB 233.a1 / Cremona 233a2)  workbench model y² + xy = x³ + 3x² − x, N = 233");
var e233 = new EllipticCurve(1, 3, 0, -1, 0, 233, new long[] { 233 });
var r233 = BsdCompiler.RunRankZero(e233, digits: 30);
CheckLong("root number ε (ar=0)", r233.RootNumber, +1, "LMFDB 233.a1");
CheckLong("∏c_p (tamagawa_product)", r233.TamagawaProduct, 1, "LMFDB 233.a1");
CheckLong("torsion — bound = published order 2 (tight)", r233.TorsionBound, 2, "LMFDB 233.a1 order");
CheckDecimal("|Sha| analytic order (sha_an), estimate rounds to", r233.ShaEstimate, "1", "LMFDB 233.a1");
Console.WriteLine();

Console.WriteLine($"{pass} passed, {fail} failed.");
return fail == 0 ? 0 : 1;
