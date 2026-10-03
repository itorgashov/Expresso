# `addyears`

Adds a number of years to a date. A negative amount subtracts years.

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
| 2 | `amount` | `int` (zero and negative values are allowed) |

## Validation & exceptions

- If you pass any number of arguments other than 2, parsing fails with `System.Exception`: `"Addyears() function should have 2 arguments."`
- The parser converts a quoted date or time string in the first argument to `DateTime`, and a literal in the second argument to `int`.
- A `null` in either argument throws `ArgumentNullException`.
- A first argument whose type is not listed in the Arguments table throws `ArgumentException`.
- An `amount` that is not an `int` throws `ArgumentException`. A `byte` or `double` amount, such as a fractional literal, is rejected. Only whole `int` amounts are supported.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`amount` is the bound `int` (zero and negative allowed).

### SQL Server

```sql
DATEADD(year, amount, datetime)
```

SQL Server example: `gt(addyears(createdat,1), dateTo)` renders as `(DATEADD(year, @wparam_0, [created_at]) > [date_to])`.

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

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.AddYears(amount)
```

The result is NULL when `datetime` or `amount` is NULL. Example: `eq(year(addyears(createdat,1)),2021)` builds `e => e.CreatedAt.AddYears(p0).Year == p1`, where `createdat` is a non-nullable `DateTime` column and `p0` and `p1` are captured parameters.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEADD(year, CAST(@__Value_0 AS int), [w].[created_at])
```

### DB2

`ExpressoDbFunctions.AddYears`:

```sql
ADD_YEARS(datetime, amount)
```

## EF6 rendering

### All providers

The canonical `DbFunctions.AddYears(datetime, amount)`, used by SQL Server and PostgreSQL. On SQL Server:

```sql
CAST( DATEADD (year, @p__linq__0, [Extent1].[created_at]) AS datetime2)
```

### Not supported

Expresso throws `NotSupportedException` on these providers:

- MySQL / MariaDB: "MySQL adds months only with INTERVAL syntax, which no store function can express".
- Oracle: "the provider pastes the amount into an INTERVAL literal, so parameters fail (ORA-01867)".
- SQLite: "the provider translates no canonical date arithmetic".

## Notes

- Negative and zero amounts are supported. `addyears(createdat,-1)` subtracts a year, and `addyears(createdat,0)` returns `createdat` unchanged. Negative offsets are passed through, so no special rendering is needed.
- You can add whole years only, as an `int`. v1 has no fractional or partial-year variant.
- See [`addmonths`](addmonths.md), [`adddays`](adddays.md), [`addhours`](addhours.md), [`addminutes`](addminutes.md), [`addseconds`](addseconds.md) for the other date-arithmetic functions.
