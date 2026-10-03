# `dayofweek`

Returns the day of the week of a date, numbered like the C# `DayOfWeek` enum (`Sunday = 0` … `Saturday = 6`).

## Syntax

```text
dayofweek(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime` or `DateOnly` (net6.0) |

## Validation & exceptions

- If you pass any number of arguments other than 1, parsing fails with `System.Exception`: `"Dayofweek() function should have 1 argument."`
- The parser converts a quoted date or time string to `DateTime`.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not listed in the Arguments table throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
Every dialect is normalized to C# `DayOfWeek` (`Sunday = 0` ... `Saturday = 6`).

### SQL Server

Session-independent via `@@DATEFIRST`:

```sql
((DATEPART(weekday, datetime) + @@DATEFIRST - 1) % 7)
```

SQL Server example: `eq(dayofweek(createdat),0)` renders as `(((DATEPART(weekday, [created_at]) + @@DATEFIRST - 1) % 7) = @wparam_0)`.

### PostgreSQL

```sql
CAST(EXTRACT(DOW FROM datetime) AS integer)
```

### SQLite

```sql
CAST(strftime('%w', datetime) AS integer)
```

### MySQL / MariaDB, DB2

Native `DAYOFWEEK` is 1-7 with Sunday = 1:

```sql
(DAYOFWEEK(datetime) - 1)
```

### Oracle

Assumes `NLS_TERRITORY` where `TO_CHAR(..., 'D')` uses Sunday = 1 (e.g. America):

```sql
(TO_NUMBER(TO_CHAR(datetime, 'D')) - 1)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
(int)datetime.DayOfWeek
```

The result is NULL when `datetime` is NULL. Example: `eq(dayofweek(createdat),0)` builds `e => (int)e.CreatedAt.DayOfWeek == p0`, where `createdat` is a non-nullable `DateTime` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda on PostgreSQL, MySQL / MariaDB and SQLite. On SQLite:

```sql
CAST(strftime('%w', "w"."created_at") AS INTEGER)
```

### SQL Server

`ExpressoDbFunctions.DayOfWeek`, independent of the session's `DATEFIRST`:

```sql
((DATEPART(weekday, datetime) + @@DATEFIRST) - 1) % 7
```

### Oracle

`ExpressoDbFunctions.DayOfWeek`, with the same `NLS_TERRITORY` assumption as the Oracle SQL renderer:

```sql
TO_NUMBER(TO_CHAR(datetime, 'D')) - 1
```

### DB2

`ExpressoDbFunctions.DayOfWeek`:

```sql
DAYOFWEEK(datetime) - 1
```

## EF6 rendering

### All providers

Expresso counts days from a known Sunday (1900-01-07) with the canonical `DbFunctions.DiffDays`, so the result does not depend on `DATEFIRST` or NLS settings. The outer `+ 7) % 7` keeps dates before 1900-01-07 non-negative. SQL Server, PostgreSQL and Oracle use this form:

```csharp
((DbFunctions.DiffDays(new DateTime(1900, 1, 7), datetime) % 7) + 7) % 7
```

On SQL Server:

```sql
((((DATEDIFF (day, convert(datetime2, '1900-01-07 00:00:00.0000000', 121), [Extent1].[created_at])) % 7) + 7) % 7)
```

### MySQL / MariaDB

`Ef6Functions.MySqlDayOfWeek(datetime) - 1`, the store function `DAYOFWEEK` (Sunday = 1).

### SQLite

`Ef6Functions.SqliteDatePart("weekday", datetime)`, the store function `DATEPART`.

Every EF6 provider supports `dayofweek`.

## Notes

- The native SQL Server `DATEPART(weekday, ...)` depends on the session. Its numbering shifts with the `@@DATEFIRST` setting, which decides which day is "day 1". Expresso reads `@@DATEFIRST` at query time and converts the result to the fixed C# convention (`Sunday=0` … `Saturday=6`). The result is correct for any `DATEFIRST` value, not only the SQL Server default of `7` (Sunday).
- See [`day`](day.md) for day-of-month and [`dayofyear`](dayofyear.md) for day-of-year.
