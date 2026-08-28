# `day`

The day-of-month component (1–31) of a `DateTime` value.

## Syntax

```text
day(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime` or `DateOnly` (net6.0) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Day() function should have 1 argument."`
- **Parser coercion:** argument coerced to `DateTime` if a quoted date/time token.
- **IR construction** (`DayFunc`, via base `DateTimeSingleArgIntFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `DateTime` → `ArgumentException`

## SQL Server rendering

```sql
DAY(datetime)
```

Example: `eq(day(createdat),15)` renders as `(DAY([created_at]) = @wparam_0)`.

## Notes

- Day of the **month**, not day of the year — see [`dayofyear`](dayofyear.md) for that, and [`dayofweek`](dayofweek.md) for the weekday.
