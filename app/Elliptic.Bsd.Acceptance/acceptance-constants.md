# Acceptance constants — inventory and provenance

Rulebook §5 remediation (Brief 03, commissioned by CinC, 29 July 2026). Executor: Mr Code.

This file is the **before-photograph**: every constant the acceptance suite
(`Program.cs`) asserts, exactly as it stands *before* any conversion to published
values. Nothing in this commit changes behaviour.

**Current state.** All 18 constants are **frozen internal bench values** (3 July 2026,
"BSD Arc Repair Manifest, Appendix"), named after LMFDB curves but **not pulled from
LMFDB**. The suite therefore tests the code against its own remembered outputs —
agreement is one claim wearing a chorus, not corroboration (W-107 in workbench form).
The job replaces each with the published value plus source and date.

The provenance columns (published value, both labels, page URL, access date) and the
tolerance rule are filled by later steps, in their own commits. This commit is the
inventory only.

## Curves under test

| bench name | a-invariants [a1,a2,a3,a4,a6] | conductor | role in the suite |
|---|---|---|---|
| 11a1      | [0, −1, 1, −10, −20]                 | 11    | rank-0 boot curve |
| 27606c1   | [1, 0, 0, −10289707, 12703497719]    | 27606 | rank-0, \|Ш\| = 4 |
| 37a1      | [0, 0, 1, −1, 0]                     | 37    | period-only cross-check (rank 1) |
| 389a1     | [0, 1, 1, −2, 0]                     | 389   | period-only cross-check (rank 2) |
| n233      | [1, 3, 0, −1, 0]                     | 233   | rank-0, Finding-1 regression |

## The 18 constants

| # | curve | quantity (engine field) | current bench value | compare | Program.cs | LMFDB quantity it represents |
|---|---|---|---|---|---|---|
| 1  | 11a1    | root number ε (`RootNumber`)       | +1                                  | integer, exact   | L36 | root number (sign of functional equation) |
| 2  | 11a1    | Tamagawa ∏cₚ (`TamagawaProduct`)   | 5                                   | integer, exact   | L37 | Tamagawa product |
| 3  | 11a1    | torsion **bound** (`TorsionBound`) | 5                                   | integer, exact   | L38 | torsion order † |
| 4  | 11a1    | real period Ω (`Omega`)            | 1.26920930427955342168879461675     | decimal, 30 dp   | L39 | real period |
| 5  | 11a1    | L(E,1) (`LValue`)                  | 0.253841860855910                   | decimal, 15 dp   | L40 | special value L(E,1) |
| 6  | 11a1    | \|Ш\| estimate (`ShaEstimate`)     | 1  (as 1.0000000000000000000)       | integer, exact   | L41 | analytic order of Ш |
| 7  | 27606c1 | root number ε                      | +1                                  | integer, exact   | L48 | root number |
| 8  | 27606c1 | Tamagawa ∏cₚ                       | 3                                   | integer, exact   | L49 | Tamagawa product |
| 9  | 27606c1 | torsion **bound**                  | 1                                   | integer, exact   | L50 | torsion order † |
| 10 | 27606c1 | real period Ω                      | 0.538085890979675477333935451400    | decimal, 30 dp   | L52 | real period |
| 11 | 27606c1 | L(E,1)                             | 6.45703069175610                    | decimal, 14 dp   | L53 | special value L(E,1) |
| 12 | 27606c1 | \|Ш\| estimate                     | 4  (as 4.0000000000000000000)       | integer, exact   | L54 | analytic order of Ш |
| 13 | 37a1    | real period Ω                      | 5.986917292463919259664019958       | decimal, 27 dp   | L60 | real period |
| 14 | 389a1   | real period Ω                      | 4.980425121710110150642715583       | decimal, 27 dp   | L63 | real period |
| 15 | n233    | root number ε                      | +1                                  | integer, exact   | L72 | root number |
| 16 | n233    | Tamagawa ∏cₚ                       | 1                                   | integer, exact   | L73 | Tamagawa product |
| 17 | n233    | torsion **bound**                  | 2                                   | integer, exact   | L74 | torsion order † |
| 18 | n233    | \|Ш\| estimate                     | 1  (as 1.000000000000000)           | integer, exact   | L75 | analytic order of Ш |

