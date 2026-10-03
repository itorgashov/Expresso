# `dayofyear`

The day-of-year component (1–366) of a `DateTime` value.

## Syntax

```text
dayofyear(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime` or `DateOnly` (net6.0) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Dayofyear() function should have 1 argument."`
- **Parser coercion:** argument coerced to `DateTime` if a quoted date/time token.
- **IR construction** (`DayOfYearFunc`, via base `DateTimeSingleArgIntFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not one of the types in the table above → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server

```sql
DATEPART(dayofyear, datetime)
```

Example: `eq(dayofyear(createdat),32)` renders as `(DATEPART(dayofyear, [created_at]) = @wparam_0)`.

### PostgreSQL

```sql
CAST(EXTRACT(DOY FROM datetime) AS integer)
```

### SQLite

```sql
CAST(strftime('%j', datetime) AS integer)
```

### MySQL / MariaDB, DB2

```sql
DAYOFYEAR(datetime)
```

### Oracle

```sql
TO_NUMBER(TO_CHAR(datetime, 'DDD'))
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
datetime.DayOfYear
```

The result is NULL when `datetime` is NULL. Example: `eq(dayofyear(createdat),32)` builds `e => e.CreatedAt.DayOfYear == p0`, where `createdat` is a non-nullable `DateTime` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
DATEPART(dayofyear, [w].[created_at])
```

No provider overrides.

## EF6 rendering

### All providers

`DbFunctions` has no day-of-year function, so by default the transformer counts days from January 1 of the same year. PostgreSQL uses this form:

```csharp
DbFunctions.DiffDays(DbFunctions.AddMonths(DbFunctions.AddDays(datetime, 1 - datetime.Day), 1 - datetime.Month), datetime) + 1
```

### SQL Server, Oracle

`Ef6Functions.DayOfYear(datetime)`, the canonical `Edm.DayOfYear` function. On SQL Server:

```sql
DATEPART (dayofyear, [Extent1].[created_at])
```

### MySQL / MariaDB

`Ef6Functions.MySqlDayOfYear(datetime)`, the store function `DAYOFYEAR(datetime)`.

### SQLite

`Ef6Functions.SqliteDatePart("dayofyear", datetime)`, the store function `DATEPART`.

Every EF6 provider supports `dayofyear`.

## Notes

- Matches C#'s `DateTime.DayOfYear` (1-based). See [`day`](day.md) for day-of-month instead.
