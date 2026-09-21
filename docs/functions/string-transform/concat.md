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

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, PostgreSQL, MySQL / MariaDB

```sql
CONCAT(text1, text2, ...)
```

Example: `eq(concat(firstname,lastname),"GeorgeOrwell")` renders as `(CONCAT([firstname], [lastname]) = @wparam_0)` on SQL Server.
SQL Server `CONCAT` requires SQL Server 2012+ and treats `NULL` arguments as empty strings.

### SQLite, Oracle, DB2

```sql
(text1 || text2 || ...)
```

## Notes

- On SQLite, Oracle, and DB2, `||` typically propagates `NULL` if any operand is `NULL`. SQL Server `CONCAT` (2012+) treats `NULL` as empty string.
