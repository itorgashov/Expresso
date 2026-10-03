# `addhours`

Adds a number of hours to a date or time of day. A negative amount subtracts hours.

## Syntax

```text
addhours(datetime, amount)
```

Exactly 2 arguments.

- **Category:** DateTime arithmetic
- **Return type:** same as first argument (`DateTime`, `TimeSpan`, or `TimeOnly` on net6.0)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `dateTime` | `DateTime`, `TimeSpan` (time-of-day), or `TimeOnly` (net6.0) |
| 2 | `amount` | `int` (zero and negative values are allowed) |

## Validation & exceptions

- If you pass any number of arguments other than 2, parsing fails with `System.Exception`: `"Addhours() function should have 2 arguments."`
- The parser converts a quoted date or time string in the first argument to `DateTime`, and a literal in the second argument to `int`.
- A `null` in either argument throws `ArgumentNullException`.
- A first argument whose type is not listed in the Arguments table throws `ArgumentException`.
- An `amount` that is not an `int` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`amount` is the bound `int` (zero and negative allowed).

### SQL Server

```sql
DATEADD(hour, amount, datetime)
```

SQL Server example: `gt(addhours(createdat,24), dateTo)` renders as `(DATEADD(hour, @wparam_0, [created_at]) > [date_to])`.

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

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.AddHours(amount)
```

On a `DateTime` or `TimeOnly` the amount is passed as `double`. On a time-of-day `TimeSpan` the shape is `datetime + TimeSpan.FromHours(amount)`. The result is NULL when `datetime` or `amount` is NULL. Example: `eq(hour(addhours(createdat,2)),12)` builds `e => e.CreatedAt.AddHours((double)p0).Hour == p1`, where `createdat` is a non-nullable `DateTime` column and `p0` and `p1` are captured parameters; `eq(hour(addhours(opens,10)),3)` builds `e => (e.Opens + TimeSpan.FromHours((double)p0)).Hours == p1`.

### In-memory

On a time-of-day `TimeSpan`, `ExpressoFunctions.AddTimeOfDay(datetime, TimeSpan.FromHours(amount))` wraps at 24 hours like PostgreSQL `time` arithmetic, so 17:00 plus 10 hours is 03:00. Other types are the same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEADD(hour, CAST(@__p_0 AS int), [w].[created_at])
```

### SQL Server

For a time-of-day `TimeSpan`, `ExpressoDbFunctions.AddHours`:

```sql
DATEADD(hour, amount, datetime)
```

### MySQL / MariaDB

For a time-of-day `TimeSpan`, `ExpressoDbFunctions.AddHours`. Like the MySQL renderer's `DATE_ADD`, it does not wrap at 24 hours.

```sql
ADDTIME(datetime, SEC_TO_TIME(amount * 3600))
```

### SQLite

For a time-of-day `TimeSpan`, `ExpressoDbFunctions.AddHours`:

```sql
datetime(datetime, printf('%d hours', amount))
```

### DB2

`ExpressoDbFunctions.AddHours` is `ADD_HOURS(datetime, amount)` on a `DateTime`. DB2 has no `ADD_HOURS` for `time`, so a time-of-day `TimeSpan` goes through a timestamp:

```sql
TIME(ADD_HOURS(TIMESTAMP('2000-01-01', datetime), amount))
```

## EF6 rendering

### All providers

The canonical `DbFunctions.AddHours(datetime, amount)`, on a `DateTime` or a time-of-day `TimeSpan`, used by SQL Server and PostgreSQL. On SQL Server:

```sql
CAST( DATEADD (hour, @p__linq__0, [Extent1].[created_at]) AS datetime2)
```

### MySQL / MariaDB

On a `DateTime`, `Ef6Functions.MySqlTimestamp` and `Ef6Functions.MySqlSecToTime` give `TIMESTAMP(datetime, SEC_TO_TIME(amount * 3600))`. On a time-of-day `TimeSpan`, `Ef6Functions.MySqlAddTime` gives `ADDTIME(datetime, SEC_TO_TIME(amount * 3600))`. On MySQL 8 (not MariaDB), comparing that time-of-day result with a `TimeSpan` parameter never matches, because MySql.Data sends the parameter as `'0 hh:mm:ss.ffffff'`; this is driver-level and does not throw (see [docs/semantics.md](../../semantics.md)).

### Not supported

Expresso throws `NotSupportedException` on these providers:

- Oracle: "the provider pastes the amount into an INTERVAL literal, so parameters fail (ORA-01867)".
- SQLite: "the provider translates no canonical date arithmetic".

## Notes

- Negative and zero amounts are supported. `addhours(createdat,-1)` subtracts one hour.
- You can add whole hours only, as an `int`. See [`adddays`](adddays.md) for a larger unit, and [`addminutes`](addminutes.md) or [`addseconds`](addseconds.md) for smaller ones.
