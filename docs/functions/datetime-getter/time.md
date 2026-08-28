# `time`

Converts a value to a SQL Server `time` (time-of-day only). Conversion happens in SQL via `CAST`; no C# conversion is performed in the function itself.

**Availability:** net6.0 package TFM only (`time` is not compiled into `netstandard2.0`).

## Syntax

```text
time(value)
```

Exactly 1 argument.

- **Category:** DateTime getter / conversion
- **Return type:** `TimeOnly`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `value` | `DateTime`, `string`, or `TimeOnly` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Time() function should have 1 argument."`
- **Parser coercion:** quoted tokens become **string** literals (not parsed as `DateTime` in C#).
- **IR construction** (`TimeFunc`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not allowed → `ArgumentException`

## SQL Server rendering

```sql
CAST(value AS time)
```

Example: `eq(time(createdat),"14:30:00")` renders as `(CAST([created_at] AS time) = @wparam_0)`.

## Notes

- Compare `time(...)` results to `TimeOnly` fields/literals, not raw `DateTime` fields.
- Pair with [`hour`](hour.md), [`minute`](minute.md), [`second`](second.md) for time components.