## Notes carried into the conversion

**† torsion is a bound, not an order.** `TorsionBound` is a gcd over #E(𝔽ₚ) at small
good primes — an *upper bound* on the torsion order, not the order itself. LMFDB
publishes the true torsion order. They coincide only where the bound is tight, which is
the case for all five curves here; the step-7 benchmark curve is chosen precisely so
that they diverge. When the published order is attached, this constant is compared as
"bound = published order", and any curve where that fails is a genuine finding, not a
tolerance problem.

**|Ш| is the engine's estimate.** The `ShaEstimate` is `L·T² / (Ω·∏cₚ)` with `T` the
gcd torsion *bound*; it equals the analytic order of Ш only where `T` is tight. Same
caveat as the torsion row, one level up.

**37a1 and 389a1 are period-only.** They exercise the Δ>0 AGM branch at rank ≥ 1; the
L-machinery is rank-0 only, so only Ω is asserted for them (no L, ∏cₚ, or Ш).

**n233 is the workbench's own model.** `[1,3,0,−1,0]` is LMFDB 233.a1 (minimal model
`[1,0,1,−5,3]`) — the same curve by matching c₄=217, c₆=−3133, Δ=233 (recorded under the
30a1 / trueOrder work). Because that identity is our own computation rather than a direct
a-invariant display, its label-verification (step 2) is flagged for care.

**Compare precision is provisional.** The "compare" column reflects the `ToDecimalString(n)`
prefix each check currently uses. Step 5 fixes the comparison precision from the *source's*
published precision, not from the current code — a tolerance chosen so the code passes is
the disease this job cures.

## Job order (each in its own commit)

1. **Inventory** — this file. ✅ committed before anything changes.
2. Verify a-invariants against each LMFDB page (identity before value).
3. Record both labels (Cremona + LMFDB) as the page shows them.
4. Pull published values: value, LMFDB quantity name, both labels, page URL, access date.
5. Pin the tolerance rule — committed *before* the first comparison.
6. Swap bench → published + provenance; run; report every red as a finding, diagnosed, none tuned. Fixes go in separate follow-on commits.
7. Add the torsion-bound benchmark curve (published order vs bound-derived), with rationale.

---

# Provenance — pulled from LMFDB (steps 2–4)

**Route.** Values pulled from the **LMFDB API** — `ec_curvedata` (labels, a-invariants,
conductor, rank, `analytic_rank`, torsion order, `sha`) and `ec_mwbsd` (`real_period`,
`special_value` = L(E,1), `tamagawa_product`, `sha_an`). **Access date: 2026-07-30**
(LMFDB API response timestamp). The citation is the LMFDB curve page,
`https://www.lmfdb.org/EllipticCurve/Q/<Cremona>/`.

**Root number.** LMFDB carries no separate "root number" field; the functional-equation
sign is `(−1)^analytic_rank` from the published `analytic_rank` (0 for the three rank-0
curves → +1).

**Methodology finding — the narrative fetch was unreliable.** A prose WebFetch of the
same pages misreported the root number as −1 for all three rank-0 curves; it returned a
*local* sign at a bad prime, not the *global* sign, which for a rank-0 curve is
necessarily +1. Every value here is taken from the structured API, not the summariser.

## Identity verified (step 2) and both labels (step 3)

