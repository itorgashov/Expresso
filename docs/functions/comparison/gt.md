# `gt`

Greater-than comparison.

## Syntax

```text
gt(left, right)
```

Exactly 2 arguments.

- **Category:** Comparison
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `leftOperand` | `byte`, `int`, `double`, or `DateTime` |
| 2 | `rightOperand` | Same set; see compatibility rule below |

## Type compatibility rule

`GtFunc` first applies the base comparison check (both `bool`, both `string`, both `DateTime`, or both numeric), then narrows further: only **numeric-vs-numeric** (mixed `byte`/`int`/`double`) or **`DateTime` vs `DateTime`** are accepted. A `bool`+`bool` or `string`+`string` pair passes the base check but is then rejected here with `ArgumentException` — ordering comparisons don't make sense for those types in v1.

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Gt() function should have 2 arguments."`
- **Parser coercion:** literal type inferred from the first operand, applied to the second.
- **IR construction** (`GtFunc`, built via reflection): `ArgumentNullException` / `ArgumentException` for `null`/incompatible/`bool`/`string` operands; wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
(left > right)
```

Example: `gt(age,25)` renders as `([age] > @wparam_0)`.

## Notes

- See [`gte`](gte.md), [`lt`](lt.md), [`lte`](lte.md) for the other ordering comparisons, and [`eq`](eq.md)/[`neq`](neq.md) for equality (which do allow `bool`/`string`).
