# `div`

Division of two numeric arguments (`argument1 / argument2`).

## Syntax

```text
div(argument1, argument2)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Div() function should have 2 arguments."`
- **Parser coercion:** each literal argument's type is inferred independently.
- **IR construction** (`DivFunc`, built via reflection): `ArgumentNullException` for a `null` argument; `ArgumentException("Illegal argument type", "argument1"|"argument2")` for a non-numeric `ReturnType`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
(argument1 / argument2)
```

Example: `gt(div(revenue,unitsSold),10)` renders as `(([revenue] / [unitsSold]) > @wparam_0)`.

## Notes

- SQL Server performs **integer division** when both operands are integral (`byte`/`int`) — `div(sub(price,1), 2)` truncates just like plain SQL `/` would. Cast a field to `double`-typed data or use a `double` literal if you need fractional results.
- `ReturnType` is copied from `argument1` only.
- See [`add`](add.md), [`sub`](sub.md), [`mult`](mult.md) for the other arithmetic operators, and [`mod`](mod.md) for the remainder of an integer division.
- See also [`round`](round.md), [`floor`](floor.md), [`ceiling`](ceiling.md), [`sign`](sign.md), [`power`](power.md), [`sqrt`](sqrt.md), [`min`](min.md), [`max`](max.md) for the wider numeric function set.
