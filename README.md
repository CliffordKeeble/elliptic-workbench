# elliptic-workbench

[![ci](https://github.com/CliffordKeeble/elliptic-workbench/actions/workflows/ci.yml/badge.svg)](https://github.com/CliffordKeeble/elliptic-workbench/actions/workflows/ci.yml)
[![deploy](https://github.com/CliffordKeeble/elliptic-workbench/actions/workflows/deploy.yml/badge.svg)](https://github.com/CliffordKeeble/elliptic-workbench/actions/workflows/deploy.yml)

A small, dependency-light .NET library for the rank-0 case of the Birch and
Swinnerton-Dyer formula over ℚ, with a high-precision arithmetic core. BCL-only:
the compute projects reference no NuGet packages and restore offline.

## What it computes

Given a minimal Weierstrass model of an elliptic curve over ℚ, `Elliptic.Bsd`
assembles the refined BSD quotient in the rank-0 channel:

- **L(E, 1)** by the exact smoothed Dirichlet series
  `L(E,1) = 2 Σ (a_n / n) e^(−2πn/√N)` — an identity, not a quadrature; the only
  errors are series truncation (set by the digit target) and roundoff.
- **Real period Ω** by the arithmetic-geometric mean, both discriminant signs,
  with full-precision bisection for the cubic roots (no quadrature, no seeds).
- **a_p and the Hecke rebuild**: a_p by direct point counting over 𝔽_p, then the
  full multiplicative sequence a_n by the Hecke recursion.
- **Tamagawa product ∏c_p** for semistable curves.
- **Global root number ε**.
- **|Ш| estimate**, assembled as `|Ш| = L(E,1) · |tor|² / (Ω · ∏c_p)`.

High-precision arithmetic (`BigFloat`, π via Brent–Salamin, AGM) lives in
`Elliptic.Numerics`, built on `System.Numerics.BigInteger` alone.

## Scope and limits

- **Rank 0, ε = +1, semistable, minimal models only.**
- Odd analytic rank (ε = −1) throws by design — the derivative engine is out of scope.
- Even analytic rank ≥ 2 is refused too: if |L(E,1)| is no larger than the series tail, L(E,1)
  is not certified non-zero and `RunRankZero` throws `NotSupportedException` (389a1, ε = +1,
  rank 2, is the witness).
- `digits` is bounded by the working precision: `1 ≤ digits ≤ ⌊0.30103 · BigFloat.Precision⌋ − 10`,
  else `ArgumentOutOfRangeException`. The series is capped at 10,000,000 terms (`An` allocates a
  table that long, 80 MB), which bounds the conductor for a given digit target.
- Additive reduction throws by design — the full Tate algorithm is out of scope.
- The model must be minimal; a necessary minimality test is enforced.
- Not thread-safe for concurrent use at different precisions: `BigFloat.Precision` is one process-wide setting, so set it once, before use.

### Known limitation — torsion bound

`TorsionBound` returns a gcd **upper bound** on |E(ℚ)_tors|, not a certified order,
and it enters the |Ш| estimate **squared**. An overshoot by a factor k inflates the
estimate by k². The |Ш| figure is therefore an **upper bound**, exact only for curves
whose torsion is independently known. Certified torsion (Lutz–Nagell) is future work.

## Acceptance constants

`Elliptic.Bsd.Acceptance` validates the pipeline against **published reference values
from LMFDB** (`ec_curvedata` + `ec_mwbsd`, accessed 2026-07-31), each curve citing its
LMFDB label. The raw API responses are archived under
`app/Elliptic.Bsd.Acceptance/lmfdb/` and **integrity-checked at the start of every run**
— label field, required fields, and a per-file SHA-256 against `lmfdb/manifest.json`.
Each acceptance constant is then **pinned to the archived field it was drawn from**, so
the suite establishes engine = constant = archive rather than engine = a hard-coded
oracle of unknown provenance.

The reference values **never feed the pipeline** — the engine computes L(E,1), Ω, ∏c_p,
the torsion bound and the root number itself (see `Compiler.cs`), and LMFDB appears only in
the acceptance suite, as an oracle-after. What the engine is **given** is the five
Weierstrass coefficients, the conductor, and the list of bad primes. It does not take those
three on trust: the constructor requires every declared prime to be a distinct prime in
[2, 2³¹ − 1], requires them to be *exactly* the primes dividing Δ, and requires the
conductor to be their product. Reduction at each must be multiplicative, which with the
other two makes the model minimal at every prime, so a non-minimal or non-semistable model
is refused rather than silently given wrong numbers. The acceptance harness reads the
conductor and the bad primes from the archived `ec_curvedata` records, not from literals
typed at the bench — they are inputs, so they carry provenance like every other value. 37a1 and
389a1 are period-only cross-checks (the Δ > 0 AGM branch at rank ≥ 1). For L(E,1),
LMFDB's `special_value` is compared at its self-consistent precision (~28 dp): its
display carries more digits than it is internally consistent to — a **precision seam,
not an error**. Provenance, per-constant deltas, the tolerance rule, and that seam are
documented in
[acceptance-constants.md](app/Elliptic.Bsd.Acceptance/acceptance-constants.md).

## Layout

```
src/Elliptic.Numerics          BigFloat, Reals (π, AGM)  — BCL-only
src/Elliptic.Bsd               EllipticCurve, Analytic, BsdCompiler, RankZeroReport
app/Elliptic.Bsd.Acceptance    console harness; exit 0 iff all checks pass
tests/Elliptic.Numerics.Tests  xUnit battery for BigFloat, π and the AGM
tests/Elliptic.Bsd.Tests       xUnit component battery
tools/gen-bsd-rank0            producer of data/bsd-rank0.json (see its README)
```

The repository-root `nuget.config` clears all remote package sources, so the
compute, acceptance and generator projects restore with no network. The two test
projects, `Elliptic.Numerics.Tests` and `Elliptic.Bsd.Tests`, each carry their own
`nuget.config` re-enabling nuget.org for the xUnit packages.

## Build and run

```
dotnet build elliptic-workbench.slnx
dotnet run --project app/Elliptic.Bsd.Acceptance   # prints results; exit 0 on all-pass
dotnet test elliptic-workbench.slnx
```

A good run prints `59 passed, 0 failed.` from the harness (and exits 0), then
`Passed!  - Failed: 0, Passed: 40` for `Elliptic.Numerics.Tests.dll` and
`Passed!  - Failed: 0, Passed: 62` for `Elliptic.Bsd.Tests.dll`.

Requires the .NET 10 SDK.

## Curve labels

Cremona, LMFDB, and the workbench's own identifiers do **not** agree nominally, and
"same curve" is an invariant-level (c₄, c₆ / j) fact, not a label match. Anyone adding
label lookup should read [LABEL-CONVENTIONS.md](LABEL-CONVENTIONS.md) first — it records
the known divergences (e.g. Cremona `11a1` = LMFDB `11.a2`) as design inputs, not bugs.

## Licence

MIT — see [LICENSE](LICENSE).
