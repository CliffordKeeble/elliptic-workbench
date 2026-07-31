# Curve labels — an inheritance note for the label-lookup brief

**Written:** 31 July 2026, Mr Code, on CinC's follow-on pass to Brief 03 (item 3).
**Audience:** whoever picks up the future **LMFDB-label-lookup brief**. This is a
**design input you inherit**, not a bug to rediscover. The divergences below are known,
recorded, and deliberately left un-normalised. Read this before designing the lookup.

---

## What this note fixes in advance

When curves are looked up by label, three distinct naming systems meet: the workbench's
own generic identifiers, **Cremona** labels, and **LMFDB** labels. They do **not** agree
nominally, and treating any one as canonical for another will mis-map curves. Two concrete
divergences were observed during Brief 03 and are the design constraints you inherit.

### Divergence 1 — Cremona number ≠ LMFDB number within a conductor

Cremona `11a1` is LMFDB **`11.a2`**. Same curve, `[0,−1,1,−10,−20]`, conductor 11 — but the
**curve-number-within-isogeny-class differs** between the two schemes (Cremona's optimal-curve
ordering is not LMFDB's). The class *letter* happens to agree here (`a`), but the trailing
number does not.

Note this divergence is **not universal**: for the other Brief 03 curves the labels align
nominally — Cremona `27606c1` = LMFDB `27606.c1`, `37a1` = `37.a1`, `389a1` = `389.a1`. So a
lookup cannot assume alignment *and* cannot assume misalignment: the mapping is per-curve and
must come from a table or the API, never from string surgery on one label to produce the other.

### Divergence 2 — a workbench generic name that matches neither label

The workbench model `n233`, a-invariants `[1,3,0,−1,0]`, conductor 233, is:

- **Cremona `233a2`** (not `233a1`), and
- **LMFDB `233.a1`**, whose minimal model is `[1,0,1,−5,3]`.

So one curve carries **three** identifiers, none of which is a substring transform of another,
and the workbench's own a-invariants match **neither** published minimal model directly.

## The identity is invariant-level, not nominal

`n233`'s identity to LMFDB `233.a1` was established **at the level of the isomorphism
invariants**, not by comparing labels or a-invariants:

```
c₄ = 217,  c₆ = −3133,  Δ = 233,  and  c₄³ − c₆² = 1728·Δ = 1728·233
```

Equal (c₄, c₆) over ℚ means the same curve up to isomorphism; the workbench model and the LMFDB
minimal model are **translation-equivalent minimal models** of that one curve, differing only in
Weierstrass presentation. Two independent computations (Mr Code's and CinC's re-derivation) agree
on these invariants. **This is the correct notion of "same curve" for a lookup to key on** —
labels and a-invariant strings are surface forms; (c₄, c₆) / j is the invariant.

## Normalisation was deliberately NOT attempted

Reducing the workbench models to LMFDB reduced form was **parked, on purpose**, not overlooked.
Normalising `n233` (and any curve like it) to reduced form would move **every curated point** on
the curve into new coordinates — a change that ripples through the panel data and its generator.
That is a considered piece of work in its own right, not a step to fold silently into a lookup.
The workbench keeps its own identifiers; published labels live in provenance only.

## What the lookup brief inherits (the design inputs)

1. **Key on invariants, not labels.** Establish "same curve" by (c₄, c₆) / j over ℚ, and carry
   the published labels as attached provenance — do not derive one label from another by string
   manipulation.
2. **Carry both published labels** (Cremona *and* LMFDB) plus the workbench identifier as three
   separate fields. Any of the three may differ from the others; none is a transform of another.
3. **Expect nominal alignment to be coincidental.** Where Cremona and LMFDB labels look alike
   (`27606c1`/`27606.c1`), that is luck, not a rule (`11a1`/`11.a2` breaks it).
4. **Model normalisation is a prerequisite decision, not a lookup detail.** If the lookup ever
   needs curves in a single canonical model, that normalisation is its own commissioned step
   (it moves curated points); decide it explicitly, up front, with CinC.

## Where the raw evidence lives

The Brief 03 archive under `app/Elliptic.Bsd.Acceptance/lmfdb/` holds the LMFDB API responses
these labels were read from (`Clabel` = Cremona, `lmfdb_label` = LMFDB), integrity-checked by the
acceptance suite against `lmfdb/manifest.json`. The provenance narrative is in
`app/Elliptic.Bsd.Acceptance/acceptance-constants.md` (identity table and the two label surprises).
