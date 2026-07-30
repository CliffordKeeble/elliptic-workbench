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

Findings (conversions, deltas, reds and diagnoses) are appended below as the job proceeds.
