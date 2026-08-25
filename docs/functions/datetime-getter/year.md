# `year`

The calendar year component of a `DateTime` value.

## Syntax

```text
year(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Year() function should have 1 argument."`
- **Parser coercion:** argument coerced to `DateTime` if a quoted date/time token.
- **IR construction** (`YearFunc`, via base `DateTimeSingleArgIntFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `DateTime` → `ArgumentException`

## SQL Server rendering

```sql
YEAR(datetime)
```

Example: `eq(year(createdat),2020)` renders as `(YEAR([created_at]) = @wparam_0)`.

## Notes

- See [`month`](month.md), [`day`](day.md), [`dayofyear`](dayofyear.md), [`hour`](hour.md), [`minute`](minute.md), [`second`](second.md), [`dayofweek`](dayofweek.md) for the other single-component getters, and [`date`](date.md) for truncating to midnight instead of extracting a component.
