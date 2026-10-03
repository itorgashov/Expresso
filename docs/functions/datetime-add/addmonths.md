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
  - `dateTime.ReturnType` is not one of the types in the table above → `ArgumentException`
  - `amount.ReturnType` is not `int` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`amount` is the bound `int` (zero and negative allowed).

### SQL Server

```sql
DATEADD(month, amount, datetime)
```

Example: `eq(addmonths(createdat,0), createdat)` renders as `(DATEADD(month, @wparam_0, [created_at]) = [created_at])`.

### PostgreSQL

```sql
(datetime + ((amount) * INTERVAL '1 month'))
```

### SQLite

```sql
datetime(datetime, ((amount) || ' months'))
```

### MySQL / MariaDB

```sql
DATE_ADD(datetime, INTERVAL amount MONTH)
```

### Oracle

```sql
(datetime + NUMTOYMINTERVAL(amount, 'MONTH'))
```

### DB2

```sql
(datetime + amount MONTHS)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.AddMonths(amount)
```

The result is NULL when `datetime` or `amount` is NULL. Example: `eq(month(addmonths(createdat,1)),2)` builds `e => e.CreatedAt.AddMonths(p0).Month == p1`, where `createdat` is a non-nullable `DateTime` column and `p0` and `p1` are captured parameters.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEADD(month, CAST(@__Value_0 AS int), [w].[created_at])
```

### DB2

`ExpressoDbFunctions.AddMonths`:

```sql
ADD_DAYS(ADD_MONTHS(datetime, amount), LEAST(0, DAY(datetime) - DAY(ADD_MONTHS(datetime, amount))))
```

`ADD_MONTHS` moves a month-end day to the new month end. The `LEAST` term takes those extra days back, so the result matches the DB2 renderer's `datetime + amount MONTHS`, which keeps the day and clamps it to the month end.

## EF6 rendering

### All providers

The canonical `DbFunctions.AddMonths(datetime, amount)`, used by SQL Server and PostgreSQL. On SQL Server:

```sql
CAST( DATEADD (month, @p__linq__0, [Extent1].[created_at]) AS datetime2)
```

### Not supported

The transformer throws `NotSupportedException` on these providers:

- MySQL / MariaDB: "MySQL adds months only with INTERVAL syntax, which no store function can express".
- Oracle: "the provider pastes the amount into an INTERVAL literal, so parameters fail (ORA-01867)".
- SQLite: "the provider translates no canonical date arithmetic".

## Notes

- **Negative and zero amounts are supported**: `addmonths(createdat,-3)` subtracts 3 months.
- Month-end behavior (e.g. adding a month to January 31) follows SQL Server's native `DATEADD` rules, which match .NET's `DateTime.AddMonths` day-clamping behavior in the common case.
- See [`addyears`](addyears.md) and [`adddays`](adddays.md) for related date-arithmetic functions.
