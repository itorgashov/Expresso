# `adddays`

Adds (or subtracts) a whole number of days to a `DateTime` value.

## Syntax

```text
adddays(datetime, amount)
```

Exactly 2 arguments.

- **Category:** DateTime arithmetic
- **Return type:** `DateTime`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `dateTime` | `DateTime` |
| 2 | `amount` | `int` — zero and negative values are allowed |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Adddays() function should have 2 arguments."`
- **Parser coercion:** argument 1 coerced to `DateTime` if a quoted date/time token; argument 2 coerced to `int` if a literal token.
- **IR construction** (`AddDaysFunc`, via base `DateTimeAddFunction`):
  - Either argument is `null` → `ArgumentNullException`
  - `dateTime.ReturnType` is not `DateTime` → `ArgumentException`
  - `amount.ReturnType` is not `int` → `ArgumentException`

## SQL Server rendering

```sql
DATEADD(day, amount, datetime)
```

Example: `gt(adddays(createdat,-7), dateTo)` renders as `(DATEADD(day, @wparam_0, [created_at]) > [date_to])`, with `-7` bound as the parameter — a common "created within the last 7 days" pattern.

## Notes

- **Negative and zero amounts are supported**: `adddays(createdat,-7)` looks back 7 days; `adddays(createdat,0)` is equivalent to `createdat`.
- Only whole days via `int`; there is no fractional-day variant. Use [`addhours`](addhours.md)/[`addminutes`](addminutes.md)/[`addseconds`](addseconds.md) to add sub-day increments.
- See [`addyears`](addyears.md) and [`addmonths`](addmonths.md) for larger-unit date arithmetic.
