# `replace`

Replaces all occurrences of a substring within a string.

## Syntax

```text
replace(text, oldValue, newValue)
```

Exactly 3 arguments.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `sourceString` | `string` |
| 2 | `oldValue` | `string` |
| 3 | `newValue` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 3` → `System.Exception`: `"Replace() function should have 3 arguments."`
- **Parser coercion:** all three arguments are coerced to `string` if they are quoted string tokens.
- **IR construction** (`ReplaceFunc`):
  - Any argument is `null` → `ArgumentNullException`
  - Any argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
REPLACE(text, oldValue, newValue)
```

Example: `eq(replace(isbn,"-",""),"9780000000000")` renders as `(REPLACE([isbn], @wparam_0, @wparam_1) = @wparam_2)` on SQL Server. Replaces every occurrence.

## Notes

- Replaces **every** occurrence of `oldValue`, matching SQL Server's `REPLACE` semantics.
