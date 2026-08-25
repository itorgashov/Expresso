# `lower`

Converts a string to lowercase.

## Syntax

```text
lower(text)
```

Exactly 1 argument.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Lower() function should have 1 argument."`
- **Parser coercion:** argument coerced to `string` if a quoted string token.
- **IR construction** (`LowerFunc`, via base `StringSingleArgFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `string` → `ArgumentException`

## SQL Server rendering

```sql
LOWER(text)
```

Example: `eq(lower(email),"a@b.com")` renders as `(LOWER([email]) = @wparam_0)`.

## Notes

- See [`upper`](upper.md) for the inverse transform.
