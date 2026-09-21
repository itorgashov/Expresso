# `gte`

Greater-than-or-equal comparison.

## Syntax

```text
gte(left, right)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Gte() function should have 2 arguments."`
- **Parser coercion:** literal type inferred from the first operand, applied to the second.
- **IR construction** (`GteFunc`, built via reflection): `ArgumentNullException` / `ArgumentException` for `null`/incompatible/`bool`/`string` operands; wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(left >= right)
```

Example: `gte(createdat,"2020-01-01")` renders as `([created_at] >= @wparam_0)` on SQL Server.

## Notes

- See [`gt`](gt.md), [`lt`](lt.md), [`lte`](lte.md) for the other ordering comparisons.
