using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
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

void CheckBool(string name, bool ok, string detail)
{
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}  {detail}");
    if (ok) pass++; else fail++;
}

// Decimal comparison per the tolerance rule (with the 29-Jul amendment): compare at the
// source's SELF-CONSISTENT precision, ½ ulp, in decimal (scaled BigInteger, no double).
// atDp overrides the compare precision where a field's DISPLAY precision exceeds its
// self-consistent precision (special_value: displayed 38 dp, self-consistent ~28 dp,
// anchored by real_period's 28 dp — see acceptance-constants.md and the amendment commit).
void CheckDecimal(string name, BigFloat got, string lmfdb, string src, int atDp = -1)
{
    int dot = lmfdb.IndexOf('.');
    int shown = dot >= 0 ? lmfdb.Length - dot - 1 : 0;     // digits the source displays
    int d = atDp >= 0 ? atDp : shown;                      // compare precision
    int hi = d + 12;                                       // extra guard from the engine
    BigInteger pub = RoundScaled(lmfdb, d) * BigInteger.Pow(10, hi - d);  // source rounded to compare precision
    BigInteger eng = Scaled(got.ToDecimalString(hi), hi);
    BigInteger halfUlp = 5 * BigInteger.Pow(10, hi - d - 1);
    BigInteger diff = BigInteger.Abs(eng - pub);
    bool ok = diff <= halfUlp;
    string tag = (atDp >= 0 && atDp < shown) ? $"@ {d} dp (amended; display {shown} dp)" : $"@ {d} dp";
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}  {tag}, ±½ulp  [{src}]");
    Console.WriteLine($"         got  {got.ToDecimalString(Math.Min(shown, d) + 2)}");
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

// Round decimal string s to d fractional places (half up), returned as value × 10^d.
static BigInteger RoundScaled(string s, int d)
{
    var v = Scaled(s, d + 1);
    bool neg = v.Sign < 0; v = BigInteger.Abs(v);
    var q = BigInteger.DivRem(v, 10, out var rem);
    if (rem >= 5) q += 1;
    return neg ? -q : q;
}

Console.WriteLine("Elliptic.Bsd v1 acceptance — rank-0 channel, constants validated against LMFDB\n");