| curve (bench) | Cremona | LMFDB | LMFDB a-invariants | workbench a-invariants | identity |
|---|---|---|---|---|---|
| 11a1    | 11a1    | 11.a2     | [0,−1,1,−10,−20]                  | [0,−1,1,−10,−20]              | ✓ direct |
| 27606c1 | 27606c1 | 27606.c1  | [1,0,0,−10289707,12703497719]     | [1,0,0,−10289707,12703497719] | ✓ direct |
| 37a1    | 37a1    | 37.a1     | [0,0,1,−1,0]                      | [0,0,1,−1,0]                  | ✓ direct |
| 389a1   | 389a1   | 389.a1    | [0,1,1,−2,0]                      | [0,1,1,−2,0]                  | ✓ direct |
| n233    | **233a2** | 233.a1  | [1,0,1,−5,3] (minimal)            | [1,3,0,−1,0]                  | same curve (c₄=217, c₆=−3133, Δ=233), **models differ — FLAG** |

Two label surprises, taken from the page not assumed: Cremona `11a1` = LMFDB `11.**a2**`
(the numberings differ), and n233 is Cremona **233a2** (not 233a1) = LMFDB 233.a1 — the
workbench's generic "n233" matches neither Cremona nor LMFDB label. **Flag for CinC:** n233's
identity is our own c₄/c₆/Δ computation (models differ on the page), not a direct a-invariant
display — the same provenance caveat as its `trueOrder`.

## Published values (step 4) and delta to bench

| # | curve | quantity → LMFDB field | bench value | LMFDB published value | Δ |
|---|---|---|---|---|---|
| 1  | 11a1    | root number → sign         | +1                                | +1                                                | 0 |
| 2  | 11a1    | ∏cₚ → tamagawa_product     | 5                                 | 5                                                 | 0 |
| 3  | 11a1    | torsion bound → order †     | 5                                 | 5                                                 | 0 |
| 4  | 11a1    | Ω → real_period            | 1.26920930427955342168879461675   | 1.2692093042795534216887946168 (28 sf)            | agree to LMFDB precision |
| 5  | 11a1    | L(E,1) → special_value     | 0.253841860855910                 | 0.25384186085591068433775892335043887465 (38 sf)  | bench is a matching prefix |
| 6  | 11a1    | \|Ш\| → sha_an             | 1                                 | 1                                                 | 0 |
| 7  | 27606c1 | root number → sign         | +1                                | +1                                                | 0 |
| 8  | 27606c1 | ∏cₚ → tamagawa_product     | 3                                 | 3                                                 | 0 |
| 9  | 27606c1 | torsion bound → order †     | 1                                 | 1                                                 | 0 |
| 10 | 27606c1 | Ω → real_period            | 0.538085890979675477333935451400  | 0.53808589097967547733393545140 (29 sf)           | agree to LMFDB precision |
| 11 | 27606c1 | L(E,1) → special_value     | 6.45703069175610                  | 6.4570306917561057280072254168078748568 (38 sf)   | bench is a matching prefix |
| 12 | 27606c1 | \|Ш\| → sha_an             | 4                                 | 4                                                 | 0 |
| 13 | 37a1    | Ω → real_period            | 5.986917292463919259664019958     | 5.9869172924639192596640199589 (29 sf)            | agree to LMFDB precision |
| 14 | 389a1   | Ω → real_period            | 4.980425121710110150642715583     | 4.9804251217101101506427155839 (29 sf)            | agree to LMFDB precision |
| 15 | n233    | root number → sign         | +1                                | +1                                                | 0 |
| 16 | n233    | ∏cₚ → tamagawa_product     | 1                                 | 1                                                 | 0 |
| 17 | n233    | torsion bound → order †     | 2                                 | 2                                                 | 0 |
| 18 | n233    | \|Ш\| → sha_an             | 1                                 | 1                                                 | 0 |

**Finding: all 18 constants agree with the LMFDB published values.** The bench values were
accurate; they were simply not independently sourced. Attaching the citation turns the suite
from a regression suite wearing a validation costume into an actual validation suite. The
conversion (step 6) is therefore expected to stay green; any red will still be reported as a
finding, but none is anticipated on these five curves. The divergence the job exists to
surface — bound ≠ order — is real only off the tight patch, and is deferred to the step-7
benchmark curve.

