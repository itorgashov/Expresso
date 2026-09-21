# `left`

Returns the leftmost N characters of a string.

## Syntax

```text
left(text, length)
```

Exactly 2 arguments.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `sourceString` | `string` |
| 2 | `length` | `int` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Left() function should have 2 arguments."`
- **Parser coercion:** argument 1 coerced to `string`; argument 2 coerced to `int` if a literal token.
- **IR construction** (`LeftFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - `sourceString.ReturnType` is not `string` → `ArgumentException`
  - `length.ReturnType` is not `int` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, PostgreSQL, MySQL / MariaDB, DB2

```sql
LEFT(text, length)
```

Example: `eq(left(isbn,3),"978")` renders as `(LEFT([isbn], @wparam_0) = @wparam_1)` on SQL Server.

### SQLite

```sql
substr(text, 1, length)
```

### Oracle

```sql
SUBSTR(text, 1, length)
```

## Notes

- See [`right`](right.md) for the mirror function, and [`substring`](substring.md) for arbitrary start positions.
