# `minute`

The minute component (0–59) of a `DateTime` value.

## Syntax

```text
minute(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime`, `TimeSpan` (time-of-day), or `TimeOnly` (net6.0) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Minute() function should have 1 argument."`
- **Parser coercion:** argument coerced to `DateTime` if a quoted date/time token.
- **IR construction** (`MinuteFunc`, via base `DateTimeSingleArgIntFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not one of the types in the table above → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server

```sql
DATEPART(minute, datetime)
```

Example: `eq(minute(createdat),30)` renders as `(DATEPART(minute, [created_at]) = @wparam_0)`.

### PostgreSQL

```sql
CAST(EXTRACT(MINUTE FROM datetime) AS integer)
```

### SQLite

```sql
CAST(strftime('%M', datetime) AS integer)
```

### MySQL / MariaDB, DB2

```sql
MINUTE(datetime)
```

### Oracle

```sql
EXTRACT(MINUTE FROM datetime)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.Minute
```

On a time-of-day `TimeSpan` the property is `datetime.Minutes`. The result is NULL when `datetime` is NULL. Example: `eq(minute(createdat),30)` builds `e => e.CreatedAt.Minute == p0`, where `createdat` is a non-nullable `DateTime` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEPART(minute, [w].[created_at])
```

### MySQL / MariaDB, DB2

For a time-of-day `TimeSpan` only, `ExpressoDbFunctions.Minute`:

```sql
MINUTE(datetime)
```

### SQLite

For a time-of-day `TimeSpan` only, `ExpressoDbFunctions.Minute`:

```sql
CAST(strftime('%M', datetime) AS INTEGER)
```

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Minute` function. On SQL Server:

```sql
DATEPART (minute, [Extent1].[created_at])
```

Every EF6 provider supports `minute` on a `DateTime`. Oracle and SQLite have no time-of-day (`Edm.Time`) type, so a `TimeSpan` argument fails there.

## Notes

- See [`hour`](hour.md) and [`second`](second.md) for the other time components.
