# Pre-registration — the 30a1 torsion cure and its |Ш| prediction

**Staked:** 31 July 2026, Mr Code, on CinC's follow-on pass to Brief 03 (item 2).
**Status at stake time:** cure **not started**. `TorsionBound` on 30a1 is still 12.
**Frozen:** this file is a pre-registration in the Methods sense — once staked it is
not edited to absorb what it predicts. If the result differs from the stake, the stake
stays and the result cites it.

---

## The hypothesis under test

30a1 (LMFDB 30.a8) shows **two** apparent defects:

- torsion **gcd bound = 12** against **published order 6** (overshoot by k = 2);
- **|Ш| estimate = 4** against **analytic order 1**.

CinC's arithmetic (follow-on item 2, tagged [A] — arithmetic on two reported numbers,
offered as a hypothesis to confirm or kill): torsion enters the strong-BSD ratio
**squared, in the denominator**, so a bound too large by a factor k inflates the |Ш|
estimate by exactly k². With k = 2 that is a factor of 4. The reading is therefore that
these are **not two defects but one defect seen twice** — once directly, once squared.

## Step 1 — confirmed: |Ш| is drawn from the same torsion quantity

Required before staking: check that the |Ш| estimate is in fact computed from the same
gcd bound, not from an independent torsion figure. If it were independent, the hypothesis
would be dead and this item would end here.

It is the same quantity. `src/Elliptic.Bsd/Compiler.cs`, rank-0 assembly:

```csharp
var tor = e.TorsionBound();                 // the gcd upper bound (= 12 on 30a1)
// |Sha| estimate = L * |tor|^2 / (Ω * ∏c_p)
var sha = (l * (tor * tor)) / (omega * BigFloat.From(tam));
```

`sha` reads `tor = e.TorsionBound()` and squares it. There is no second torsion source.
The |Ш| estimate and the torsion bound are the **same number**, and the estimate carries
its square. **Hypothesis alive → prediction staked below.**

## The staked prediction

The single cure is `TorsionBound` on 30a1: **12 → 6** (the published order). It is the only
change; nothing else in the assembly is touched. The exact-arithmetic consequence is:

```
before:  sha = L · 12² / (Ω · ∏c_p) = L · 144 / (Ω · ∏c_p) = 4
after:   sha = L ·  6² / (Ω · ∏c_p) = L ·  36 / (Ω · ∏c_p) = 1
```

(For 30a1 the strong-BSD identity gives L = Ω·∏c_p / 6 with ∏c_p = 6, so `sha` reduces to
144/36 = 4 before and 36/36 = 1 after — both exact rationals, independent of how many
digits L and Ω are carried to.)

**Prediction, sharp:**

1. `r30.TorsionBound` goes from **12 to 6**.
2. `r30.ShaEstimate` goes to **exactly 1** — not merely closer to 1.

**Tolerance and residual (so "close to 1" cannot pass itself off as 1).** The current
known-defect check compares `ShaEstimate` at **0 dp**, i.e. a rounding window of ±0.5 — wide
enough that anything in [0.5, 1.5) would read as "1". That window is not admissible as
confirmation. The cured check will instead assert

```
| ShaEstimate − 1 | < 0.5 × 10⁻²⁰      (compare at 20 dp, ½-ulp)
```

and **report the actual residual `ShaEstimate − 1` to full engine precision** in the run
output. The engine computes 30a1 at `digits: 34`, so a correct single-cure lands the residual
at the arithmetic floor (~10⁻³⁰ or smaller); anything at O(10⁻¹) or larger is a real second
effect, not rounding. The 20-dp gate is ~10²⁰ tighter than the present window and excludes any
O(0.1) masquerade while sitting far inside the engine's own precision.

## Kill conditions (what refutes the single-defect reading)

- **|Ш| lands anywhere other than 1 once the torsion bound is 6** → a second, independent
  defect exists and the one-defect reading was **wrong**. The stake stays; the result cites it.
- **|Ш| reaches 1 while `TorsionBound` is still 12** → not a confirmation but a **compensating
  error**, and a worse finding than the one we started with (two errors cancelling). This would
  mean the cure was mis-attributed; investigate before claiming anything.
- A residual larger than ~10⁻²⁰ but still rounding to 1 is itself reportable — it would mean an
  unmodelled numerical effect distinct from the torsion overshoot.

## What is NOT authorised by this file

The **cure itself is parked.** This pre-registration only freezes the stake. Running the cure —
editing `TorsionBound` (a certified Nagell–Lutz order in place of the gcd bound) — is **not green
under standing trust**; it waits for Cliff to unpark it. The point of the exercise is that the
known-defect flip becomes a **test** rather than a confirmation, and it can only be that if the
stake is frozen before the cure runs. This file is that freeze.

When the cure is unparked and run, its result is recorded **beneath** this stake, not inside it.
