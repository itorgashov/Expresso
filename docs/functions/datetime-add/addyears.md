# `addyears`

Adds (or subtracts) a whole number of years to a `DateTime` value.

## Syntax

```text
addyears(datetime, amount)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Addyears() function should have 2 arguments."`
- **Parser coercion:** argument 1 coerced to `DateTime` if a quoted date/time token; argument 2 coerced to `int` if a literal token.
- **IR construction** (`AddYearsFunc`, via base `DateTimeAddFunction`):
  - Either argument is `null` → `ArgumentNullException`
  - `dateTime.ReturnType` is not `DateTime` → `ArgumentException`
  - `amount.ReturnType` is not `int` → `ArgumentException` (a `byte` or `double` amount, e.g. a fractional literal, is rejected — only whole `int` amounts are supported)

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`amount` is the bound `int` (zero and negative allowed).

### SQL Server

```sql
DATEADD(year, amount, datetime)
```

Example: `gt(addyears(createdat,1), dateTo)` renders as `(DATEADD(year, @wparam_0, [created_at]) > [date_to])`.

### PostgreSQL

```sql
(datetime + ((amount) * INTERVAL '1 year'))
```

### SQLite

```sql
datetime(datetime, ((amount) || ' years'))
```

### MySQL / MariaDB

```sql
DATE_ADD(datetime, INTERVAL amount YEAR)
```

### Oracle

```sql
(datetime + NUMTOYMINTERVAL(amount, 'YEAR'))
```

### DB2

```sql
(datetime + amount YEARS)
```

## Notes

- **Negative and zero amounts are supported**: `addyears(createdat,-1)` subtracts a year; `addyears(createdat,0)` is a no-op equivalent to `createdat` itself. Negative offsets are passed through; no special rendering is needed.
- Only whole years via `int`; there is no fractional/partial-year variant in v1.
- See [`addmonths`](addmonths.md), [`adddays`](adddays.md), [`addhours`](addhours.md), [`addminutes`](addminutes.md), [`addseconds`](addseconds.md) for the other date-arithmetic functions.
