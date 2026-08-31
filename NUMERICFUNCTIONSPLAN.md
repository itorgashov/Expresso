# Numeric functions — first wave plan

> Status: implemented, tested, documented, and released as **0.4.0**. See [docs/functions/arithmetic/](docs/functions/arithmetic/).

## Scope

9 IR functions, 11 parser names (`ceil`→`CeilingFunc`, `pow`→`PowerFunc`):

| Function | Args | Return |
|---|---|---|
| `mod` | `(n, d)` numeric | same as `n` |
| `floor`, `ceiling`/`ceil` | `(n)` numeric | `double` |
| `round` | `(n)` or `(n, digits)` | `double` |
| `sign` | `(n)` numeric | `int` |
| `power`/`pow` | `(base, exp)` numeric | `double` |
| `sqrt` | `(n)` numeric | `double` |
| `min`, `max` | `(a, b)` numeric | same as `a` |

Out of scope: docs, version bump, `toint`/`todouble`, `trunc`, log/trig.

## Architecture

Core bases → parser partial → SQL Server renderer partial → tests (Core, Parsing, Rendering).

## SQL Server rendering

| IR | SQL |
|---|---|
| `ModFunc` | `(a % b)` |
| `FloorFunc` | `FLOOR(arg)` |
| `CeilingFunc` | `CEILING(arg)` |
| `SqrtFunc` | `SQRT(arg)` |
| `SignFunc` | `SIGN(arg)` |
| `PowerFunc` | `POWER(a, b)` |
| `RoundFunc` (1 arg) | `ROUND(arg, 0)` |
| `RoundFunc` (2 args) | `ROUND(a, b)` |
| `MinFunc` | `CASE WHEN (a) < (b) THEN (a) ELSE (b) END` |
| `MaxFunc` | `CASE WHEN (a) > (b) THEN (a) ELSE (b) END` |
