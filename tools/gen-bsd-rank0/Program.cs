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
// M is a FLOOR on the term count; using more terms only certifies more digits.
static int InvertTerms(long N, int D)
{
    double q = Math.Exp(-2.0 * Math.PI / Math.Sqrt(N));
    return (int)Math.Ceiling((D * Math.Log(10) + Math.Log(4.0 / (1.0 - q))) / (-Math.Log(q)));
}

// Smallest engine digit target whose own TermsFor clears the certified floor M (>= OutDigits emit).
static int ChooseDigits(long N, int M, int emit)
{
    int d = emit;
    while (Analytic.TermsFor(N, d) < M) d++;
    return d;
}

// Fizz tail bound at a given term count: 4 q^(nmax+1) / (1-q).
static double TailAt(long N, int nmax)
{
    double q = Math.Exp(-2.0 * Math.PI / Math.Sqrt(N));
    return 4.0 * Math.Pow(q, nmax + 1) / (1.0 - q);
}

static BigFloat QOf(long N) => BigFloat.Exp(-(Reals.Pi().Twice() / BigFloat.Sqrt(BigFloat.From(N))));

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
Console.WriteLine($"{"curve",-10} {"M(req)",7} {"digits",7} {"termsUsed",10} {"certDig",7} {"gate",5} {"sq",4} {"stableL/Ω/Q"}");

foreach (var d in defs)
{
    int M = InvertTerms(d.N, OutDigits);               // certified floor
    int digits = ChooseDigits(d.N, M, OutDigits);      // engine digit target that clears M
    int termsUsed = Analytic.TermsFor(d.N, digits);    // engine's actual term count (>= M)

    // ---- THE single engine call: every report value comes from here ----
    BigFloat.Precision = WorkBits;
    var e = new EllipticCurve(d.A1, d.A2, d.A3, d.A4, d.A6, d.N, d.Bad);
    var rep = BsdCompiler.RunRankZero(e, digits);
    long tor = rep.TorsionBound, prod = rep.TamagawaProduct, w = rep.RootNumber;

    // Engine coefficients (a_p) — read, for point counts and the per-prime Tamagawa derivation.
    _ = e.An(termsUsed, out var ap);

    // ---- certificate (generator-side; describes the engine's computation) ----
    var qbf = QOf(d.N);
    double tailD = TailAt(d.N, termsUsed);
    int certDigits = (int)Math.Floor(-Math.Log10(tailD));
    bool gatePass = rep.LValue.Abs().ToDouble() > tailD;
    double ratioD = rep.LValue.Abs().ToDouble() / tailD;

    // ---- conditional square check (labelled conditional) ----
    long rounded = (long)Math.Round(rep.ShaEstimate.ToDouble(), MidpointRounding.AwayFromZero);
    bool isSq = IsSquare(rounded);

    // ---- precision self-check (section 9): same call at 640 bits ----
    BigFloat.Precision = CheckBits;
    var e2 = new EllipticCurve(d.A1, d.A2, d.A3, d.A4, d.A6, d.N, d.Bad);
    var rep2 = BsdCompiler.RunRankZero(e2, digits);
    int stL = AgreeFrac(rep.LValue, rep2.LValue), stO = AgreeFrac(rep.Omega, rep2.Omega), stQ = AgreeFrac(rep.ShaEstimate, rep2.ShaEstimate);
    BigFloat.Precision = WorkBits;

    // ---- assertion (a): gcd over odd good primes only (derived; engine gives only the value) ----
    var gcdPrimes = EngineGcdPrimes.Where(p => e.Delta % p != 0).ToArray();
    bool excludesTwo = !EngineGcdPrimes.Contains(2);
    long gcdCheck = 0; foreach (var p in gcdPrimes) { long np = p + 1 - e.Ap(p); gcdCheck = gcdCheck == 0 ? np : Gcd(gcdCheck, np); }
    bool torCrosscheck = gcdCheck == tor;

    // ---- assertions (b)+(c) + per-prime Tamagawa (derived — engine gives only the product) ----
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

    // ---- point counts: every prime the engine computes (p <= termsUsed) ----
    var pointCounts = new JsonArray();
    foreach (var p in EllipticCurve.Primes(termsUsed))
        pointCounts.Add(new JsonObject { ["p"] = p, ["Np"] = p + 1 - ap[p], ["ap"] = ap[p] });

    Console.WriteLine($"{d.Label,-10} {M,7} {digits,7} {termsUsed,10} {certDigits,7} {(gatePass ? "pass" : "FAIL"),5} {(isSq ? "sq" : "NO"),4} {stL}/{stO}/{stQ}");

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
            ["value"] = rep.LValue.ToDecimalString(OutDigits),   // engine output (RunRankZero)
            ["method"] = "smoothed Dirichlet series",
            ["termsUsed"] = termsUsed,                           // the engine's actual term count
            ["termsRequired"] = M,                               // Fizz inversion: the certified floor
            ["termsRule"] = "engine LValueRankZero at digits chosen so its TermsFor >= M; M = ceil((D·ln10 + ln(4/(1-q)))/(-ln q))",
            ["q"] = qbf.ToDecimalString(60),
            ["tailBound"] = tailD.ToString("E4", inv),           // tail at termsUsed
            ["certifiedDigits"] = certDigits,                    // from termsUsed, not M
            ["digitsEmitted"] = OutDigits,
            ["seriesVerified"] = true,                           // brief 03 §2: engine series matches the certified series exactly
        },
        ["nonVanishing"] = new JsonObject
        {
            ["lValue"] = rep.LValue.Abs().ToDecimalString(20),
            ["tailBound"] = tailD.ToString("E4", inv),
            ["ratio"] = ratioD.ToString("E4", inv),
            ["gate"] = gatePass ? "pass" : "fail",
            ["note"] = "w catches odd analytic rank; this catches even rank >= 2 (|L| certified clear of the tail).",
        },
        ["period"] = new JsonObject
        {
            ["value"] = rep.Omega.ToDecimalString(OutDigits),    // engine output
            ["method"] = "AGM",
            ["digits"] = OutDigits,
            ["note"] = "AGM in BOTH discriminant branches (incl. 27606c1's spike case); no quadrature branch in the engine.",
        },
        ["regulator"] = new JsonObject { ["value"] = "1", ["note"] = "rank 0" },
        ["tamagawa"] = new JsonObject
        {
            ["product"] = prod,                                  // engine output
            ["discriminant"] = e.Delta.ToString(),               // engine field
            ["discriminantIsMinimal"] = true,
            ["_minimalityBasis"] = "engine constructor enforces the necessary u^12 minimality test (p^12|Δ ∧ p^4|c4 ∧ p^6|c6 rejected)",
            ["perPrime"] = perPrime,
            ["allSemistable"] = allSemistable,
            ["perPrimeProvenance"] = "derived in generator from public primitives (Ord, Δ, a_p) per the (v_p(Δ),a_p) recipe; the engine "
                                   + "computes only the product, so nothing is duplicated. Product cross-checked against engine TamagawaProduct = "
                                   + (tamagawaCrosscheck ? "OK" : "MISMATCH"),
        },
        ["torsion"] = new JsonObject
        {
            ["value"] = tor,                                     // engine output
            ["exact"] = false,
            ["method"] = "gcd bound",
            ["gcdPrimes"] = new JsonArray(gcdPrimes.Select(x => (JsonNode)x).ToArray()),  // derived: the engine gives only the gcd value
            ["excludesTwo"] = excludesTwo,
            ["_gcdCrosscheck"] = torCrosscheck ? "OK" : "MISMATCH",
            ["_note"] = "gcd of #E(F_p) over odd good primes only (reduction injective on torsion there; not guaranteed at p=2). "
                      + "Upper bound, enters |Sha| squared, so |Sha| is an upper bound. Not Nagell-Lutz.",
        },
        ["rootNumber"] = new JsonObject { ["available"] = true, ["value"] = w },   // engine output
        ["quotient"] = new JsonObject
        {
            ["value"] = rep.ShaEstimate.ToDecimalString(OutDigits),   // engine output (RunRankZero), NOT reassembled
            ["relation"] = "upper bound",
            ["formula"] = "|Sha| = L(E,1)·|tor|^2 / (Ω·∏cp)",
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
            ["note"] = "fractional digits agreeing between the two working precisions (same engine call); tests the implementation",
        },
    });
}

