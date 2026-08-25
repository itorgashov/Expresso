# `concat`

Concatenates two or more strings.

## Syntax

```text
concat(text1, text2, ...)
```

At least 2 arguments; no upper bound.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1..N | `arguments` | `string` (each) |

## Validation & exceptions

- **Parser arity check:** fewer than 2 arguments → `System.Exception`: `"Concat() function should have at least 2 arguments."`
- **Parser coercion:** every argument is coerced to `string` if it is a quoted string token.
- **IR construction** (`ConcatFunc`):
  - `arguments` is `null` → `ArgumentNullException`
  - Fewer than 2 arguments → `ArgumentException`
  - Any argument's `ReturnType` is not `string` → `ArgumentException`

## SQL Server rendering

```sql
CONCAT(text1, text2, ...)
```

Example: `eq(concat(firstname,lastname),"GeorgeOrwell")` renders as `(CONCAT([firstname], [lastname]) = @wparam_0)`.

## Notes

- **Requires SQL Server 2012 or later** (the `CONCAT` function was introduced there).
- SQL Server's `CONCAT` treats `NULL` arguments as empty strings rather than propagating `NULL` — unlike ordinary `+` string concatenation in T-SQL.
