# gen-bsd-rank0

Producer of [`data/bsd-rank0.json`](../../data/bsd-rank0.json) (schema `bsd-rank0/v6`).

Runs the real `Elliptic.Bsd` engine over the rank-0 curves in the acceptance suite and
emits the data contract for the rank-0 quotient panel. **Generator-side only** — it makes no
change to how the engine computes any quantity.

Every real quantity in the file (L, period, Tamagawa product, torsion, root number, quotient)
is a direct output of a **single `RunRankZero` call per curve** — nothing is re-summed or
reassembled generator-side. The rule: **derive what the engine does not compute; never
re-derive what it does.**

What it does beyond taking engine outputs:

- **Certified term count** by inversion of the tail bound `|tail| ≤ 4·q^(M+1)/(1−q)`,
  `q = exp(−2π/√N)` (Fizz): `M = ⌈(D·ln10 + ln(4/(1−q))) / (−ln q)⌉`, the certified **floor**
  for `D` digits. The generator picks the engine `digits` whose own `TermsFor` clears `M`, so
  the single engine call sums `nmax ≥ M` terms. `termsUsed` is the engine's actual count,
  `termsRequired` is `M`, and `certifiedDigits` is computed from `termsUsed`.
- **Non-vanishing gate**: `|L(E,1)|` against the certified tail (catches even analytic rank ≥ 2,
  which the root number alone does not).
- **Per-prime Tamagawa** derived from public primitives (`Ord`, `Δ`, `a_p`); product cross-checked
  against the engine's `TamagawaProduct`.
- **Assertions**: gcd runs over odd good primes only (p=2 excluded); each bad prime is on the
  semistable patch (`p² ∤ N`); the discriminant used is emitted with a minimality flag.
- **Conditional square check** on `round(quotient)`, explicitly labelled `conditional` (assumes BSD
  + Cassels).
- **Precision self-check** at two working precisions (implementation axis), separate from the
  certificate (analysis axis).

## Run

```
dotnet run -c Release --project tools/gen-bsd-rank0 -- <engineCommit> <YYYY-MM-DD> <outPath> [<archiveDir>]
```

`engineCommit` must be a git short SHA (7–40 hex): the generator refuses to run otherwise, so a
mis-parsed or unexpanded argument fails loudly instead of stamping a bogus commit into the file.

`archiveDir` is the LMFDB archive the `labels` block is read from, defaulting to
`app/Elliptic.Bsd.Acceptance/lmfdb`. That default is **relative to the shell's working
directory, not the project's** — `dotnet run --project` does not chdir to the project — so a run
from anywhere but the repo root must pass it explicitly. CI passes it absolutely for that reason.

Regenerate in place (bash or PowerShell, which expand `$(...)`; in cmd.exe pass the SHA literally):

```
dotnet run -c Release --project tools/gen-bsd-rank0 -- $(git rev-parse --short HEAD) 2026-07-29 data/bsd-rank0.json
```

Real quantities are emitted as JSON strings so precision beyond `double` survives the round trip.
A curve's top-level `label` is an internal bench name, not an LMFDB pull; its `labels` block is the
sourced one, carrying the Cremona and LMFDB labels read from the archived `ec_curvedata` record
(Brief 06 §2). The two are separate on purpose — a value that feeds the panel's arithmetic must be
sourced end to end, while a display label may name an unsourced attachment.
