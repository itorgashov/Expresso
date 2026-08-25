# `date`

Truncates a `DateTime` value to midnight, discarding the time-of-day component.

## Syntax

```text
date(datetime)
```

Exactly 1 argument.

- **Category:** DateTime getter
- **Return type:** `DateTime`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `DateTime` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Date() function should have 1 argument."`
- **Parser coercion:** argument coerced to `DateTime` if a quoted date/time token.
- **IR construction** (`DateFunc`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `DateTime` → `ArgumentException`

## SQL Server rendering

```sql
CAST(datetime AS date)
```

Example: `eq(date(createdat),"2020-01-01")` renders as `(CAST([created_at] AS date) = @wparam_0)`.

## Notes

- Unlike the other DateTime getters, `date` returns `DateTime` (not `int`) — the result can be used anywhere a `DateTime` expression is expected, including comparisons against other `DateTime` fields or `date(...)` calls.
- Equivalent to comparing "same calendar day", ignoring the time-of-day portion. Commonly paired with [`eq`](../comparison/eq.md) to match an exact day regardless of the stored time component.