† The bench asserts the gcd **bound**; LMFDB gives the true **order**. They coincide for all
five (tight), so the conversion compares "bound = published order". Where they differ (the
benchmark curve) the bound exceeds the order and the difference is a documented defect, not a
tolerance problem.

## Still to do (own commits)

5. Pin the tolerance rule (integers exact; decimals at LMFDB's published precision, ½ ulp) — committed *before* the conversion.
6. Swap bench → published + provenance in `Program.cs`; run; report any red as a finding.
7. The torsion-bound benchmark curve.

# Tolerance rule (step 5 — pinned before any comparison)

- **Integer quantities** (root number, Tamagawa product, torsion, |Ш| as integer): compared **exactly**.
- **Decimal quantities** (real period, L(E,1)): compared at **LMFDB's published precision** — the number
  of decimal places LMFDB displays — with tolerance **½ unit in the last published place**.
- The comparison is performed **in decimal**: the engine value is rendered to the published number of
  places and compared against LMFDB's decimal string as scaled `BigInteger`s. **No binary-double round
  trip** — parsing to `double` would silently eat the tolerance (CinC caution).
- The tolerance derives **only** from the source's precision. The engine must compute to at least the
  published precision; where the harness's digit target is lower, the shortfall is **reported as a
  finding**, never absorbed by widening the tolerance or lowering the compare precision.
- **Amendment (29 Jul, own commit).** Where a source field's *display* precision exceeds its
  *self-consistent* precision, the comparison runs at the self-consistent precision. This applies to
  `special_value` — displayed at 38 dp but self-consistent to ~28 dp (anchored by `real_period`'s 28
  dp), the true value verified at three benches (conversion findings below). This is **not** lowering
  the tolerance to pass: it is comparing at the precision the source is actually right to; the display
  overrun is documented, not honoured.
- **Torsion** is validated as "engine bound is tight and equals the published order" on the five tight
  curves; the benchmark curve (step 7) documents the non-tight case.

n233 identity is **confirmed at the invariant level** (two independent computations: ours and CinC's
re-derivation; c₄=217, c₆=−3133, Δ=233, and c₄³−c₆²=1728·233 checks). Equal (c₄,c₆) over ℚ is the same
curve up to isomorphism; the two are translation-equivalent minimal models, ours not in LMFDB reduced
form. Direct a-invariant comparison is not applicable; both computations are cited; both true labels are
carried (Cremona 233a2 / LMFDB 233.a1). Model normalisation to reduced form is **parked** (would move
every curated point — a candidate future job). Identifiers stay as-is; labels live in provenance only.

# Findings from the conversion run

## Conversion run (step 6) — 16 pass, 2 fail

Swapped the bench values for LMFDB published values + provenance and ran the suite. Engine
untouched; only the harness constants and the comparator changed. **16 of 18 pass** at
LMFDB's published precision. **Two reds, both L(E,1):**

| red | published (LMFDB) | engine (= Ω/5) | agree to | DIFF |
|---|---|---|---|---|
| 11a1 L(E,1) @ 38 dp | 0.…9233504388746500 | 0.…9233509094610439 | 30 dp | ≈ 4.7×10⁻³¹ |
| 27606c1 L(E,1) @ 37 dp | 6.…254168078748568 | 6.…254168081281631 | 30 dp | ≈ 2.5×10⁻³¹ |

**Diagnosis — a GENUINE numerical gap. (First read "harness digit-target": RETRACTED.)** At
`digits=34` the engine already sums ~106 terms with tail bound ≈ 10⁻⁸⁸, so L(E,1) is accurate to
~88 places, not 34. Confirmed empirically: the engine's L is **identical at digits = 34, 50, 70**
(converged — raising the target changes nothing), **stable to 95 digits** under the 512-vs-640
self-check, and **equals Ω/5 exactly to 40 digits** — Ω via the independent AGM route, Ω/5
BSD-exact for |Ш|=1. Two independent engine methods agree to 40 digits. The engine and LMFDB agree
to 30 digits and **disagree at digit 31**:

```
engine (= Ω/5): 0.253841860855910684337758923350 9094610439…
LMFDB special:  0.253841860855910684337758923350 4388746500
```

This is neither truncation nor a value tuned to pass.

**Resolved (CinC ruling, 29 Jul) — a precision seam, verified at three benches.** The true value
of L(E,1) is confirmed independently:

- **PARI/GP 2.15.4**, `lfun(ellinit([0,-1,1,-10,-20]), 1)` at 100 digits:
  `0.2538418608559106843377589233509094610438984483661217335934273842460816677225555445380724611843025215`,
  with L − Ω/5 = 2.3×10⁻⁹⁷ (the BSD identity to the working floor).
- **CinC's independent mpmath build** (aₚ from brute-force point counts on the curve equation;
  a₁₁ = 1 from the nonsingular count, not a table; smoothed series at 70 digits): agrees with PARI
  to 1.4×10⁻⁷¹.
