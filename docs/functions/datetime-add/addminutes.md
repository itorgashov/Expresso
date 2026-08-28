# `addminutes`

Adds (or subtracts) a whole number of minutes to a `DateTime` value.

## Syntax

```text
addminutes(datetime, amount)
```

Exactly 2 arguments.

- **Category:** DateTime arithmetic
- **Return type:** same as first argument (`DateTime` or `TimeOnly` on net6.0)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `dateTime` | `DateTime` or `TimeOnly` (net6.0) |
| 2 | `amount` | `int` — zero and negative values are allowed |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Addminutes() function should have 2 arguments."`
- **Parser coercion:** argument 1 coerced to `DateTime` if a quoted date/time token; argument 2 coerced to `int` if a literal token.
- **IR construction** (`AddMinutesFunc`, via base `DateTimeAddFunction`):
  - Either argument is `null` → `ArgumentNullException`
  - `dateTime.ReturnType` is not `DateTime` → `ArgumentException`
  - `amount.ReturnType` is not `int` → `ArgumentException`

## SQL Server rendering

```sql
DATEADD(minute, amount, datetime)
```

Example: `gt(addminutes(createdat,-30), dateTo)` renders as `(DATEADD(minute, @wparam_0, [created_at]) > [date_to])`.

## Notes

- **Negative and zero amounts are supported**: `addminutes(createdat,-30)` looks back 30 minutes.
- Only whole minutes via `int`. See [`addhours`](addhours.md) for a larger unit and [`addseconds`](addseconds.md) for a smaller one.
