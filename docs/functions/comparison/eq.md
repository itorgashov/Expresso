# `eq`

Equality comparison.

## Syntax

```text
eq(left, right)
```

Exactly 2 arguments.

- **Category:** Comparison
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `leftOperand` | `byte`, `int`, `double`, `DateTime`, `bool`, `string`, `Guid`, `TimeSpan`; plus `DateOnly`/`TimeOnly` on net6.0 |
| 2 | `rightOperand` | Same set; see compatibility rule below |

## Type compatibility rule

`EqFunc` (like all comparison functions) first checks that the two operands form one of these compatible pairs:

- both `bool`
- both `string`
- both `DateTime`
- both `Guid`
- both `TimeSpan` (time-of-day)
- both `DateOnly` (net6.0)
- both `TimeOnly` (net6.0)
- both numeric (`byte`/`int`/`double`, mixed numeric types allowed, e.g. `byte` vs `int`)

Any other pairing → `ArgumentException("Incompatible argument types")`.

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Eq() function should have 2 arguments."`
- **Parser coercion:** if the first argument is a quoted/unquoted literal token, its type is inferred (`GetLiteralType`); the second literal argument is then coerced to that same type. `Incompatible argument types: expected {type}, got {type}.` (`ArgumentException`) if a non-literal second argument's `ReturnType` still mismatches after coercion.
- **IR construction** (`EqFunc`, built via reflection by the parser): `ArgumentNullException` for a `null` operand; `ArgumentException` for incompatible/disallowed types. Because the parser constructs comparison functions via `Activator.CreateInstance`, these surface as `System.Reflection.TargetInvocationException` with the real exception in `.InnerException` — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
(left = right)
```

Example: `eq(status,1)` renders as `([status] = @wparam_0)`.

## Notes

- See [`neq`](neq.md) for the negated form, and [`gt`](gt.md)/[`gte`](gte.md)/[`lt`](lt.md)/[`lte`](lte.md) for ordering comparisons (which do **not** allow `bool`/`string` operands).