- **This engine**: its 40-digit value is the correct rounding of the above.

So LMFDB's `special_value` is **correct to its actual precision (~digit 30)** — consistent with its
own `real_period` being published at 28 digits — and the displayed tail beyond that is conversion
junk from the stored representation. **This is a precision seam, not an error: LMFDB is right to the
precision it computes; the display overstates it.** Under the amended tolerance rule (compare at the
source's self-consistent precision) the two L checks **pass at 28 dp**. They are preserved here as
the job's reds with the full chain — banked red (b243ac3) → first diagnosis retracted (dc73d53) →
three-bench verification → registered amendment → green. That chain is the difference between
discovery and tuning.

The other 16 constants validate at LMFDB's published precision: root numbers, Tamagawa products,
torsion (bound = published order, all tight), all four real periods, and |Ш| (analytic order).

## Step 7 — the 30a1 torsion-bound benchmark

Added **30a1** (LMFDB **30.a8**, a-invariants `[1,0,1,1,2]`, conductor 30) as the benchmark for the
non-tight case. Rationale: small conductor, a clean 2× gap, provenance already established (brief 05,
LMFDB 30.a8 torsion order 6). The engine's gcd `TorsionBound` = **12**, the **published order = 6** —
so the bound overshoots by k = 2, and the |Ш| *estimate* = L·12²/(Ω·∏cₚ) = **4** where the analytic
order is **1**. The defect is **live**: the trueOrder work added a *data* field, it did not cure the
engine's `TorsionBound` (a gcd bound, not Nagell–Lutz). Documented under the **known-defect
convention** — the two benchmark checks assert the current (defective) engine values (12 and 4) so a
future cure flips them visibly, with the published order / analytic order stated as the targets. The
cure is **parked** (candidate future brief); this pass documents, it does not fix.

## Job status

Suite: **20 passed, 0 failed** — 18 constants validated against LMFDB published values + 2 known-defect
benchmark checks. The two L(E,1) checks are green under the amended rule (compare at the source's
self-consistent precision) and preserved above as the job's reds with the full discovery chain (banked
→ retracted → three-bench verified → amended → green). Raw source responses archived under `lmfdb/`,
access-dated. **Ready for Sketch to mark rulebook §5 discharged.**

---

# Follow-on pass to Brief 03 (CinC, 31 July 2026)

Three items arising from the close-out pass, none of them a re-opening of it.

## Item 1 — the archive is now integrity-checked, not just parsed

The old check was "every file parses as JSON". That is weaker than the thing it stood in for:
a well-formed API error parses, and a correct-format record for the **wrong curve** parses
perfectly (W-111 at retrieval depth). Replaced by a **field-level integrity check** that runs
as part of the acceptance suite (`Program.cs`, first block; the archive is copied to the build
output via a `<Content>` item in the `.csproj`, so it is checked every time the suite runs, not
in a one-off shell). Per archived response, three assertions, driven by
[`lmfdb/manifest.json`](lmfdb/manifest.json):

1. **The record's own label field equals the requested label.** `Clabel` for `ec_curvedata`,
   `lmfdb_label` for `ec_mwbsd`, each checked against the label the file was fetched for — the
   assertion that catches a wrong-curve record.
