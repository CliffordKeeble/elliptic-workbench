# gen-bsd-rank0

Producer of [`data/bsd-rank0.json`](../../data/bsd-rank0.json) (schema `bsd-rank0/v2`).

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
dotnet run -c Release --project tools/gen-bsd-rank0 -- <engineCommit> <YYYY-MM-DD> <outPath>
```

Example (regenerate in place):

```
dotnet run -c Release --project tools/gen-bsd-rank0 -- $(git rev-parse --short HEAD) 2026-07-29 data/bsd-rank0.json
```

Real quantities are emitted as JSON strings so precision beyond `double` survives the round trip.
Labels are internal bench names, not LMFDB pulls.
