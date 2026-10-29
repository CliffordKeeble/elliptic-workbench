# elliptic-workbench

[![ci](https://github.com/CliffordKeeble/elliptic-workbench/actions/workflows/ci.yml/badge.svg)](https://github.com/CliffordKeeble/elliptic-workbench/actions/workflows/ci.yml)
[![deploy](https://github.com/CliffordKeeble/elliptic-workbench/actions/workflows/deploy.yml/badge.svg)](https://github.com/CliffordKeeble/elliptic-workbench/actions/workflows/deploy.yml)

A small, dependency-light .NET library for the rank-0 case of the Birch and
Swinnerton-Dyer formula over ℚ, with a high-precision arithmetic core. BCL-only:
the compute projects reference no NuGet packages and restore offline.

**Live:** https://elliptic-workbench.clifford-keeble.workers.dev/

## What it computes

Given a minimal Weierstrass model of an elliptic curve over ℚ, `Elliptic.Bsd`
assembles the refined BSD quotient in the rank-0 channel:

- **L(E, 1)** by the exact smoothed Dirichlet series
  `L(E,1) = 2 Σ (a_n / n) e^(−2πn/√N)` — an identity, not a quadrature; the only
  errors are series truncation and roundoff. The digit target sets the truncation only
  approximately: `TermsFor` leaves one term out of the tail bound, so at a large conductor the
  tail can exceed 10^−digits (at N = 10¹⁰ and 30 digits it is 10^−25.2). The report carries the
  tail bound the run achieved, as `SeriesTailBound`.
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
  else `ArgumentOutOfRangeException`. The public helpers refuse absurd input the same way:
  `TermsFor` needs `digits ≥ 1`, and `An` and `Primes` need a bound in `[1, MaxSeriesTerms]` and
  `[0, MaxSeriesTerms]`.
- The series is capped at `Analytic.MaxSeriesTerms` = 10,000,000 terms, which bounds the conductor
  for a given digit target. The a_n table is 80 MB at the cap, plus the prime sieve and the a_p
  dictionary. The time is Θ(n²/ln n), because `Ap` counts points at each prime. `An` on 11a1
  takes 0.64 s at n = 10⁴ and 63 s at n = 10⁵ (medians of three Release runs on an AMD Ryzen 5
  PRO 7535U, 6 cores, 30.8 GB, Windows 11, .NET SDK 10.0.402), so by that growth the cap is days
  of CPU: roughly five to seven.
- Additive reduction throws by design — the full Tate algorithm is out of scope.
- The model must be minimal, and the constructor enforces it. It requires the declared bad primes
  to be exactly the primes dividing Δ, the reduction at each to be multiplicative, and the
  conductor to be their product; together these imply the model is minimal at every prime, which
  is stronger than the u¹² test (kept as a named diagnostic, because its message is clearer on a
  rescaled model).
- `TorsionBound` throws `NotSupportedException` when every torsion prime (3 to 37) divides Δ,
  because there is then no bound to return.
- Not thread-safe for concurrent use at different precisions: `BigFloat.Precision` is one process-wide setting, so set it once, before use. It accepts 64 to 1,048,576 bits and refuses anything else.

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
app/Elliptic.Site              local web host for the pages; F5 in Visual Studio
tests/Elliptic.Numerics.Tests  xUnit battery for BigFloat, π and the AGM
tests/Elliptic.Bsd.Tests       xUnit component battery
tests/Elliptic.Site.Tests      xUnit: the host serves what the deploy publishes, and nothing else
tools/gen-bsd-rank0            producer of data/bsd-rank0.json (see its README)
```

The repository-root `nuget.config` clears all remote package sources, so the
compute, acceptance, site and generator projects restore with no network. The
three test projects, `Elliptic.Numerics.Tests`, `Elliptic.Bsd.Tests` and
`Elliptic.Site.Tests`, each carry their own `nuget.config` re-enabling nuget.org
for the xUnit packages (and, for the last, the ASP.NET Core test host).

## Build and run

```
dotnet build elliptic-workbench.slnx
dotnet run --project app/Elliptic.Bsd.Acceptance   # prints results; exit 0 on all-pass
dotnet test elliptic-workbench.slnx
```

A good run prints `59 passed, 0 failed.` from the harness (and exits 0), then one line for each
test project, as the tool prints it (the duration is whatever the run took):

```
Passed!  - Failed:     0, Passed:    47, Skipped:     0, Total:    47, Duration: … - Elliptic.Numerics.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:    78, Skipped:     0, Total:    78, Duration: … - Elliptic.Bsd.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:    16, Skipped:     0, Total:    16, Duration: … - Elliptic.Site.Tests.dll (net10.0)
```

The build runs the .NET 10 recommended analyzers with warnings as errors
(`Directory.Build.props`: `AnalysisLevel` `10.0-recommended`, `TreatWarningsAsErrors`). There are
two exceptions. CA1707 is switched off in the three test projects, because xUnit's
`Method_Condition` test names are the point of them. And the start-up log line in
`Elliptic.Site` is a source-generated `[LoggerMessage]` method rather than a suppression of
CA1848 and CA1873: it is the platform's own pattern, and it costs nothing when the level is off.

Requires the .NET 10 SDK.

## Run the site locally

The pages are plain static files in the repository: `index.html`, `web/` and
`data/bsd-rank0.json`. `app/Elliptic.Site` serves them on your own machine,
straight from those folders.

- **Visual Studio:** open `elliptic-workbench.slnx`; right-click `Elliptic.Site`,
  *Set as Startup Project*; press F5 (or Ctrl+F5 to run without the debugger).
  The browser opens at `http://localhost:5190`.
- **Command line:** `dotnet run --project app/Elliptic.Site`; the site is at
  `http://localhost:5190`.
- Edit a page and refresh the browser; no rebuild is needed.

Only the published pages are served; the source, the tools and `.git` are not
reachable. The deployed site is still built by `tools/build-site.sh`; this host
does not touch it.

## Curve labels

Cremona, LMFDB, and the workbench's own identifiers do **not** agree nominally, and
"same curve" is an invariant-level (c₄, c₆ / j) fact, not a label match. Anyone adding
label lookup should read [LABEL-CONVENTIONS.md](LABEL-CONVENTIONS.md) first — it records
the known divergences (e.g. Cremona `11a1` = LMFDB `11.a2`) as design inputs, not bugs.

## Licence

MIT — see [LICENSE](LICENSE).