2. **Every field an acceptance constant is drawn from is present, non-null, and of the right
   shape** — named explicitly per file in the manifest's `requiredFields`, not a generic
   non-empty test. For the real-literal fields (`real_period`, `special_value`, `sha_an`) the
   check descends into `.data` (where LMFDB carries the value + `prec`), so a field present but
   hollow still fails.
3. **A SHA-256 content hash per file matches the manifest**, recorded alongside the access date
   (`2026-07-30`), so a later silent re-fetch or hand-edit is detectable rather than assumed
   absent.

**Result — all 12 files pass** (the 10 from Brief 03 plus the two 30a1 evidence files
`ec_curvedata_30a1.json` / `ec_mwbsd_30.a8.json`, now committed with the manifest):

| file | expected label | label | fields | hash |
|---|---|---|---|---|
| ec_curvedata_11a1.json    | 11a1    | ✓ | ✓ | ✓ |
| ec_mwbsd_11.a2.json       | 11.a2   | ✓ | ✓ | ✓ |
| ec_curvedata_27606c1.json | 27606c1 | ✓ | ✓ | ✓ |
| ec_mwbsd_27606.c1.json    | 27606.c1| ✓ | ✓ | ✓ |
| ec_curvedata_37a1.json    | 37a1    | ✓ | ✓ | ✓ |
| ec_mwbsd_37.a1.json       | 37.a1   | ✓ | ✓ | ✓ |
| ec_curvedata_389a1.json   | 389a1   | ✓ | ✓ | ✓ |
| ec_mwbsd_389.a1.json      | 389.a1  | ✓ | ✓ | ✓ |
| ec_curvedata_233a2.json   | 233a2   | ✓ | ✓ | ✓ |
| ec_mwbsd_233.a1.json      | 233.a1  | ✓ | ✓ | ✓ |
| ec_curvedata_30a1.json    | 30a1    | ✓ | ✓ | ✓ |
| ec_mwbsd_30.a8.json       | 30.a8   | ✓ | ✓ | ✓ |

No file failed; nothing was re-fetched or repaired. The suite is now **32 passed, 0 failed**
(12 integrity + 20 constants). The evidence base is checked rather than believed, and the check
runs whenever the suite does.

## Item 2 — the 30a1 defect hypothesis: confirmed alive, prediction frozen

CinC's reading is that the two 30a1 defect numbers (torsion bound 12 vs order 6; |Ш| estimate 4
vs analytic 1) are **one defect seen twice** — torsion enters the BSD ratio squared, so a bound
too large by k = 2 inflates |Ш| by exactly 4. Step 1 (does |Ш| draw on the *same* torsion
quantity?) is **confirmed**: `Compiler.cs` computes `sha = L·(tor·tor)/(Ω·∏c_p)` with
`tor = e.TorsionBound()` — the same gcd bound, squared, no independent torsion source. Hypothesis
alive. Per CinC's instruction the **prediction is now frozen before any cure runs**, in
[`30a1-torsion-cure-prereg.md`](30a1-torsion-cure-prereg.md): TorsionBound 12→6, |Ш| → **exactly
1** at a 20-dp / ½-ulp tolerance with the residual reported (replacing the current 0-dp window),
with kill conditions stated. **The cure stays parked** — it is not green under standing trust and
waits for Cliff to unpark it.

## Item 3 — the label divergence, made inheritable

The parked label findings (Cremona `11a1` = LMFDB `11.a2`; `n233` = Cremona `233a2` / LMFDB
`233.a1`) are written up as an explicit inheritance note at the repo root,
[`LABEL-CONVENTIONS.md`](../../LABEL-CONVENTIONS.md), linked from the README so the future
LMFDB-label-lookup brief reads it as a **design input** rather than rediscovering it as a bug: the
two divergences with instances, the identity being invariant-level (c₄, c₆ / j) not nominal, and
model normalisation deliberately parked because it would move every curated point.