// ── Archive integrity — the LMFDB evidence base (follow-on item 1) ──────────
// Verify the archive BEFORE validating constants against it: each record's own label
// field equals the requested label (catches a wrong-curve record), every field a
// constant draws is present and non-null, and the content hash matches the manifest
// (catches a silent re-fetch or edit). A generic "parses as JSON" would pass all three.
Console.WriteLine("Archive integrity — LMFDB evidence base");
string lmfdbDir = Path.Combine(AppContext.BaseDirectory, "lmfdb");
var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(lmfdbDir, "manifest.json")))!;
var realLit = manifest["realLiteralFields"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();
foreach (var e in manifest["files"]!.AsArray())
{
    string file = e!["file"]!.GetValue<string>();
    string exp = e["expectedLabel"]!.GetValue<string>();
    var problems = new List<string>();
    byte[] bytes = File.ReadAllBytes(Path.Combine(lmfdbDir, file));
    if (Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() != e["sha256"]!.GetValue<string>())
        problems.Add("sha256 mismatch");
    JsonNode? rec = null;
    try { var data = JsonNode.Parse(bytes)!["data"]; rec = data is JsonArray arr ? arr[0] : data; }
    catch { problems.Add("not JSON"); }
    if (rec is null) problems.Add("no data record");
    else
    {
        string lf = e["labelField"]!.GetValue<string>();
        string? got = rec[lf]?.GetValue<string>();
        if (got != exp) problems.Add($"label {lf}={got ?? "absent"} != {exp}");
        foreach (var rfn in e["requiredFields"]!.AsArray())
        {
            string rf = rfn!.GetValue<string>();
            var v = rec[rf];
            if (v is null) problems.Add($"{rf} absent");
            else if (realLit.Contains(rf) && (v as JsonObject)?["data"] is null) problems.Add($"{rf}.data null");
        }
    }
    CheckBool($"archive {file} ({exp})", problems.Count == 0,
              problems.Count == 0 ? "label✓ fields✓ hash✓" : "FAIL: " + string.Join("; ", problems));
}
Console.WriteLine();

// ── Acceptance constants — each pinned to its archived source in the block below ──
// Declared once here, asserted equal to the archive next, then fed to the engine checks.
// 11a1 = LMFDB 11.a2
long root11 = +1, tam11 = 5, tor11 = 5;
string omega11 = "1.2692093042795534216887946168";
string lval11 = "0.25384186085591068433775892335043887465";
string sha11 = "1";
// 27606c1 = LMFDB 27606.c1
long root27 = +1, tam27 = 3, tor27 = 1;
string omega27 = "0.53808589097967547733393545140";
string lval27 = "6.4570306917561057280072254168078748568";
string sha27 = "4";
// 37a1 / 389a1 (period-only, Δ > 0 branch)
string omega37 = "5.9869172924639192596640199589";
string omega389 = "4.9804251217101101506427155839";
// n233 = LMFDB 233.a1 (Cremona 233a2)
long root233 = +1, tam233 = 1, tor233 = 2;
string sha233 = "1";

// ── Constants vs archive — the middle link (CinC follow-on) ─────────────────
// The engine checks below prove engine == constant; the integrity block above proves
// archive == manifest. Neither proves constant == archive — and if a constant were an
// oracle-after copied from the engine rather than from LMFDB, engine == constant would
// pass while proving nothing (W-107). This block pins each constant to its independent
// archived source field, so engine == constant == archive gives real engine == archive.
// The SAME variables asserted here are the ones fed to the engine comparison below.
Console.WriteLine("Constants vs archive — the middle link");
var recs = new Dictionary<string, JsonNode>();
JsonNode Rec(string file)
{
    if (!recs.TryGetValue(file, out var r))
    {
        var dn = JsonNode.Parse(File.ReadAllText(Path.Combine(lmfdbDir, file)))!["data"];
        r = (dn is JsonArray a ? a[0]! : dn)!;
        recs[file] = r;
    }
    return r;
}
long AInt(JsonNode rec, string field) => rec[field]!.GetValue<long>();
string ALit(JsonNode rec, string field) => rec[field]!["data"]!.GetValue<string>();
long RootOf(long ar) => ar % 2 == 0 ? +1 : -1;               // ε = (−1)^analytic_rank
void EqInt(string name, long c, long a) =>
    CheckBool(name, c == a, c == a ? $"{c} = archive {a}" : $"constant {c} != archive {a}");
void EqStr(string name, string c, string a) =>
    CheckBool(name, c == a, c == a ? $"= {a}" : $"constant {c} != archive {a}");
void EqSha(string name, string c, JsonNode mw)              // sha_an is stored as "N.000…0"
{
    string a = ALit(mw, "sha_an");
    bool ok = BigInteger.Parse(c) * BigInteger.Pow(10, 28) == Scaled(a, 28);
    CheckBool(name, ok, ok ? $"{c} = archive sha_an {a}" : $"constant {c} != archive sha_an {a}");
}
JsonNode? TryRec(string file) { try { return Rec(file); } catch { return null; } }

// Resolve a curve's two archive records from its Cremona label alone (Brief 06 §3). The
// Cremona↔LMFDB correspondence is taken from the ec_curvedata record's own lmfdb_label —
// NOT a hardcoded pairing, which would be a bench constant with no source (W-107 one level up,
// inside the artefact built to close it). Assert at BOTH hops that each record's own label
// matches the file it was opened by, so a mis-named file cannot pass the map off as sourced
// from a filename. This deliberately does not lean on the manifest's expectedLabel (hand-typed).
(JsonNode cd, JsonNode mw) MapCurve(string clabel)
{
    var cd = Rec($"ec_curvedata_{clabel}.json");             // presence integrity-guaranteed
    string cdLabel    = cd["Clabel"]!.GetValue<string>();
    string lmfdbLabel = cd["lmfdb_label"]!.GetValue<string>();
    var mw = TryRec($"ec_mwbsd_{lmfdbLabel}.json");
    string mwLabel = mw?["lmfdb_label"]?.GetValue<string>() ?? "(absent)";
    bool ok = cdLabel == clabel && mw is not null && mwLabel == lmfdbLabel;
    CheckBool($"map {clabel} → {lmfdbLabel} (Cremona↔LMFDB from the ec_curvedata record, not hardcoded)", ok,
        ok ? "curvedata Clabel✓, derived mwbsd lmfdb_label✓"
           : $"MISMATCH: curvedata Clabel={cdLabel} (want {clabel}); mwbsd for '{lmfdbLabel}' lmfdb_label={mwLabel}");
    return (cd, mw ?? cd);
}

var (cd11, mw11) = MapCurve("11a1");
EqInt("11a1 root ε ← (−1)^analytic_rank", root11, RootOf(AInt(cd11, "analytic_rank")));
EqInt("11a1 ∏c_p ← tamagawa_product",     tam11,   AInt(mw11, "tamagawa_product"));
EqInt("11a1 torsion ← curvedata torsion", tor11,   AInt(cd11, "torsion"));
EqStr("11a1 Ω ← real_period",             omega11, ALit(mw11, "real_period"));
EqStr("11a1 L(E,1) ← special_value",      lval11,  ALit(mw11, "special_value"));
EqSha("11a1 |Sha| ← sha_an",              sha11,   mw11);

var (cd27, mw27) = MapCurve("27606c1");
EqInt("27606c1 root ε ← (−1)^analytic_rank", root27, RootOf(AInt(cd27, "analytic_rank")));
EqInt("27606c1 ∏c_p ← tamagawa_product",     tam27,   AInt(mw27, "tamagawa_product"));
EqInt("27606c1 torsion ← curvedata torsion", tor27,   AInt(cd27, "torsion"));
EqStr("27606c1 Ω ← real_period",             omega27, ALit(mw27, "real_period"));
EqStr("27606c1 L(E,1) ← special_value",      lval27,  ALit(mw27, "special_value"));
EqSha("27606c1 |Sha| ← sha_an",              sha27,   mw27);

var (_, mw37) = MapCurve("37a1");
EqStr("37a1 Ω ← real_period",  omega37,  ALit(mw37, "real_period"));
var (_, mw389) = MapCurve("389a1");
EqStr("389a1 Ω ← real_period", omega389, ALit(mw389, "real_period"));

var (cd233, mw233) = MapCurve("233a2");
EqInt("n233 root ε ← (−1)^analytic_rank", root233, RootOf(AInt(cd233, "analytic_rank")));
EqInt("n233 ∏c_p ← tamagawa_product",     tam233,  AInt(mw233, "tamagawa_product"));
EqInt("n233 torsion ← curvedata torsion", tor233,  AInt(cd233, "torsion"));
EqSha("n233 |Sha| ← sha_an",              sha233,  mw233);
Console.WriteLine();

// ── 11a1 = LMFDB 11.a2 = [0,−1,1,−10,−20], N = 11 ───────────────────────────
Console.WriteLine("11a1  (LMFDB 11.a2)  y² + y = x³ − x² − 10x − 20");
var e11 = new EllipticCurve(0, -1, 1, -10, -20, 11, new long[] { 11 });
var r11 = BsdCompiler.RunRankZero(e11, digits: 34);
CheckLong("root number ε (sign = (−1)^analytic_rank, ar=0)", r11.RootNumber, root11, "LMFDB 11.a2");
CheckLong("∏c_p (tamagawa_product)", r11.TamagawaProduct, tam11, "LMFDB 11.a2");
CheckLong("torsion — bound = published order 5 (tight)", r11.TorsionBound, tor11, "LMFDB 11.a2 order");
CheckDecimal("Ω (real_period)", r11.Omega, omega11, "LMFDB 11.a2");
CheckDecimal("L(E,1) (special_value)", r11.LValue, lval11, "LMFDB 11.a2", atDp: 28);
CheckDecimal("|Sha| analytic order (sha_an), estimate rounds to", r11.ShaEstimate, sha11, "LMFDB 11.a2");
Console.WriteLine();

// ── 27606c1 = LMFDB 27606.c1 = [1,0,0,−10289707,12703497719] ─────────────────
Console.WriteLine("27606c1  (LMFDB 27606.c1)  y² + xy = x³ − 10289707x + 12703497719");
var e27 = new EllipticCurve(1, 0, 0, -10289707, 12703497719, 27606, new long[] { 2, 3, 43, 107 });
var r27 = BsdCompiler.RunRankZero(e27, digits: 34);
CheckLong("root number ε (ar=0)", r27.RootNumber, root27, "LMFDB 27606.c1");
CheckLong("∏c_p (tamagawa_product)", r27.TamagawaProduct, tam27, "LMFDB 27606.c1");
CheckLong("torsion — bound = published order 1 (tight)", r27.TorsionBound, tor27, "LMFDB 27606.c1 order");
CheckDecimal("Ω (real_period)", r27.Omega, omega27, "LMFDB 27606.c1");
CheckDecimal("L(E,1) (special_value)", r27.LValue, lval27, "LMFDB 27606.c1", atDp: 28);
CheckDecimal("|Sha| analytic order (sha_an), estimate rounds to", r27.ShaEstimate, sha27, "LMFDB 27606.c1");
Console.WriteLine();

// ── Period-only cross-checks, Δ > 0 branch (rank ≥ 1) ───────────────────────
Console.WriteLine("Δ > 0 period branch (rank ≥ 1; L-machinery is rank-0 only)");
var e37 = new EllipticCurve(0, 0, 1, -1, 0, 37, new long[] { 37 });
CheckDecimal("Ω(37a1) (LMFDB 37.a1 real_period)", Analytic.RealPeriod(e37), omega37, "LMFDB 37.a1");
var e389 = new EllipticCurve(0, 1, 1, -2, 0, 389, new long[] { 389 });
CheckDecimal("Ω(389a1) (LMFDB 389.a1 real_period)", Analytic.RealPeriod(e389), omega389, "LMFDB 389.a1");
Console.WriteLine();

// ── n233 = LMFDB 233.a1 (Cremona 233a2); workbench model [1,3,0,−1,0] ────────
// Same curve as LMFDB's minimal [1,0,1,−5,3] by c4=217, c6=−3133, Δ=233 (invariant-level,
// two independent computations; direct a-invariant comparison n/a). Regression: bad prime = Δ.
Console.WriteLine("n233  (LMFDB 233.a1 / Cremona 233a2)  workbench model y² + xy = x³ + 3x² − x, N = 233");
var e233 = new EllipticCurve(1, 3, 0, -1, 0, 233, new long[] { 233 });
var r233 = BsdCompiler.RunRankZero(e233, digits: 30);
CheckLong("root number ε (ar=0)", r233.RootNumber, root233, "LMFDB 233.a1");
CheckLong("∏c_p (tamagawa_product)", r233.TamagawaProduct, tam233, "LMFDB 233.a1");
CheckLong("torsion — bound = published order 2 (tight)", r233.TorsionBound, tor233, "LMFDB 233.a1 order");
CheckDecimal("|Sha| analytic order (sha_an), estimate rounds to", r233.ShaEstimate, sha233, "LMFDB 233.a1");
Console.WriteLine();

// ── 30a1 = LMFDB 30.a8 — torsion-bound benchmark (Brief 03 step 7) ───────────
// KNOWN DEFECT, LIVE. The engine's TorsionBound is a gcd upper bound, not the true order.
// Published torsion order (LMFDB 30.a8) = 6; the engine's gcd bound = 12 (overshoots by k=2).
// The trueOrder work added a DATA field; it did not cure the engine's TorsionBound. Documented
// under the known-defect convention: the checks assert the CURRENT (defective) engine values so a
// future Nagell–Lutz cure flips these lines visibly. The published order 6 is the target. The
// cure is parked (candidate future brief); this pass documents, it does not fix.
Console.WriteLine("30a1  (LMFDB 30.a8)  torsion-bound benchmark — KNOWN DEFECT (live)");
var e30 = new EllipticCurve(1, 0, 1, 1, 2, 30, new long[] { 2, 3, 5 });
var r30 = BsdCompiler.RunRankZero(e30, digits: 34);
CheckLong("KNOWN DEFECT — torsion: gcd bound overshoots the published order 6 (target)", r30.TorsionBound, 12, "engine gcd bound; LMFDB 30.a8 order = 6");
CheckDecimal("KNOWN DEFECT — |Sha| estimate inflated by (bound/order)² = 4 (analytic order is 1)", r30.ShaEstimate, "4", "engine estimate; LMFDB 30.a8 analytic order = 1", atDp: 0);
Console.WriteLine();

Console.WriteLine($"{pass} passed, {fail} failed.");
return fail == 0 ? 0 : 1;
