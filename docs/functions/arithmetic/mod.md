# `mod`

Remainder of dividing `argument1` by `argument2` (`argument1 % argument2`).

## Syntax

```text
mod(argument1, argument2)
```

Exactly 2 arguments.

- **Category:** Arithmetic
- **Return type:** same as `argument1`'s type (`byte`, `int`, or `double`)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument1` | `byte`, `int`, or `double` |
| 2 | `argument2` | `byte`, `int`, or `double` (mixed numeric types allowed) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Mod() function should have 2 arguments."`
- **Parser coercion:** each literal argument's type is inferred independently.
- **IR construction** (`ModFunc`, built via reflection): `ArgumentNullException` for a `null` argument; `ArgumentException("Illegal argument type", "argument1"|"argument2")` for a non-numeric `ReturnType`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
(argument1 % argument2)
```

Example: `eq(mod(status,2),0)` renders as `(([status] % @wparam_0) = @wparam_1)`.

## Notes

- SQL Server's `%` follows the sign of the dividend, matching C#'s `%` operator (e.g. `mod(-7,3)` is `-1`, not `2`).
- `ReturnType` is copied from `argument1` only.
- `argument2 = 0` raises a SQL Server divide-by-zero error at query execution time, same as [`div`](div.md).
- See [`div`](div.md) for integer/float division, and [`floor`](floor.md)/[`round`](round.md) for other numeric shaping functions.
