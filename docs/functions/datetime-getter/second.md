# `second`

Returns the second (0–59) of a date and time or of a time of day.

## Syntax

```text
second(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime`, `TimeSpan` (time-of-day), or `TimeOnly` (net6.0) |

## Validation & exceptions

- If you pass any number of arguments other than 1, parsing fails with `System.Exception`: `"Second() function should have 1 argument."`
- The parser converts a quoted date or time string to `DateTime`.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not listed in the Arguments table throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server

```sql
DATEPART(second, datetime)
```

SQL Server example: `eq(second(createdat),0)` renders as `(DATEPART(second, [created_at]) = @wparam_0)`.

### PostgreSQL

```sql
CAST(EXTRACT(SECOND FROM datetime) AS integer)
```

### SQLite

```sql
CAST(strftime('%S', datetime) AS integer)
```

### MySQL / MariaDB, DB2

```sql
SECOND(datetime)
```

### Oracle

```sql
EXTRACT(SECOND FROM datetime)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.Second
```

On a time-of-day `TimeSpan` the property is `datetime.Seconds`. The result is NULL when `datetime` is NULL. Example: `eq(second(createdat),0)` builds `e => e.CreatedAt.Second == p0`, where `createdat` is a non-nullable `DateTime` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEPART(second, [w].[created_at])
```

### MySQL / MariaDB, DB2

For a time-of-day `TimeSpan` only, `ExpressoDbFunctions.Second`:

```sql
SECOND(datetime)
```

### SQLite

For a time-of-day `TimeSpan` only, `ExpressoDbFunctions.Second`:

```sql
CAST(strftime('%S', datetime) AS INTEGER)
```

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Second` function. On SQL Server:

```sql
DATEPART (second, [Extent1].[created_at])
```

Every EF6 provider supports `second` on a `DateTime`. Oracle and SQLite have no time-of-day (`Edm.Time`) type, so a `TimeSpan` argument fails there.

## Notes

- This returns whole seconds only. Expresso v1 does not expose milliseconds or microseconds. See [`hour`](hour.md) and [`minute`](minute.md) for the other time components.
