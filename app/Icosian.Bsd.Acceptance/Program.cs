using Icosian.Bsd;
using Icosian.Numerics;

// ─────────────────────────────────────────────────────────────────────────────
// Icosian.Bsd acceptance suite (v1, rank-0 channel + period cross-checks).
// Reference constants are the frozen 3 July 2026 bench values (BSD Arc Repair
// Manifest, Appendix), themselves cross-validated by independent methods.
// They are oracles-after: nothing here feeds the pipeline.
// ─────────────────────────────────────────────────────────────────────────────

BigFloat.Precision = 384;
int pass = 0, fail = 0;

void Check(string name, string got, string wantPrefix)
{
    bool ok = got.StartsWith(wantPrefix, StringComparison.Ordinal);
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}");
    Console.WriteLine($"         got  {got}");
    if (!ok) Console.WriteLine($"         want {wantPrefix}…");
    if (ok) pass++; else fail++;
}

void CheckLong(string name, long got, long want)
{
    bool ok = got == want;
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}: got {got}, want {want}");
    if (ok) pass++; else fail++;
}

Console.WriteLine("Icosian.Bsd v1 acceptance — rank-0 channel\n");

// ── 11a1 = [0,−1,1,−10,−20], N = 11 — the boot curve (Paper 141) ────────────
Console.WriteLine("11a1  y² + y = x³ − x² − 10x − 20");
var e11 = new EllipticCurve(0, -1, 1, -10, -20, 11, new long[] { 11 });
var r11 = BsdCompiler.RunRankZero(e11, digits: 34);
CheckLong("root number ε", r11.RootNumber, +1);
CheckLong("∏c_p (the row 145 got wrong)", r11.TamagawaProduct, 5);
CheckLong("torsion bound", r11.TorsionBound, 5);
Check("Ω", r11.Omega.ToDecimalString(30), "1.26920930427955342168879461675");
Check("L(E,1)", r11.LValue.ToDecimalString(20), "0.253841860855910");
Check("|Sha| (rank 0)", r11.ShaEstimate.ToDecimalString(24), "1.0000000000000000000");
Console.WriteLine();

// ── 27606c1 = [1,0,0,−10289707,12703497719] — the |Sha| = 4 curve (Paper 144) ─
Console.WriteLine("27606c1  y² + xy = x³ − 10289707x + 12703497719");
var e27 = new EllipticCurve(1, 0, 0, -10289707, 12703497719, 27606, new long[] { 2, 3, 43, 107 });
var r27 = BsdCompiler.RunRankZero(e27, digits: 34);
CheckLong("root number ε", r27.RootNumber, +1);
CheckLong("∏c_p", r27.TamagawaProduct, 3);
CheckLong("torsion bound", r27.TorsionBound, 1);
Check("Ω (spike case, AGM — 30 digits, no quadrature)",
      r27.Omega.ToDecimalString(31), "0.538085890979675477333935451400");
Check("L(E,1)", r27.LValue.ToDecimalString(20), "6.45703069175610");
Check("|Sha| = 4 = 2²", r27.ShaEstimate.ToDecimalString(24), "4.0000000000000000000");
Console.WriteLine();

// ── Period-only cross-checks, Δ > 0 branch (v2 curves) ──────────────────────
Console.WriteLine("Δ > 0 period branch (rank ≥ 1 curves; L-machinery is v2)");
var e37 = new EllipticCurve(0, 0, 1, -1, 0, 37, new long[] { 37 });
Check("Ω(37a1)", Analytic.RealPeriod(e37).ToDecimalString(28), "5.986917292463919259664019958");
var e389 = new EllipticCurve(0, 1, 1, -2, 0, 389, new long[] { 389 });
Check("Ω(389a1) — the corrected value (142 §2.4 erratum)",
      Analytic.RealPeriod(e389).ToDecimalString(28), "4.980425121710110150642715583");
Console.WriteLine();

// ── v1.0.1 regression — Finding 1 (Mr Code adversarial review, 3 July 2026) ─
// Prime conductor exceeding the series bound: nmax(233, digits=30) = 232 < 233.
// v1.0 threw KeyNotFoundException here; the fix seeds a_p for all conductor primes.
Console.WriteLine("v1.0.1 regression: bad prime beyond the series bound (N = 233 = Δ)");
var e233 = new EllipticCurve(1, 3, 0, -1, 0, 233, new long[] { 233 });
var r233 = BsdCompiler.RunRankZero(e233, digits: 30);
CheckLong("root number ε", r233.RootNumber, +1);
CheckLong("∏c_p", r233.TamagawaProduct, 1);
CheckLong("torsion bound (bound, not certified — see Finding 2)", r233.TorsionBound, 2);
Check("|Sha| (rank 0)", r233.ShaEstimate.ToDecimalString(20), "1.000000000000000");
Console.WriteLine();

Console.WriteLine($"{pass} passed, {fail} failed.");
return fail == 0 ? 0 : 1;
