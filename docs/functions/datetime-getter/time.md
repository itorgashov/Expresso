# `time`

Converts a value to a time of day. The database does the conversion; the function itself performs no conversion in C#.

**Availability:** all package TFMs (`netstandard2.0` and `net6.0`).

## Syntax

```text
time(value)
```

Exactly 1 argument.

- **Category:** DateTime getter / conversion
- **Return type:** `TimeOnly` on **net6.0**; `TimeSpan` (time-of-day) on **netstandard2.0**

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `value` | `DateTime`, `string`; plus `TimeOnly` on net6.0 or `TimeSpan` on netstandard2.0 |

## Validation & exceptions

- If you pass any number of arguments other than 1, parsing fails with `System.Exception`: `"Time() function should have 1 argument."`
- The parser treats a quoted value as a string literal. It does not parse it as a `DateTime` in C#.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not listed in the Arguments table throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
The database does the conversion; the function itself does not convert in C#.

### SQL Server, PostgreSQL, MySQL / MariaDB, DB2

```sql
CAST(value AS time)
```

SQL Server example: `eq(time(createdat),"14:30")` renders as `(CAST([created_at] AS time) = @wparam_0)`. The parameter is a `TimeSpan` on netstandard2.0 and a `TimeOnly` on net6.0.

### SQLite

```sql
time(value)
```

### Oracle

```sql
(value - TRUNC(value))
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
TimeOnly.FromDateTime(value)
```

On netstandard2.0 the shape is `value.TimeOfDay`. A `TimeOnly` (net6.0) or `TimeSpan` (netstandard2.0) argument is passed through unchanged. Unlike SQL rendering, a string literal is parsed in C# with the invariant culture (`TimeOnly.Parse`, or `TimeSpan.Parse` on netstandard2.0) and sent as a parameter; any other string argument throws `NotSupportedException` ("TimeFunc accepts a string argument only as a literal in LINQ rendering."). The result is NULL when `value` is NULL. Example: `eq(time(createdat),"14:30")` builds `e => TimeOnly.FromDateTime(e.CreatedAt) == p0`, where `p0` is a captured `TimeOnly` parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda on PostgreSQL. Every other provider overrides it with `ExpressoDbFunctions.Time`.

### SQL Server, MySQL / MariaDB

```sql
CAST(value AS time)
```

On SQL Server, this is `CAST([w].[created_at] AS time)`.

### SQLite

```sql
time(value)
```

### Oracle

```sql
value - TRUNC(value)
```

The result is `INTERVAL DAY TO SECOND` and keeps fractional seconds, so `hour(time(createdat))` and `addseconds` on that result stay on an interval. A comparison with a `TimeOnly` literal binds the parameter as `TimeSpan`. A `TimeOnly` column mapped as text (`NVARCHAR2`) still throws `NotSupportedException`; map it to `INTERVAL DAY(0) TO SECOND(0)`. See [docs/semantics.md](../../semantics.md).

### DB2

```sql
TIME(value)
```

## EF6 rendering

### All providers

EF6 runs on net48, where `time` returns `TimeSpan`. SQL Server uses the canonical `DbFunctions.CreateTime`:

```csharp
DbFunctions.CreateTime(value.Hour, value.Minute, value.Second + value.Millisecond / 1000.0)
```

On SQL Server:

```sql
convert (time, convert(varchar(255), DATEPART (hour, [Extent1].[created_at])) + ':'
    + convert(varchar(255), DATEPART (minute, [Extent1].[created_at])) + ':'
    + str( CAST( DATEPART (second, [Extent1].[created_at]) AS float)
        + ( CAST( DATEPART (millisecond, [Extent1].[created_at]) AS float) / cast(1000 as float(53))), 10, 7), 121)
```

This keeps millisecond precision, where the SQL renderer keeps 100 ns; see [docs/semantics.md](../../semantics.md).

### PostgreSQL

```csharp
DbFunctions.AddMilliseconds(TimeSpan.Zero, DbFunctions.DiffMilliseconds(DbFunctions.TruncateTime(value), value))
```

### MySQL / MariaDB

`Ef6Functions.MySqlMakeTime(value.Hour, value.Minute, value.Second)`, the store function `MAKETIME`, which drops fractional seconds. On MySQL 8 (not MariaDB) the comparison never matches, because MySql.Data sends the `TimeSpan` parameter as `'0 hh:mm:ss.ffffff'`; this is driver-level and does not throw (see [docs/semantics.md](../../semantics.md)).

Oracle and SQLite throw `NotSupportedException`: "the provider has no time-of-day (Edm.Time) type".

## Notes

- On net6.0, compare `time(...)` results to `TimeOnly` fields or literals. On netstandard2.0, use `TimeSpan` fields or literals (for example `eq(opens,"09:00")` on a SQL `time` column).
- Do not use `TimeSpan` in the field catalog for SQL `interval` columns. Expresso treats `TimeSpan` as a clock time of day only.
- Pair with [`hour`](hour.md), [`minute`](minute.md), [`second`](second.md) for time components.
