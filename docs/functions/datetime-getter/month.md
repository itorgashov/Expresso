# `month`

The month component (1–12) of a `DateTime` value.

## Syntax

```text
month(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Month() function should have 1 argument."`
- **Parser coercion:** argument coerced to `DateTime` if a quoted date/time token.
- **IR construction** (`MonthFunc`, via base `DateTimeSingleArgIntFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `DateTime` → `ArgumentException`

## SQL Server rendering

```sql
MONTH(datetime)
```

Example: `eq(month(createdat),1)` renders as `(MONTH([created_at]) = @wparam_0)`.

## Notes

- See [`year`](year.md), [`day`](day.md), [`dayofyear`](dayofyear.md) for related component getters.
