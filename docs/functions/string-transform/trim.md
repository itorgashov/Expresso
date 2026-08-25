# `trim`

Trims leading and trailing whitespace from a string.

## Syntax

```text
trim(text)
```

Exactly 1 argument.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Trim() function should have 1 argument."`
- **Parser coercion:** argument coerced to `string` if a quoted string token.
- **IR construction** (`TrimFunc`, via base `StringSingleArgFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `string` → `ArgumentException`

## SQL Server rendering

```sql
TRIM(text)
```

Example: `eq(trim(name),"Jo")` renders as `(TRIM([name]) = @wparam_0)`.

## Notes

- **Requires SQL Server 2017 or later** (`TRIM` as a built-in function). For older SQL Server versions, use [`ltrim`](ltrim.md) and [`rtrim`](rtrim.md) together instead, which have been available since early versions.
