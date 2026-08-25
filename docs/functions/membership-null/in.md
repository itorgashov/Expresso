# `in`

True if the first argument equals **any** of the remaining arguments.

## Syntax

```text
in(probe, candidate1, candidate2, ...)
```

At least 2 arguments total (a probe plus at least one candidate).

- **Category:** Membership / null
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `probe` (arguments[0]) | Any supported type |
| 2..N | `candidates` | **Exactly the same `ReturnType`** as the probe (position 1) — no mixed-numeric leniency |

## Validation & exceptions

- **Parser arity check:** fewer than 2 arguments → `System.Exception`: `"In() function should have at least 2 arguments."`
- **Parser coercion:** the probe's literal type is inferred first (`GetLiteralType`); remaining literal candidates are coerced to that same type.
- **IR construction** (`InFunc`):
  - `arguments` is `null` → `ArgumentNullException`
  - Fewer than 2 arguments → `ArgumentException`
  - Any argument is `null` → `ArgumentException`
  - Any argument's `ReturnType` differs from `arguments[0].ReturnType` → `ArgumentException`

Unlike [`eq`](../comparison/eq.md), `in` requires an **exact** `ReturnType` match — mixed numeric types (e.g. `byte` probe against an `int` candidate) are rejected.

## SQL Server rendering

```sql
(probe IN (candidate1, candidate2, ...))
```

Example: `in(status,1,2,3)` renders as `([status] IN (@wparam_0, @wparam_1, @wparam_2))`.

## Notes

- See [`isnull`](isnull.md) for the other membership/null function.
