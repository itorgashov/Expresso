# `addmonths`

Adds (or subtracts) a whole number of months to a `DateTime` value.

## Syntax

```text
addmonths(datetime, amount)
```

Exactly 2 arguments.

- **Category:** DateTime arithmetic
- **Return type:** same as first argument (`DateTime` or `DateOnly` on net6.0)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `dateTime` | `DateTime` or `DateOnly` (net6.0) |
| 2 | `amount` | `int` — zero and negative values are allowed |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Addmonths() function should have 2 arguments."`
- **Parser coercion:** argument 1 coerced to `DateTime` if a quoted date/time token; argument 2 coerced to `int` if a literal token.
- **IR construction** (`AddMonthsFunc`, via base `DateTimeAddFunction`):
  - Either argument is `null` → `ArgumentNullException`
  - `dateTime.ReturnType` is not `DateTime` → `ArgumentException`
  - `amount.ReturnType` is not `int` → `ArgumentException`

## SQL Server rendering

```sql
DATEADD(month, amount, datetime)
```

Example: `eq(addmonths(createdat,0), createdat)` renders as `(DATEADD(month, @wparam_0, [created_at]) = [created_at])`.

## Notes

- **Negative and zero amounts are supported**: `addmonths(createdat,-3)` subtracts 3 months.
- Month-end behavior (e.g. adding a month to January 31) follows SQL Server's native `DATEADD` rules, which match .NET's `DateTime.AddMonths` day-clamping behavior in the common case.
- See [`addyears`](addyears.md) and [`adddays`](adddays.md) for related date-arithmetic functions.
