# `month`

Returns the month (1–12) of a date.

## Syntax

```text
month(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime` or `DateOnly` (net6.0) |

## Validation & exceptions

- If you pass any number of arguments other than 1, parsing fails with `System.Exception`: `"Month() function should have 1 argument."`
- The parser converts a quoted date or time string to `DateTime`.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not listed in the Arguments table throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, MySQL / MariaDB, DB2

```sql
MONTH(datetime)
```

SQL Server example: `eq(month(createdat),1)` renders as `(MONTH([created_at]) = @wparam_0)`.

### PostgreSQL

```sql
CAST(EXTRACT(MONTH FROM datetime) AS integer)
```

### SQLite

```sql
CAST(strftime('%m', datetime) AS integer)
```

### Oracle

```sql
EXTRACT(MONTH FROM datetime)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.Month
```

The result is NULL when `datetime` is NULL. Example: `eq(month(createdat),1)` builds `e => e.CreatedAt.Month == p0`, where `createdat` is a non-nullable `DateTime` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEPART(month, [w].[created_at])
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Month` function. On SQL Server:

```sql
DATEPART (month, [Extent1].[created_at])
```

Every EF6 provider supports `month`.

## Notes

- See [`year`](year.md), [`day`](day.md), [`dayofyear`](dayofyear.md) for related component getters.
