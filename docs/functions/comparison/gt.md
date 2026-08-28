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
| 1 | `leftOperand` | `byte`, `int`, `double`, `DateTime`; plus `DateOnly`/`TimeOnly` on net6.0 |
| 2 | `rightOperand` | Same set; see compatibility rule below |

## Type compatibility rule

`GtFunc` first applies the base comparison check (both `bool`, both `string`, both `DateTime`, both `Guid`, both `DateOnly`/`TimeOnly` on net6.0, or both numeric), then narrows further: only **numeric-vs-numeric** (mixed `byte`/`int`/`double`), **`DateTime` vs `DateTime`**, or same-type **`DateOnly`/`TimeOnly`** on net6.0 are accepted. `Guid` and `bool`/`string` pairs pass the base check but are then rejected — ordering comparisons don't apply to those types.

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
