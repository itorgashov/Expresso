# `dayofweek`

The day-of-week component of a `DateTime` value, normalized to match C#'s `DayOfWeek` enum (`Sunday = 0` … `Saturday = 6`).

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

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Dayofweek() function should have 1 argument."`
- **Parser coercion:** argument coerced to `DateTime` if a quoted date/time token.
- **IR construction** (`DayOfWeekFunc`, via base `DateTimeSingleArgIntFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `DateTime` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
Every dialect is normalized to C# `DayOfWeek` (`Sunday = 0` ... `Saturday = 6`).

### SQL Server

Session-independent via `@@DATEFIRST`:

```sql
((DATEPART(weekday, datetime) + @@DATEFIRST - 1) % 7)
```

Example: `eq(dayofweek(createdat),0)` renders as `(((DATEPART(weekday, [created_at]) + @@DATEFIRST - 1) % 7) = @wparam_0)`.

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

## Notes

- SQL Server's native `DATEPART(weekday, ...)` is **session-dependent**: its numbering shifts with the `@@DATEFIRST` setting (which day is "day 1"). This formula normalizes that back to the fixed, session-independent C# convention (`Sunday=0` … `Saturday=6`) by reading `@@DATEFIRST` at query time — it is correct **regardless of the session's `DATEFIRST` value**, not just the SQL Server default of `7` (Sunday).
- See [`day`](day.md) for day-of-month and [`dayofyear`](dayofyear.md) for day-of-year.
