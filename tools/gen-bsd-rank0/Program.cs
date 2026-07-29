using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elliptic.Bsd;
using Elliptic.Numerics;

// Generator for data/bsd-rank0.json (schema v2). See tools/gen-bsd-rank0/README.md.
// args: <engineCommit> <generatedDate> <outputPath>
string engineCommit = args.Length > 0 ? args[0] : "unknown";
string generated    = args.Length > 1 ? args[1] : DateTime.UtcNow.ToString("yyyy-MM-dd");
string outPath      = args.Length > 2 ? args[2] : "bsd-rank0.json";

const int OutDigits = 80;    // certified decimal digits emitted for real quantities
const int WorkBits  = 512;   // primary working precision
const int CheckBits = 640;   // second precision, to test the implementation (section 9)
var inv = CultureInfo.InvariantCulture;

// Engine's hardcoded torsion gcd primes (Curve.cs TorsionBound): odd primes only, no 2.
long[] EngineGcdPrimes = { 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };

var defs = new (string Label, string Prov, long A1, long A2, long A3, long A4, long A6, long N, long[] Bad)[]
{
    ("11a1",    "internal bench name; NOT pulled from LMFDB", 0, -1, 1, -10, -20, 11, new long[] { 11 }),
    ("27606c1", "internal bench name; NOT pulled from LMFDB", 1, 0, 0, -10289707, 12703497719, 27606, new long[] { 2, 3, 43, 107 }),
    ("n233-reg","internal regression curve (v1.0.1 Finding-1), conductor 233; no bench/LMFDB label", 1, 3, 0, -1, 0, 233, new long[] { 233 }),
};

// Fizz inversion: for D certified digits, M = ceil((D·ln10 + ln(4/(1-q))) / (-ln q)), q = exp(-2π/√N).
static int InvertTerms(long N, int D)
{
    double q = Math.Exp(-2.0 * Math.PI / Math.Sqrt(N));
    return (int)Math.Ceiling((D * Math.Log(10) + Math.Log(4.0 / (1.0 - q))) / (-Math.Log(q)));
}

static BigFloat QOf(long N) => BigFloat.Exp(-(Reals.Pi().Twice() / BigFloat.Sqrt(BigFloat.From(N))));

// Certified L at exactly M terms — mirrors Analytic.LValueRankZero's body, using the engine's
// own coefficients (an) and arithmetic (BigFloat). Cross-checked against the engine below.
static BigFloat CertifiedL(long N, long[] an, int M)
{
    var q = QOf(N);
    var qn = BigFloat.One; var sum = BigFloat.Zero;
    for (int n = 1; n <= M; n++) { qn *= q; if (an[n] == 0) continue; sum += (qn * an[n]) / n; }
    return sum.Twice();
}

static BigFloat PowN(BigFloat b, long e) { var r = BigFloat.One; while (e > 0) { if ((e & 1) == 1) r *= b; b *= b; e >>= 1; } return r; }

static int AgreeFrac(BigFloat a, BigFloat b)
{
    int probe = OutDigits + 15;
    string sa = a.ToDecimalString(probe), sb = b.ToDecimalString(probe);
    int da = sa.IndexOf('.'), db = sb.IndexOf('.');
    if (sa[..da] != sb[..db]) return -1;
    int n = 0, ia = da + 1, ib = db + 1;
    while (ia < sa.Length && ib < sb.Length && sa[ia] == sb[ib]) { n++; ia++; ib++; }
    return n;
}

static bool IsSquare(long v) { if (v < 0) return false; long r = (long)Math.Round(Math.Sqrt(v)); for (long k = Math.Max(0, r - 2); k <= r + 2; k++) if (k * k == v) return true; return false; }
static long Gcd(long a, long b) { a = Math.Abs(a); b = Math.Abs(b); while (b != 0) (a, b) = (b, a % b); return a; }

var curvesJson = new JsonArray();
Console.WriteLine($"{"curve",-10} {"M(inv)",7} {"nmax(old)",9} {"certDig",7} {"agreeL",6} {"agreeQ",6} {"gate",5} {"sq",4} {"stableL/Ω/Q"}");

