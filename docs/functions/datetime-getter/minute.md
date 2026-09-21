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
| 1 | `argument` | `DateTime` or `TimeOnly` (net6.0) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Minute() function should have 1 argument."`
- **Parser coercion:** argument coerced to `DateTime` if a quoted date/time token.
- **IR construction** (`MinuteFunc`, via base `DateTimeSingleArgIntFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `DateTime` → `ArgumentException`

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

## Notes

- See [`hour`](hour.md) and [`second`](second.md) for the other time components.
