# `contains`

True if the string contains the given substring anywhere within it.

## Syntax

```text
contains(text, substring)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Contains() function should have 2 arguments."`
- **Parser coercion:** both arguments are coerced to `string` literals if they are quoted string tokens.
- **IR construction** (`StrContainsFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - Either argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
Wildcard escaping of a **string literal** pattern (`\` then `%` then `_`, in that order) is done in C# before binding on **all** dialects.
`%` is added on **both** sides for a literal substring: `contains(title,"ar")` binds `"%ar%"`; `contains(title,"100%")` binds `"%100\%%"`.

### SQL Server, PostgreSQL, SQLite, Oracle, DB2

Literal substring:

```sql
(text LIKE @p ESCAPE '\')
```

Non-literal substring:

```sql
-- SQL Server
(text LIKE ('%' + REPLACE(REPLACE(REPLACE(substring, '\', '\\'), '%', '\%'), '_', '\_') + '%') ESCAPE '\')

-- PostgreSQL, SQLite, Oracle, DB2
(text LIKE ('%' || REPLACE(REPLACE(REPLACE(substring, '\', '\\'), '%', '\%'), '_', '\_') || '%') ESCAPE '\')
```

### MySQL / MariaDB

```sql
(text LIKE @p ESCAPE '\\')
```

Non-literal substring:

```sql
(text LIKE CONCAT('%', REPLACE(REPLACE(REPLACE(substring, '\\', '\\\\'), '%', '\\%'), '_', '\\_'), '%') ESCAPE '\\')
```

## Notes

- See [`startswith`](startswith.md) and [`endswith`](endswith.md) for the other `LIKE`-based predicates.
