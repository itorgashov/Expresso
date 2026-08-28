# `date`

Converts a value to a SQL Server `date` (calendar day only). Conversion happens in SQL via `CAST`; no C# conversion is performed in the function itself.

## Syntax

```text
date(value)
```

Exactly 1 argument.

- **Category:** DateTime getter / conversion
- **Return type:** `DateOnly` on **net6.0**; `DateTime` on **netstandard2.0**

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `value` | `DateTime`, `string`, or `DateOnly` (net6.0) |

On **netstandard2.0**, only `DateTime` is accepted.

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Date() function should have 1 argument."`
- **Parser coercion:** quoted tokens become **string** literals (not parsed as `DateTime` in C#).
- **IR construction** (`DateFunc`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not allowed → `ArgumentException`

## SQL Server rendering

```sql
CAST(value AS date)
```

Example: `eq(date(createdat),"2020-01-01")` renders as `(CAST([created_at] AS date) = @wparam_0)` where the parameter is a `DateOnly` literal on net6.0.

## Notes

- On **net6.0**, compare `date(...)` results to `DateOnly` fields/literals, not raw `DateTime` fields. Use `eq(date(createdat), date(other))` or compare to a `DateOnly` literal.
- Equivalent to comparing "same calendar day" when the underlying column is `datetime`/`datetime2`.
