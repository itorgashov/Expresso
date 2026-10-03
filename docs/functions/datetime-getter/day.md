# `day`

Returns the day of the month (1–31) of a date.

## Syntax

```text
day(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime` or `DateOnly` (net6.0) |

## Validation & exceptions

- If you pass any number of arguments other than 1, parsing fails with `System.Exception`: `"Day() function should have 1 argument."`
- The parser converts a quoted date or time string to `DateTime`.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not listed in the Arguments table throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, MySQL / MariaDB, DB2

```sql
DAY(datetime)
```

SQL Server example: `eq(day(createdat),15)` renders as `(DAY([created_at]) = @wparam_0)`.

### PostgreSQL

```sql
CAST(EXTRACT(DAY FROM datetime) AS integer)
```

### SQLite

```sql
CAST(strftime('%d', datetime) AS integer)
```

### Oracle

```sql
EXTRACT(DAY FROM datetime)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.Day
```

The result is NULL when `datetime` is NULL. Example: `eq(day(createdat),15)` builds `e => e.CreatedAt.Day == p0`, where `createdat` is a non-nullable `DateTime` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEPART(day, [w].[created_at])
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Day` function. On SQL Server:

```sql
DATEPART (day, [Extent1].[created_at])
```

Every EF6 provider supports `day`.

## Notes

- This is the day of the month, not the day of the year. See [`dayofyear`](dayofyear.md) for the day of the year and [`dayofweek`](dayofweek.md) for the weekday.
