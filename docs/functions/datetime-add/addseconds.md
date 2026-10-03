# `addseconds`

Adds a number of seconds to a date or time of day. A negative amount subtracts seconds.

## Syntax

```text
addseconds(datetime, amount)
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

- If you pass any number of arguments other than 2, parsing fails with `System.Exception`: `"Addseconds() function should have 2 arguments."`
- The parser converts a quoted date or time string in the first argument to `DateTime`, and a literal in the second argument to `int`.
- A `null` in either argument throws `ArgumentNullException`.
- A first argument whose type is not listed in the Arguments table throws `ArgumentException`.
- An `amount` that is not an `int` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`amount` is the bound `int` (zero and negative allowed).

### SQL Server

```sql
DATEADD(second, amount, datetime)
```

SQL Server example: `eq(addseconds(createdat,0), createdat)` renders as `(DATEADD(second, @wparam_0, [created_at]) = [created_at])`.

### PostgreSQL

```sql
(datetime + ((amount) * INTERVAL '1 second'))
```

### SQLite

```sql
datetime(datetime, ((amount) || ' seconds'))
```

### MySQL / MariaDB

```sql
DATE_ADD(datetime, INTERVAL amount SECOND)
```

### Oracle

```sql
(datetime + NUMTODSINTERVAL(amount, 'SECOND'))
```

### DB2

```sql
(datetime + amount SECONDS)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.AddSeconds(amount)
```

On a `DateTime` the amount is passed as `double`. `TimeOnly` has no `AddSeconds`, so it uses `datetime.Add(TimeSpan.FromSeconds(amount))`, and a time-of-day `TimeSpan` uses `datetime + TimeSpan.FromSeconds(amount)`. The result is NULL when `datetime` or `amount` is NULL. Example: `eq(second(addseconds(createdat,5)),5)` builds `e => e.CreatedAt.AddSeconds((double)p0).Second == p1`, where `createdat` is a non-nullable `DateTime` column and `p0` and `p1` are captured parameters; `eq(second(addseconds(opens,3605)),5)` builds `e => (e.Opens + TimeSpan.FromSeconds((double)p0)).Seconds == p1`.

### In-memory

On a time-of-day `TimeSpan`, `ExpressoFunctions.AddTimeOfDay(datetime, TimeSpan.FromSeconds(amount))` wraps at 24 hours like PostgreSQL `time` arithmetic, so 23:59:59 plus 2 seconds is 00:00:01. Other types are the same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEADD(second, CAST(@__p_0 AS int), [w].[created_at])
```

### SQL Server

For a time-of-day `TimeSpan`, `ExpressoDbFunctions.AddSeconds`:

```sql
DATEADD(second, amount, datetime)
```

### MySQL / MariaDB

For a time-of-day `TimeSpan`, `ExpressoDbFunctions.AddSeconds`. Like the MySQL renderer's `DATE_ADD`, it does not wrap at 24 hours.

```sql
ADDTIME(datetime, SEC_TO_TIME(amount * 1))
```

### SQLite

For a time-of-day `TimeSpan`, `ExpressoDbFunctions.AddSeconds`:

```sql
datetime(datetime, printf('%d seconds', amount))
```

### DB2

`ExpressoDbFunctions.AddSeconds` is `ADD_SECONDS(datetime, amount)` on a `DateTime`. DB2 has no `ADD_SECONDS` for `time`, so a time-of-day `TimeSpan` goes through a timestamp:

```sql
TIME(ADD_SECONDS(TIMESTAMP('2000-01-01', datetime), amount))
```

## EF6 rendering

### All providers

The canonical `DbFunctions.AddSeconds(datetime, amount)`, on a `DateTime` or a time-of-day `TimeSpan`, used by SQL Server and PostgreSQL. On SQL Server:

```sql
CAST( DATEADD (second, @p__linq__0, [Extent1].[created_at]) AS datetime2)
```

### MySQL / MariaDB

On a `DateTime`, `Ef6Functions.MySqlTimestamp` and `Ef6Functions.MySqlSecToTime` give `TIMESTAMP(datetime, SEC_TO_TIME(amount * 1))`. On a time-of-day `TimeSpan`, `Ef6Functions.MySqlAddTime` gives `ADDTIME(datetime, SEC_TO_TIME(amount * 1))`. On MySQL 8 (not MariaDB), comparing that time-of-day result with a `TimeSpan` parameter never matches, because MySql.Data sends the parameter as `'0 hh:mm:ss.ffffff'`; this is driver-level and does not throw (see [docs/semantics.md](../../semantics.md)).

### Not supported

Expresso throws `NotSupportedException` on these providers:

- Oracle: "the provider pastes the amount into an INTERVAL literal, so parameters fail (ORA-01867)".
- SQLite: "the provider translates no canonical date arithmetic".

## Notes

- Negative and zero amounts are supported. `addseconds(createdat,0)` returns `createdat` unchanged.
- You can add whole seconds only. Expresso v1 does not expose milliseconds or microseconds. See [`addminutes`](addminutes.md) for the next larger unit.
