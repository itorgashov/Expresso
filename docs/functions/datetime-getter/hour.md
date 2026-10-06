# `hour`

Returns the hour (0–23) of a date and time or of a time of day.

## Syntax

```text
hour(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime`, `TimeSpan` (time-of-day), or `TimeOnly` (net6.0) |

## Validation & exceptions

- If you pass any number of arguments other than 1, parsing fails with `System.Exception`: `"Hour() function should have 1 argument."`
- The parser converts a quoted date or time string to `DateTime`.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not a date or time type listed in the Arguments table throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server

```sql
DATEPART(hour, datetime)
```

SQL Server example: `gte(hour(createdat),9)` renders as `(DATEPART(hour, [created_at]) >= @wparam_0)`.

### PostgreSQL

```sql
CAST(EXTRACT(HOUR FROM datetime) AS integer)
```

### SQLite

```sql
CAST(strftime('%H', datetime) AS integer)
```

### MySQL / MariaDB, DB2

```sql
HOUR(datetime)
```

### Oracle

```sql
EXTRACT(HOUR FROM datetime)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.Hour
```

On a time-of-day `TimeSpan` the property is `datetime.Hours`. The result is NULL when `datetime` is NULL. Example: `gte(hour(createdat),9)` builds `e => e.CreatedAt.Hour >= p0`, where `createdat` is a non-nullable `DateTime` column and `p0` is a captured parameter; `eq(hour(opens),17)` builds `e => e.Opens.Hours == p0`.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEPART(hour, [w].[created_at])
```

On SQL Server, a time-of-day `TimeSpan` becomes `DATEPART(hour, [w].[Opens])`.

### MySQL / MariaDB, DB2

For a time-of-day `TimeSpan` only, `ExpressoDbFunctions.Hour`:

```sql
HOUR(datetime)
```

### SQLite

For a time-of-day `TimeSpan` only, `ExpressoDbFunctions.Hour`:

```sql
CAST(strftime('%H', datetime) AS INTEGER)
```

### Oracle

A `TimeOnly` stored as `INTERVAL DAY TO SECOND` takes the hour field of `TO_CHAR` of that interval and multiplies it by `-1` when the text starts with `-`, so a negative interval keeps a negative hour. A `DATE` or `TIMESTAMP` column uses `TO_NUMBER(TO_CHAR(value, 'HH24'))` instead. The same marker throws `NotSupportedException` when Oracle EF stores the value as text (`NVARCHAR2`). Map a `TimeOnly` property to `INTERVAL DAY(0) TO SECOND(0)` (the sample converts `TimeOnly` to `TimeSpan`).

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Hour` function. On SQL Server:

```sql
DATEPART (hour, [Extent1].[created_at])
```

Every EF6 provider supports `hour` on a `DateTime`. Oracle and SQLite have no time-of-day (`Edm.Time`) type, so a `TimeSpan` argument fails there.

## Notes

- 24-hour clock, matching C#'s `DateTime.Hour`. See [`minute`](minute.md) and [`second`](second.md) for the other time components.
