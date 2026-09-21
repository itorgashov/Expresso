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

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
Result is **0-based** on every dialect (`-1` if not found), matching C# `string.IndexOf`. Contrast with [`substring`](../string-transform/substring.md), which stays 1-based.

### SQL Server

Native `CHARINDEX` is 1-based and returns `0` when missing; Expresso remaps that to `-1`:

```sql
(ISNULL(NULLIF(CHARINDEX(find, text), 0), 0) - 1)
```

Example: `eq(indexof(title,"War"),0)` renders as `((ISNULL(NULLIF(CHARINDEX(@wparam_0, [title]), 0), 0) - 1) = @wparam_1)`.

### PostgreSQL

```sql
(STRPOS(text, find) - 1)
```

### SQLite, Oracle

```sql
(INSTR(text, find) - 1)
```

### MySQL / MariaDB, DB2

```sql
(LOCATE(find, text) - 1)
```

## Notes

- See [`len`](len.md) for the other `int`-returning string function.
