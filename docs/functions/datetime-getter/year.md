# `year`

Returns the year of a date.

## Syntax

```text
year(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime` or `DateOnly` (net6.0) |

## Validation & exceptions

- If you pass any number of arguments other than 1, parsing fails with `System.Exception`: `"Year() function should have 1 argument."`
- The parser converts a quoted date or time string to `DateTime`.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not listed in the Arguments table throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, MySQL / MariaDB, DB2

```sql
YEAR(datetime)
```

SQL Server example: `eq(year(createdat),2020)` renders as `(YEAR([created_at]) = @wparam_0)`.

### PostgreSQL

```sql
CAST(EXTRACT(YEAR FROM datetime) AS integer)
```

### SQLite

```sql
CAST(strftime('%Y', datetime) AS integer)
```

### Oracle

```sql
EXTRACT(YEAR FROM datetime)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.Year
```

The result is NULL when `datetime` is NULL. Example: `eq(year(createdat),2020)` builds `e => e.CreatedAt.Year == p0`, where `createdat` is a non-nullable `DateTime` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEPART(year, [w].[created_at])
```

### Oracle

A `DateOnly` stored as `DATE` uses `TO_NUMBER(TO_CHAR(date, 'YYYY'))`. Text storage (`NVARCHAR2`) throws `NotSupportedException`. Map the property to `DATE` (the sample converts `DateOnly` to `DateTime`).

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Year` function. On SQL Server:

```sql
DATEPART (year, [Extent1].[created_at])
```

Every EF6 provider supports `year`.

## Notes

- See [`month`](month.md), [`day`](day.md), [`dayofyear`](dayofyear.md), [`hour`](hour.md), [`minute`](minute.md), [`second`](second.md), [`dayofweek`](dayofweek.md) for the other single-component getters, and [`date`](date.md) for truncating to midnight instead of extracting a component.
