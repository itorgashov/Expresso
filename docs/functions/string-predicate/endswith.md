# `endswith`

True if the string ends with the given suffix.

## Syntax

```text
endswith(text, suffix)
```

Exactly 2 arguments.

- **Category:** String predicate
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `testExpression` | `string` |
| 2 | `matchToExpression` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Endswith() function should have 2 arguments."`
- **Parser coercion:** both arguments are coerced to `string` literals if they are quoted string tokens.
- **IR construction** (`StrEndswithFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - Either argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
Wildcard escaping of a **string literal** pattern (`\` then `%` then `_`, in that order) is done in C# before binding on **all** dialects.
`%` is **prepended** for a literal suffix: `endswith(name,"hn")` binds `"%hn"`.

### SQL Server, PostgreSQL, SQLite, Oracle, DB2

Literal suffix:

```sql
(text LIKE @p ESCAPE '\')
```

Non-literal suffix:

```sql
-- SQL Server
(text LIKE ('%' + REPLACE(REPLACE(REPLACE(suffix, '\', '\\'), '%', '\%'), '_', '\_')) ESCAPE '\')

-- PostgreSQL, SQLite, Oracle, DB2
(text LIKE ('%' || REPLACE(REPLACE(REPLACE(suffix, '\', '\\'), '%', '\%'), '_', '\_')) ESCAPE '\')
```

### MySQL / MariaDB

```sql
(text LIKE @p ESCAPE '\\')
```

Non-literal suffix:

```sql
(text LIKE CONCAT('%', REPLACE(REPLACE(REPLACE(suffix, '\\', '\\\\'), '%', '\\%'), '_', '\\_')) ESCAPE '\\')
```

## Notes

- See [`startswith`](startswith.md) and [`contains`](contains.md) for the other `LIKE`-based predicates.
