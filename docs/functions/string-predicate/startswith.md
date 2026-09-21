# `startswith`

True if the string starts with the given prefix.

## Syntax

```text
startswith(text, prefix)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Startswith() function should have 2 arguments."`
- **Parser coercion:** both arguments are coerced to `string` literals if they are quoted string tokens (`CoerceToString`).
- **IR construction** (`StrStartswithFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - Either argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
Wildcard escaping of a **string literal** pattern (`\` then `%` then `_`, in that order) is done in C# before binding on **all** dialects.

### SQL Server, PostgreSQL, SQLite, Oracle, DB2

Literal prefix (parameter value has `%` appended, e.g. `startswith(name,"Jo")` binds `"Jo%"`; `startswith(name,"a_b")` binds `"a\_b%"`):

```sql
(text LIKE @p ESCAPE '\')
```

Non-literal prefix. SQL Server concatenates with `+`; PostgreSQL, SQLite, Oracle, and DB2 use `||`:

```sql
-- SQL Server
(text LIKE (REPLACE(REPLACE(REPLACE(prefix, '\', '\\'), '%', '\%'), '_', '\_') + '%') ESCAPE '\')

-- PostgreSQL, SQLite, Oracle, DB2
(text LIKE (REPLACE(REPLACE(REPLACE(prefix, '\', '\\'), '%', '\%'), '_', '\_') || '%') ESCAPE '\')
```

### MySQL / MariaDB

MySQL treats `\` as a string escape, so the ESCAPE clause is `ESCAPE '\\'`. Non-literal patterns use `CONCAT` and doubled backslashes in the `REPLACE` literals.

Literal prefix:

```sql
(text LIKE @p ESCAPE '\\')
```

Non-literal prefix:

```sql
(text LIKE CONCAT(REPLACE(REPLACE(REPLACE(prefix, '\\', '\\\\'), '%', '\\%'), '_', '\\_'), '%') ESCAPE '\\')
```

## Notes

- See [`endswith`](endswith.md) and [`contains`](contains.md) for the other `LIKE`-based predicates — all three share the same escaping rules, differing only in where `%` is placed.
