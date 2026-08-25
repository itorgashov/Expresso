# `isnull`

True if the argument is `NULL`.

## Syntax

```text
isnull(expr)
```

Exactly 1 argument.

- **Category:** Membership / null
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `bool`, `string`, `byte`, `int`, `double`, or `DateTime` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"IsNull() function should have 1 argument."`
- **Parser coercion:** **none** — unlike most other functions, `isnull`'s argument is *not* passed through literal coercion. In practice this means `isnull` is meant to be used with a field reference (e.g. `isnull(publisher)`), not a raw quoted literal.
- **IR construction** (`IsNullFunc`):
  - `argument` is `null` → `ArgumentNullException`
  - `argument.ReturnType` is not one of the allowed types → `ArgumentException`

## SQL Server rendering

```sql
(expr IS NULL)
```

Example: `isnull(isbn)` renders as `([isbn] IS NULL)`.

## Notes

- Combine with [`not`](../logical/not.md) for "is not null": `not(isnull(isbn))`.
- See [`in`](in.md) for the other membership/null function.
