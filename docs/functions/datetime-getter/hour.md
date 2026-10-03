# `hour`

The hour component (0–23) of a `DateTime` value.

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

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Hour() function should have 1 argument."`
- **Parser coercion:** argument coerced to `DateTime` if a quoted date/time token.
- **IR construction** (`HourFunc`, via base `DateTimeSingleArgIntFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not an allowed time type → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server

```sql
DATEPART(hour, datetime)
```

Example: `gte(hour(createdat),9)` renders as `(DATEPART(hour, [created_at]) >= @wparam_0)`.

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

A time-of-day `TimeSpan` becomes `DATEPART(hour, [w].[Opens])` on SQL Server.

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

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Hour` function. On SQL Server:

```sql
DATEPART (hour, [Extent1].[created_at])
```

Every EF6 provider supports `hour` on a `DateTime`. Oracle and SQLite have no time-of-day (`Edm.Time`) type, so a `TimeSpan` argument fails there.

## Notes

- 24-hour clock, matching C#'s `DateTime.Hour`. See [`minute`](minute.md) and [`second`](second.md) for the other time components.
