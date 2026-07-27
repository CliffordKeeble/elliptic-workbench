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

`Elliptic.Bsd.Acceptance` checks the pipeline against a fixed set of reference
values. These are **frozen internal bench values**, cross-validated by independent
methods within this project. They are named after the corresponding curves in the
standard classification (11a1, 27606c1, 37a1, 389a1) but are **not fetched from any
external database**; they are hard-coded oracles-after and never feed the pipeline.
They are not, and should not be described as, external validation against published
tables. Cross-checking against published values is a separate, future task.

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
dotnet build elliptic-workbench.sln
dotnet run --project app/Elliptic.Bsd.Acceptance   # prints results; exit 0 on all-pass
dotnet test tests/Elliptic.Bsd.Tests
```

Requires the .NET 10 SDK.

## Licence

Not yet specified.
