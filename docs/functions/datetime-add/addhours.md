# `addhours`

Adds (or subtracts) a whole number of hours to a `DateTime` value.

## Syntax

```text
addhours(datetime, amount)
```

Exactly 2 arguments.

- **Category:** DateTime arithmetic
- **Return type:** same as first argument (`DateTime`, `TimeSpan` on netstandard2.0, or `TimeOnly` on net6.0)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `dateTime` | `DateTime`, `TimeSpan` (time-of-day), or `TimeOnly` (net6.0) |
| 2 | `amount` | `int` — zero and negative values are allowed |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Addhours() function should have 2 arguments."`
- **Parser coercion:** argument 1 coerced to `DateTime` if a quoted date/time token; argument 2 coerced to `int` if a literal token.
- **IR construction** (`AddHoursFunc`, via base `DateTimeAddFunction`):
  - Either argument is `null` → `ArgumentNullException`
  - `dateTime.ReturnType` is not `DateTime` → `ArgumentException`
  - `amount.ReturnType` is not `int` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`amount` is the bound `int` (zero and negative allowed).

### SQL Server

```sql
DATEADD(hour, amount, datetime)
```

Example: `gt(addhours(createdat,24), dateTo)` renders as `(DATEADD(hour, @wparam_0, [created_at]) > [date_to])`.

### PostgreSQL

```sql
(datetime + ((amount) * INTERVAL '1 hour'))
```

### SQLite

```sql
datetime(datetime, ((amount) || ' hours'))
```

### MySQL / MariaDB

```sql
DATE_ADD(datetime, INTERVAL amount HOUR)
```

### Oracle

```sql
(datetime + NUMTODSINTERVAL(amount, 'HOUR'))
```

### DB2

```sql
(datetime + amount HOURS)
```

## Notes

- **Negative and zero amounts are supported**: `addhours(createdat,-1)` subtracts one hour.
- Only whole hours via `int`. See [`adddays`](adddays.md) for a larger unit and [`addminutes`](addminutes.md)/[`addseconds`](addseconds.md) for smaller ones.
