# Archived LMFDB API responses

Raw LMFDB API JSON responses, archived as **source evidence** for the acceptance constants
(Brief 03). Every value the acceptance suite validates against — including the displayed
38-digit `special_value` string the precision-seam finding is about — is traceable to a file
here.

**Access date: 2026-07-30** (the LMFDB API stamps each response with its own timestamp; see the
`timestamp` field in each file).

**Source of record** is the LMFDB curve page: `https://www.lmfdb.org/EllipticCurve/Q/<Cremona>/`.
These JSON files are the raw API responses the values were pulled from.

## Files

Two collections per curve — `ec_curvedata` (identity, labels, a-invariants, conductor, rank,
`analytic_rank`, torsion order, `sha`) and `ec_mwbsd` (`real_period`, `special_value` = L(E,1),
`tamagawa_product`, `sha_an`):

| curve | Cremona | LMFDB | ec_curvedata query | ec_mwbsd query |
|---|---|---|---|---|
| 11a1    | 11a1    | 11.a2    | `?Clabel=11a1`    | `?lmfdb_label=11.a2`    |
| 27606c1 | 27606c1 | 27606.c1 | `?Clabel=27606c1` | `?lmfdb_label=27606.c1` |
| 37a1    | 37a1    | 37.a1    | `?Clabel=37a1`    | `?lmfdb_label=37.a1`    |
| 389a1   | 389a1   | 389.a1   | `?Clabel=389a1`   | `?lmfdb_label=389.a1`   |
| n233    | 233a2   | 233.a1   | `?Clabel=233a2`   | `?lmfdb_label=233.a1`   |

Base: `https://www.lmfdb.org/api/<collection>/<query>&_format=json`. n233 is fetched by its
Cremona/LMFDB labels; the workbench's own model `[1,3,0,−1,0]` is the same curve as the archived
minimal model `[1,0,1,−5,3]` at the invariant level (see acceptance-constants.md).
