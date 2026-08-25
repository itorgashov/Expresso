# `indexof`

0-based index of the first occurrence of a substring within a string, or `-1` if not found.

## Syntax

```text
indexof(text, find)
```

Exactly 2 arguments.

- **Category:** String inspection
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `sourceString` | `string` |
| 2 | `find` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Indexof() function should have 2 arguments."`
- **Parser coercion:** both arguments coerced to `string` if they are quoted string tokens.
- **IR construction** (`IndexOfFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - Either argument's `ReturnType` is not `string` → `ArgumentException`

## SQL Server rendering

```sql
(ISNULL(NULLIF(CHARINDEX(find, text), 0), 0) - 1)
```

Example: `eq(indexof(title,"War"),0)` renders as:

```sql
((ISNULL(NULLIF(CHARINDEX(@wparam_0, [title]), 0), 0) - 1) = @wparam_1)
```

testing whether `"War"` occurs at the very start of `title` (index `0`).

## Notes

- **0-based**, unlike SQL Server's native `CHARINDEX` (which is 1-based). The `-1` offset is applied by the rendered expression, not by SQL Server itself.
- Returns **`-1`** when `find` is not present in `text` (SQL `CHARINDEX` returning `0` is remapped to `0`, then the `-1` offset makes the final "not found" value `-1`) — matching common language conventions like C#'s `string.IndexOf`.
- Contrast with [`substring`](../string-transform/substring.md), whose `start` argument stays SQL's native 1-based convention — the two functions intentionally use different bases.
- See [`len`](len.md) for the other `int`-returning string function.
