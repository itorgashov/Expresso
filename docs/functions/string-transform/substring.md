# `substring`

Extracts a substring starting at a given position, for a given length.

## Syntax

```text
substring(text, start, length)
```

Exactly 3 arguments. Alias: `substr` (identical behavior, same arity).

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `sourceString` | `string` |
| 2 | `startIndex` | `int` |
| 3 | `length` | `int` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 3` → `System.Exception`: `"Substring() function should have 3 arguments."` (applies to both `substring` and the `substr` alias).
- **Parser coercion:** argument 1 coerced to `string`; arguments 2 and 3 coerced to `int` (not `byte`/`double`) if they are literal tokens.
- **IR construction** (`SubStringFunc`):
  - Any argument is `null` → `ArgumentNullException`
  - `sourceString.ReturnType` is not `string` → `ArgumentException`
  - `startIndex.ReturnType` or `length.ReturnType` is not `int` → `ArgumentException`

## SQL Server rendering

```sql
SUBSTRING(text, start, length)
```

Example: `eq(substring(name,1,3),"Mar")` renders as `(SUBSTRING([name], @wparam_0, @wparam_1) = @wparam_2)`.

## Notes

- **`start` follows SQL Server's native 1-based `SUBSTRING` convention** — it is passed through unchanged, *not* converted to 0-based. `substring(name,1,2)` extracts the first two characters, matching plain T-SQL. Contrast this with [`indexof`](../string-inspect/indexof.md), which *is* 0-based.
- See [`left`](left.md) and [`right`](right.md) for fixed-anchor substrings.
