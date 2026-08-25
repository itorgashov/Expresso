# `sub`

Subtraction of two numeric arguments (`argument1 - argument2`).

## Syntax

```text
sub(argument1, argument2)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Sub() function should have 2 arguments."`
- **Parser coercion:** each literal argument's type is inferred independently.
- **IR construction** (`SubFunc`, built via reflection): `ArgumentNullException` for a `null` argument; `ArgumentException("Illegal argument type", "argument1"|"argument2")` for a non-numeric `ReturnType`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
(argument1 - argument2)
```

Example: `gt(sub(dateTo,dateFrom),0)` — subtracting dates is **not** supported (`DateTime` is not in the allowed argument types for `sub`); this example is illustrative of the argument-order convention only. Use `sub(price,discount)` for numeric fields instead.

## Notes

- `ReturnType` is copied from `argument1` only.
- See [`add`](add.md), [`mult`](mult.md), [`div`](div.md) for the other arithmetic operators.
- See also [`mod`](mod.md), [`round`](round.md), [`floor`](floor.md), [`ceiling`](ceiling.md), [`sign`](sign.md), [`power`](power.md), [`sqrt`](sqrt.md), [`min`](min.md), [`max`](max.md) for the wider numeric function set.
