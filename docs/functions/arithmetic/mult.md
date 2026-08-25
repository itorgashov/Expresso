# `mult`

Multiplication of two numeric arguments.

## Syntax

```text
mult(argument1, argument2)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Mult() function should have 2 arguments."`
- **Parser coercion:** each literal argument's type is inferred independently.
- **IR construction** (`MultFunc`, built via reflection): `ArgumentNullException` for a `null` argument; `ArgumentException("Illegal argument type", "argument1"|"argument2")` for a non-numeric `ReturnType`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
(argument1 * argument2)
```

Example: `gt(mult(price,quantity),1000)` renders as `(([price] * [quantity]) > @wparam_0)`.

## Notes

- `ReturnType` is copied from `argument1` only.
- See [`add`](add.md), [`sub`](sub.md), [`div`](div.md) for the other arithmetic operators.