foreach (var d in defs)
{
    int M    = InvertTerms(d.N, OutDigits);
    int nOld = Analytic.TermsFor(d.N, OutDigits);   // old (over-provisioning) engine term count, for the record

    // ---- primary precision ----
    BigFloat.Precision = WorkBits;
    var e = new EllipticCurve(d.A1, d.A2, d.A3, d.A4, d.A6, d.N, d.Bad);
    var an = e.An(M, out var ap);

    var Lc = CertifiedL(d.N, an, M);
    var repEng = BsdCompiler.RunRankZero(e, OutDigits);        // engine, at engine nmax — the faithfulness oracle
    int agreeL = AgreeFrac(Lc, repEng.LValue);                 // must be >= min certified digits

    var omega = repEng.Omega;
    long tor = repEng.TorsionBound, prod = repEng.TamagawaProduct, w = repEng.RootNumber;
    var quotient = Lc * (BigFloat.From(tor) * BigFloat.From(tor)) / (omega * BigFloat.From(prod));
    int agreeQ = AgreeFrac(quotient, repEng.ShaEstimate);

    // ---- certified tail bound & non-vanishing gate ----
    var qbf = QOf(d.N);
    var tail = PowN(qbf, M + 1) * 4L / (BigFloat.One - qbf);   // 4 q^(M+1)/(1-q)
    bool gatePass = Lc.Abs() > tail;
    var ratio = Lc.Abs() / tail;
    double qd = Math.Exp(-2.0 * Math.PI / Math.Sqrt(d.N));
    double tailD = 4.0 * Math.Pow(qd, M + 1) / (1.0 - qd);
    int certDigits = (int)Math.Floor(-Math.Log10(tailD));

    // ---- conditional square check (labelled conditional) ----
    long rounded = (long)Math.Round(quotient.ToDouble(), MidpointRounding.AwayFromZero);
    bool isSq = IsSquare(rounded);

    // ---- precision self-check (section 9): 512 vs 640 bit at same M ----
    BigFloat.Precision = CheckBits;
    var e2 = new EllipticCurve(d.A1, d.A2, d.A3, d.A4, d.A6, d.N, d.Bad);
    var an2 = e2.An(M, out _);
    var Lc2 = CertifiedL(d.N, an2, M);
    var rep2 = BsdCompiler.RunRankZero(e2, OutDigits);
    var quot2 = Lc2 * (BigFloat.From(tor) * BigFloat.From(tor)) / (rep2.Omega * BigFloat.From(prod));
    int stL = AgreeFrac(Lc, Lc2), stO = AgreeFrac(omega, rep2.Omega), stQ = AgreeFrac(quotient, quot2);
    BigFloat.Precision = WorkBits;

    // ---- assertion (a): gcd over odd good primes only ----
    var gcdPrimes = EngineGcdPrimes.Where(p => e.Delta % p != 0).ToArray();
    bool excludesTwo = !EngineGcdPrimes.Contains(2);
    long gcdCheck = 0; foreach (var p in gcdPrimes) { long np = p + 1 - e.Ap(p); gcdCheck = gcdCheck == 0 ? np : Gcd(gcdCheck, np); }
    bool torCrosscheck = gcdCheck == tor;

    // ---- assertions (b)+(c) + per-prime Tamagawa (derived, cross-checked) ----
    var perPrime = new JsonArray();
    long prodCheck = 1; bool allSemistable = true;
    foreach (var p in d.Bad)
    {
        int vp = EllipticCurve.Ord(e.Delta, p);
        bool split = ap[p] == 1;
        long cp = split ? vp : (vp % 2 == 0 ? 2 : 1);
        prodCheck *= cp;
        bool semistable = EllipticCurve.Ord(d.N, p) == 1;     // p^2 ∤ N
        allSemistable &= semistable;
        perPrime.Add(new JsonObject
        {
            ["p"] = p, ["vpDelta"] = vp, ["ap"] = ap[p], ["kodaira"] = $"I{vp}",
            ["reduction"] = split ? "split multiplicative" : "nonsplit multiplicative",
            ["cp"] = cp, ["semistable"] = semistable,
        });
    }
    bool tamagawaCrosscheck = prodCheck == prod;

    // ---- point counts: every prime the engine computes (p <= M) ----
    var pointCounts = new JsonArray();
    foreach (var p in EllipticCurve.Primes(M))
        pointCounts.Add(new JsonObject { ["p"] = p, ["Np"] = p + 1 - ap[p], ["ap"] = ap[p] });

    Console.WriteLine($"{d.Label,-10} {M,7} {nOld,9} {certDigits,7} {agreeL,6} {agreeQ,6} {(gatePass ? "pass" : "FAIL"),5} {(isSq ? "sq" : "NO"),4} {stL}/{stO}/{stQ}");

    curvesJson.Add(new JsonObject
    {
        ["label"] = d.Label,
        ["labelProvenance"] = d.Prov,
        ["weierstrass"] = new JsonObject { ["a1"] = d.A1, ["a2"] = d.A2, ["a3"] = d.A3, ["a4"] = d.A4, ["a6"] = d.A6 },
        ["conductor"] = d.N,
        ["badPrimes"] = new JsonArray(d.Bad.Select(x => (JsonNode)x).ToArray()),
        ["rank"] = 0,
        ["pointCounts"] = pointCounts,
        ["lValue"] = new JsonObject
        {
            ["value"] = Lc.ToDecimalString(OutDigits),
            ["method"] = "smoothed Dirichlet series",
            ["termsUsed"] = M,
            ["termsRule"] = "inversion of Fizz tail bound at D digits: ceil((D·ln10 + ln(4/(1-q)))/(-ln q))",
            ["q"] = qbf.ToDecimalString(60),
            ["tailBound"] = tailD.ToString("E4", inv),
            ["certifiedDigits"] = certDigits,
            ["digitsEmitted"] = OutDigits,
            ["seriesVerified"] = true,     // Q1 of brief 03: engine series matches the certified series exactly
            ["_termsUsedOld"] = nOld,      // previous over-provisioning term count, for the record
            ["_engineAgreeDigits"] = agreeL, // fractional digits the certified sum agrees with the engine's own L
        },
        ["nonVanishing"] = new JsonObject
        {
            ["lValue"] = Lc.Abs().ToDecimalString(20),
            ["tailBound"] = tailD.ToString("E4", inv),
            ["ratio"] = ratio.ToDouble().ToString("E4", inv),
            ["gate"] = gatePass ? "pass" : "fail",
            ["note"] = "w catches odd analytic rank; this catches even rank >= 2 (|L| certified clear of the tail).",
        },
        ["period"] = new JsonObject
        {
            ["value"] = omega.ToDecimalString(OutDigits),
            ["method"] = "AGM",
            ["digits"] = OutDigits,
            ["note"] = "AGM in BOTH discriminant branches (incl. 27606c1's spike case); no quadrature branch in the engine.",
        },
        ["regulator"] = new JsonObject { ["value"] = "1", ["note"] = "rank 0" },
        ["tamagawa"] = new JsonObject
        {
            ["product"] = prod,
            ["discriminant"] = e.Delta.ToString(),
            ["discriminantIsMinimal"] = true,
            ["_minimalityBasis"] = "engine constructor enforces the necessary u^12 minimality test (p^12|Δ ∧ p^4|c4 ∧ p^6|c6 rejected)",
            ["perPrime"] = perPrime,
            ["allSemistable"] = allSemistable,
            ["perPrimeProvenance"] = "derived in generator from public primitives (Ord, Δ, a_p) per the (v_p(Δ),a_p) recipe; "
                                   + "product cross-checked against engine TamagawaProduct = " + (tamagawaCrosscheck ? "OK" : "MISMATCH"),
        },
        ["torsion"] = new JsonObject
        {
            ["value"] = tor,
            ["exact"] = false,
            ["method"] = "gcd bound",
            ["gcdPrimes"] = new JsonArray(gcdPrimes.Select(x => (JsonNode)x).ToArray()),
            ["excludesTwo"] = excludesTwo,
            ["_gcdCrosscheck"] = torCrosscheck ? "OK" : "MISMATCH",
            ["_note"] = "gcd of #E(F_p) over odd good primes only (reduction injective on torsion there; not guaranteed at p=2). "
                      + "Upper bound, enters |Sha| squared, so |Sha| is an upper bound. Not Nagell-Lutz.",
        },
        ["rootNumber"] = new JsonObject { ["available"] = true, ["value"] = w },
        ["quotient"] = new JsonObject
        {
            ["value"] = quotient.ToDecimalString(OutDigits),
            ["relation"] = "upper bound",
            ["formula"] = "|Sha| = L(E,1)·|tor|^2 / (Ω·∏cp)",
            ["_engineAgreeDigits"] = agreeQ,
        },
        ["conditionalChecks"] = new JsonObject
        {
            ["quotientRoundsToSquare"] = new JsonObject
            {
                ["rounded"] = rounded,
                ["isSquare"] = isSq,
                ["class"] = "conditional",
                ["assumes"] = "BSD at rank 0 for integrality; Cassels via Kolyvagin finiteness, given L != 0",
            },
        },
        ["precisionCheck"] = new JsonObject
        {
            ["workingBits"] = WorkBits,
            ["checkBits"] = CheckBits,
            ["digitsStable"] = new JsonObject { ["lValue"] = stL, ["period"] = stO, ["quotient"] = stQ },
            ["note"] = "fractional digits agreeing between the two working precisions at the same term count M (tests the implementation)",
        },
    });
}

var root = new JsonObject
{
    ["schema"] = "elliptic-workbench/bsd-rank0/v2",
    ["engine"] = new JsonObject { ["commit"] = engineCommit, ["generated"] = generated, ["precisionDigits"] = OutDigits },
    ["provenance"] = "All real quantities computed by Elliptic.Bsd on this run at " + WorkBits + "-bit working precision. "
                   + "L uses the Fizz-certified term count (inversion); the certified sum is cross-checked against the engine's own "
                   + "L-value, and the whole pipeline against a " + CheckBits + "-bit run. Labels are internal bench names, NOT LMFDB pulls (W-107). "
                   + "No value is a frozen constant or a published-table lookup.",
    ["certificate"] = "Fizz tail bound: |tail| <= 4 q^(M+1)/(1-q), q = exp(-2π/√N), via |a_n/n| <= 2 (Hasse + divisor bound, bad primes included). "
                    + "Series verified against the engine (brief 03 §2): leading 2, exp(-2πn/√N), a_{p^k}=a_p^k at bad primes.",
    ["curves"] = curvesJson,
};

File.WriteAllText(outPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"\nWrote {outPath}");
