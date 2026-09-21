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

## Notes

- 24-hour clock, matching C#'s `DateTime.Hour`. See [`minute`](minute.md) and [`second`](second.md) for the other time components.
