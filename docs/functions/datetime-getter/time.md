# `time`

Converts a value to a SQL Server `time` (time-of-day only). Conversion happens in SQL via `CAST`; no C# conversion is performed in the function itself.

**Availability:** all package TFMs (`netstandard2.0` and `net6.0`).

## Syntax

```text
time(value)
```

Exactly 1 argument.

- **Category:** DateTime getter / conversion
- **Return type:** `TimeOnly` on **net6.0**; `TimeSpan` (time-of-day) on **netstandard2.0**

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `value` | `DateTime`, `string`; plus `TimeOnly` on net6.0 or `TimeSpan` on netstandard2.0 |

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

Example (netstandard2.0): `eq(time(createdat),"14:30")` renders as `(CAST([created_at] AS time) = @wparam_0)` with a `TimeSpan` parameter.

Example (net6.0): `eq(time(createdat),"14:30:00")` renders similarly with a `TimeOnly` parameter.

## Notes

- On **net6.0**, compare `time(...)` results to `TimeOnly` fields/literals. On **netstandard2.0**, use `TimeSpan` fields/literals (e.g. `eq(opens,"09:00")` on a SQL `time` column).
- Do not use `TimeSpan` in the field catalog for SQL **interval** columns — Expresso treats it as clock time-of-day only.
- Pair with [`hour`](hour.md), [`minute`](minute.md), [`second`](second.md) for time components.
