# `add`

Addition of two numeric arguments.

## Syntax

```text
add(argument1, argument2)
```

Exactly 2 arguments.

- **Category:** Arithmetic
- **Return type:** same as `argument1`'s type (`byte`, `int`, or `double` — **not** promoted based on `argument2`)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument1` | `byte`, `int`, or `double` |
| 2 | `argument2` | `byte`, `int`, or `double` (mixed numeric types allowed, e.g. `int` + `double`) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Add() function should have 2 arguments."`
- **Parser coercion:** each literal argument's type is inferred independently (`GetLiteralType`).
- **IR construction** (`AddFunc`, built via reflection): base `NumericArithFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", "argument1"|"argument2")` if either `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
(argument1 + argument2)
```

Example: `gt(add(price,tax),100)` renders as `(([price] + [tax]) > @wparam_0)`.

## Notes

- `ReturnType` is copied from `argument1` only.
- See [`sub`](sub.md), [`mult`](mult.md), [`div`](div.md) for the other arithmetic operators, [`abs`](abs.md) for the unary one, and [`mod`](mod.md), [`round`](round.md), [`floor`](floor.md), [`ceiling`](ceiling.md), [`sign`](sign.md), [`power`](power.md), [`sqrt`](sqrt.md), [`min`](min.md), [`max`](max.md) for the wider numeric function set.