var root = new JsonObject
{
    ["schema"] = "elliptic-workbench/bsd-rank0/v2",
    ["engine"] = new JsonObject { ["commit"] = engineCommit, ["generated"] = generated, ["precisionDigits"] = OutDigits },
    ["provenance"] = "Every real quantity is a direct output of a single Elliptic.Bsd RunRankZero call per curve (L, period, "
                   + "Tamagawa product, torsion, root number, quotient), computed at " + WorkBits + "-bit working precision with the term "
                   + "count set via digits so the engine's own TermsFor clears the Fizz-certified floor M. The generator only chooses the "
                   + "term count, computes the certificate (q, tail bound, certified digits), and DERIVES quantities the engine does not "
                   + "itself produce (per-prime Tamagawa, the gcd prime list, the non-vanishing ratio); it never re-derives a value the "
                   + "engine computes. Cross-checked against a " + CheckBits + "-bit run. Labels are internal bench names, NOT LMFDB pulls (W-107).",
    ["certificate"] = "Fizz tail bound: |tail| <= 4 q^(M+1)/(1-q), q = exp(-2π/√N), via |a_n/n| <= 2 (Hasse + divisor bound, bad primes included). "
                    + "Series verified against the engine (brief 03 §2): leading 2, exp(-2πn/√N), a_{p^k}=a_p^k at bad primes.",
    ["curves"] = curvesJson,
};

File.WriteAllText(outPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"\nWrote {outPath}");
