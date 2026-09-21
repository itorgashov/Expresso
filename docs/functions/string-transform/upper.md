# `upper`

Converts a string to uppercase.

## Syntax

```text
upper(text)
```

Exactly 1 argument.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Upper() function should have 1 argument."`
- **Parser coercion:** argument coerced to `string` if a quoted string token.
- **IR construction** (`UpperFunc`, via base `StringSingleArgFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
UPPER(text)
```

Example: `eq(upper(code),"ABC")` renders as `(UPPER([code]) = @wparam_0)` on SQL Server.

## Notes

- See [`lower`](lower.md) for the inverse transform.
