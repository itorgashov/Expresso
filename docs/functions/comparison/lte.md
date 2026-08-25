# `lte`

Less-than-or-equal comparison.

## Syntax

```text
lte(left, right)
```

Exactly 2 arguments.

- **Category:** Comparison
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `leftOperand` | `byte`, `int`, `double`, or `DateTime` |
| 2 | `rightOperand` | Same set; see [`gt`](gt.md) for the full compatibility rule (identical here) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Lte() function should have 2 arguments."`
- **Parser coercion:** literal type inferred from the first operand, applied to the second.
- **IR construction** (`LteFunc`, built via reflection): `ArgumentNullException` / `ArgumentException` for `null`/incompatible/`bool`/`string` operands; wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
(left <= right)
```

Example: `lte(price,19.99)` renders as `([price] <= @wparam_0)`.

## Notes

- See [`lt`](lt.md), [`gt`](gt.md), [`gte`](gte.md) for the other ordering comparisons.
