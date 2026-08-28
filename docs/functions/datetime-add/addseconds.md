# `addseconds`

Adds (or subtracts) a whole number of seconds to a `DateTime` value.

## Syntax

```text
addseconds(datetime, amount)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Addseconds() function should have 2 arguments."`
- **Parser coercion:** argument 1 coerced to `DateTime` if a quoted date/time token; argument 2 coerced to `int` if a literal token.
- **IR construction** (`AddSecondsFunc`, via base `DateTimeAddFunction`):
  - Either argument is `null` → `ArgumentNullException`
  - `dateTime.ReturnType` is not `DateTime` → `ArgumentException`
  - `amount.ReturnType` is not `int` → `ArgumentException`

## SQL Server rendering

```sql
DATEADD(second, amount, datetime)
```

Example: `eq(addseconds(createdat,0), createdat)` renders as `(DATEADD(second, @wparam_0, [created_at]) = [created_at])`.

## Notes

- **Negative and zero amounts are supported**: `addseconds(createdat,0)` is a no-op equivalent to `createdat` itself.
- Whole seconds only — Expresso does not expose milliseconds/microseconds in v1. See [`addminutes`](addminutes.md) for the next larger unit.
