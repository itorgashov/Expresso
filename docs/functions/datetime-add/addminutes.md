# `addminutes`

Adds a number of minutes to a date or time of day. A negative amount subtracts minutes.

## Syntax

```text
addminutes(datetime, amount)
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

- If you pass any number of arguments other than 2, parsing fails with `System.Exception`: `"Addminutes() function should have 2 arguments."`
- The parser converts a quoted date or time string in the first argument to `DateTime`, and a literal in the second argument to `int`.
- A `null` in either argument throws `ArgumentNullException`.
- A first argument whose type is not listed in the Arguments table throws `ArgumentException`.
- An `amount` that is not an `int` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`amount` is the bound `int` (zero and negative allowed).

### SQL Server

```sql
DATEADD(minute, amount, datetime)
```

SQL Server example: `gt(addminutes(createdat,-30), dateTo)` renders as `(DATEADD(minute, @wparam_0, [created_at]) > [date_to])`.

### PostgreSQL

```sql
(datetime + ((amount) * INTERVAL '1 minute'))
```

### SQLite

```sql
datetime(datetime, ((amount) || ' minutes'))
```

### MySQL / MariaDB

```sql
DATE_ADD(datetime, INTERVAL amount MINUTE)
```

### Oracle

```sql
(datetime + NUMTODSINTERVAL(amount, 'MINUTE'))
```

### DB2

```sql
(datetime + amount MINUTES)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.AddMinutes(amount)
```

On a `DateTime` or `TimeOnly` the amount is passed as `double`. On a time-of-day `TimeSpan` the shape is `datetime + TimeSpan.FromMinutes(amount)`. The result is NULL when `datetime` or `amount` is NULL. Example: `eq(minute(addminutes(createdat,15)),15)` builds `e => e.CreatedAt.AddMinutes((double)p0).Minute == p1`, where `createdat` is a non-nullable `DateTime` column and `p0` and `p1` are captured parameters; `eq(minute(addminutes(opens,40)),10)` builds `e => (e.Opens + TimeSpan.FromMinutes((double)p0)).Minutes == p1`.

### In-memory

On a time-of-day `TimeSpan`, `ExpressoFunctions.AddTimeOfDay(datetime, TimeSpan.FromMinutes(amount))` wraps at 24 hours like PostgreSQL `time` arithmetic, so 23:50 plus 20 minutes is 00:10. Other types are the same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEADD(minute, CAST(@__p_0 AS int), [w].[created_at])
```

### SQL Server

For a time-of-day `TimeSpan`, `ExpressoDbFunctions.AddMinutes`:

```sql
DATEADD(minute, amount, datetime)
```

### MySQL / MariaDB

For a time-of-day `TimeSpan`, `ExpressoDbFunctions.AddMinutes`. Like the MySQL renderer's `DATE_ADD`, it does not wrap at 24 hours.

```sql
ADDTIME(datetime, SEC_TO_TIME(amount * 60))
```

### SQLite

For a time-of-day `TimeSpan`, `ExpressoDbFunctions.AddMinutes`:

```sql
datetime(datetime, printf('%d minutes', amount))
```

### DB2

`ExpressoDbFunctions.AddMinutes` is `ADD_MINUTES(datetime, amount)` on a `DateTime`. DB2 has no `ADD_MINUTES` for `time`, so a time-of-day `TimeSpan` goes through a timestamp:

```sql
TIME(ADD_MINUTES(TIMESTAMP('2000-01-01', datetime), amount))
```

## EF6 rendering

### All providers

The canonical `DbFunctions.AddMinutes(datetime, amount)`, on a `DateTime` or a time-of-day `TimeSpan`, used by SQL Server and PostgreSQL. On SQL Server:

```sql
CAST( DATEADD (minute, @p__linq__0, [Extent1].[created_at]) AS datetime2)
```

### MySQL / MariaDB

On a `DateTime`, `Ef6Functions.MySqlTimestamp` and `Ef6Functions.MySqlSecToTime` give `TIMESTAMP(datetime, SEC_TO_TIME(amount * 60))`. On a time-of-day `TimeSpan`, `Ef6Functions.MySqlAddTime` gives `ADDTIME(datetime, SEC_TO_TIME(amount * 60))`. On MySQL 8 (not MariaDB), comparing that time-of-day result with a `TimeSpan` parameter never matches, because MySql.Data sends the parameter as `'0 hh:mm:ss.ffffff'`; this is driver-level and does not throw (see [docs/semantics.md](../../semantics.md)).

### Not supported

Expresso throws `NotSupportedException` on these providers:

- Oracle: "the provider pastes the amount into an INTERVAL literal, so parameters fail (ORA-01867)".
- SQLite: "the provider translates no canonical date arithmetic".

## Notes

- Negative and zero amounts are supported. `addminutes(createdat,-30)` looks back 30 minutes.
- You can add whole minutes only, as an `int`. See [`addhours`](addhours.md) for a larger unit and [`addseconds`](addseconds.md) for a smaller one.
