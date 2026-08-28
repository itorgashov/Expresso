# `second`

The second component (0–59) of a `DateTime` value.

## Syntax

```text
second(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime` or `TimeOnly` (net6.0) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Second() function should have 1 argument."`
- **Parser coercion:** argument coerced to `DateTime` if a quoted date/time token.
- **IR construction** (`SecondFunc`, via base `DateTimeSingleArgIntFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `DateTime` → `ArgumentException`

## SQL Server rendering

```sql
DATEPART(second, datetime)
```

Example: `eq(second(createdat),0)` renders as `(DATEPART(second, [created_at]) = @wparam_0)`.

## Notes

- Whole seconds only — Expresso does not expose milliseconds/microseconds in v1. See [`hour`](hour.md) and [`minute`](minute.md) for the other time components.
