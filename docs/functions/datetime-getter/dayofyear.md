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
  - Argument's `ReturnType` is not `DateTime` → `ArgumentException`

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

## Notes

- Matches C#'s `DateTime.DayOfYear` (1-based). See [`day`](day.md) for day-of-month instead.
