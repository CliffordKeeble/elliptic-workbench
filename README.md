# elliptic-workbench

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
- Additive reduction throws by design — the full Tate algorithm is out of scope.
- The model must be minimal; a necessary minimality test is enforced.

### Known limitation — torsion bound

`TorsionBound` returns a gcd **upper bound** on |E(ℚ)_tors|, not a certified order,
and it enters the |Ш| estimate **squared**. An overshoot by a factor k inflates the
estimate by k². The |Ш| figure is therefore an **upper bound**, exact only for curves
whose torsion is independently known. Certified torsion (Lutz–Nagell) is future work.

## Acceptance constants

`Elliptic.Bsd.Acceptance` validates the pipeline against **published reference values
from LMFDB** (`ec_curvedata` + `ec_mwbsd`, accessed 2026-07-30), each curve citing its
LMFDB label. The raw API responses are archived under
`app/Elliptic.Bsd.Acceptance/lmfdb/` and **integrity-checked at the start of every run**
— label field, required fields, and a per-file SHA-256 against `lmfdb/manifest.json`.
Each acceptance constant is then **pinned to the archived field it was drawn from**, so
the suite establishes engine = constant = archive rather than engine = a hard-coded
oracle of unknown provenance.

The reference values **never feed the pipeline** — the engine computes L(E,1), Ω, ∏c_p,
the torsion bound and the root number from the curve's coefficients alone (see
`Compiler.cs`); LMFDB appears only in the acceptance suite, as an oracle-after. 37a1 and
389a1 are period-only cross-checks (the Δ > 0 AGM branch at rank ≥ 1). For L(E,1),
LMFDB's `special_value` is compared at its self-consistent precision (~28 dp): its
display carries more digits than it is internally consistent to — a **precision seam,
not an error**. Provenance, per-constant deltas, the tolerance rule, and that seam are
documented in
[acceptance-constants.md](app/Elliptic.Bsd.Acceptance/acceptance-constants.md).

## Layout

```
src/Elliptic.Numerics       BigFloat, Reals (π, AGM)  — BCL-only
src/Elliptic.Bsd            EllipticCurve, Analytic, BsdCompiler, RankZeroReport
app/Elliptic.Bsd.Acceptance console harness; exit 0 iff all checks pass
tests/Elliptic.Bsd.Tests    xUnit component battery
```

The repository-root `nuget.config` clears all remote package sources, so the
compute and acceptance projects restore with no network. `Elliptic.Bsd.Tests`
carries its own `nuget.config` re-enabling nuget.org for the xUnit packages.

## Build and run

```
dotnet build elliptic-workbench.slnx
dotnet run --project app/Elliptic.Bsd.Acceptance   # prints results; exit 0 on all-pass
dotnet test tests/Elliptic.Bsd.Tests
```

Requires the .NET 10 SDK.

## Curve labels

Cremona, LMFDB, and the workbench's own identifiers do **not** agree nominally, and
"same curve" is an invariant-level (c₄, c₆ / j) fact, not a label match. Anyone adding
label lookup should read [LABEL-CONVENTIONS.md](LABEL-CONVENTIONS.md) first — it records
the known divergences (e.g. Cremona `11a1` = LMFDB `11.a2`) as design inputs, not bugs.

## Licence

MIT — see [LICENSE](LICENSE).
