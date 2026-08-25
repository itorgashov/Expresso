# `ltrim`

Trims leading (left-side) whitespace from a string.

## Syntax

```text
ltrim(text)
```

Exactly 1 argument.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Ltrim() function should have 1 argument."`
- **Parser coercion:** argument coerced to `string` if a quoted string token.
- **IR construction** (`LTrimFunc`, via base `StringSingleArgFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `string` → `ArgumentException`

## SQL Server rendering

```sql
LTRIM(text)
```

Example: `eq(ltrim(name),"Jo ")` renders as `(LTRIM([name]) = @wparam_0)`.

## Notes

- Available on all supported SQL Server versions (unlike [`trim`](trim.md), which needs SQL Server 2017+).
- See [`rtrim`](rtrim.md) for the right-side equivalent.
