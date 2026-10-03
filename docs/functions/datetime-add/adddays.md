# `adddays`

Adds a number of days to a date. A negative amount subtracts days.

## Syntax

```text
adddays(datetime, amount)
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

- If you pass any number of arguments other than 2, parsing fails with `System.Exception`: `"Adddays() function should have 2 arguments."`
- The parser converts a quoted date or time string in the first argument to `DateTime`, and a literal in the second argument to `int`.
- A `null` in either argument throws `ArgumentNullException`.
- A first argument whose type is not listed in the Arguments table throws `ArgumentException`.
- An `amount` that is not an `int` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`amount` is the bound `int` (zero and negative allowed).

### SQL Server

```sql
DATEADD(day, amount, datetime)
```

SQL Server example: `gt(adddays(createdat,-7), dateTo)` renders as `(DATEADD(day, @wparam_0, [created_at]) > [date_to])`.

### PostgreSQL

```sql
(datetime + ((amount) * INTERVAL '1 day'))
```

### SQLite

```sql
datetime(datetime, ((amount) || ' days'))
```

### MySQL / MariaDB

```sql
DATE_ADD(datetime, INTERVAL amount DAY)
```

### Oracle

```sql
(datetime + NUMTODSINTERVAL(amount, 'DAY'))
```

### DB2

```sql
(datetime + amount DAYS)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.AddDays(amount)
```

On a `DateTime` the amount is passed as `double`; `DateOnly.AddDays` takes the `int` directly. The result is NULL when `datetime` or `amount` is NULL. Example: `eq(day(adddays(createdat,1)),16)` builds `e => e.CreatedAt.AddDays((double)p0).Day == p1`, where `createdat` is a non-nullable `DateTime` column and `p0` and `p1` are captured parameters.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEADD(day, CAST(@__p_0 AS int), [w].[created_at])
```

### DB2

`ExpressoDbFunctions.AddDays`:

```sql
ADD_DAYS(datetime, amount)
```

## EF6 rendering

### All providers

The canonical `DbFunctions.AddDays(datetime, amount)`, used by SQL Server and PostgreSQL. On SQL Server:

```sql
CAST( DATEADD (day, @p__linq__0, [Extent1].[created_at]) AS datetime2)
```

### MySQL / MariaDB

`Ef6Functions.MySqlAddDate(datetime, amount)`, the store function `ADDDATE(datetime, amount)`.

### Not supported

Expresso throws `NotSupportedException` on these providers:

- Oracle: "the provider pastes the amount into an INTERVAL literal, so parameters fail (ORA-01867)".
- SQLite: "the provider translates no canonical date arithmetic".

## Notes

- Negative and zero amounts are supported. `adddays(createdat,-7)` looks back 7 days, and `adddays(createdat,0)` is equivalent to `createdat`.
- You can add whole days only, as an `int`. There is no fractional-day variant. To add smaller increments, use [`addhours`](addhours.md), [`addminutes`](addminutes.md) or [`addseconds`](addseconds.md).
- See [`addyears`](addyears.md) and [`addmonths`](addmonths.md) for larger-unit date arithmetic.
