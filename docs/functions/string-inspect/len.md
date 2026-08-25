# `len`

Length of a string, in characters.

## Syntax

```text
len(text)
```

Exactly 1 argument.

- **Category:** String inspection
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Len() function should have 1 argument."`
- **Parser coercion:** argument coerced to `string` if a quoted string token.
- **IR construction** (`LenFunc`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `string` → `ArgumentException`

## SQL Server rendering

```sql
LEN(text)
```

Example: `gt(len(title),50)` renders as `(LEN([title]) > @wparam_0)`.

## Notes

- Maps directly to SQL Server's `LEN`, which **does not count trailing spaces** (`LEN('abc  ')` is `3`, not `5`). Use `DATALENGTH` in raw SQL if you need byte-exact length including trailing spaces — Expresso does not expose that as a query function in v1.
- `LEN(NULL)` is `NULL` in SQL Server; compare against `isnull(...)` if you need to special-case missing values.
- See [`indexof`](indexof.md) for the other `int`-returning string function.
