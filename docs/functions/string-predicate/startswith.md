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

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.StartsWith(prefix)
```

The TRUE condition is `text != null && prefix != null && text.StartsWith(prefix)`. The FALSE condition, used by `not(...)`, is `text != null && prefix != null && !text.StartsWith(prefix)`. When either argument is NULL, neither holds. Example: `startswith(name,"Jo")` builds `e => e.Name != null && e.Name.StartsWith(p0)`, where `p0` is the captured parameter `"Jo"`; the LINQ provider adds the `%` wildcard and the escaping. Whether the match ignores case depends on the engine and column collation: see [docs/semantics.md](../../semantics.md).

### In-memory

`text.StartsWith(prefix, StringComparison.Ordinal)` is a case-sensitive, culture-independent match, like PostgreSQL `LIKE`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. The parameter carries the escaped prefix with `%` appended. On SQL Server:

```sql
[w].[Name] LIKE @__Value_0_startswith ESCAPE N'\'
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server it becomes `LIKE`, escaping the parameter with `~`:

```sql
([Extent1].[Name] IS NOT NULL) AND ([Extent1].[Name] LIKE @p__linq__0 ESCAPE N'~')
```

### SQLite

The provider translates `StartsWith` to `CHARINDEX(prefix, text) = 1`, which is case-sensitive and false for an empty prefix, and it rejects `LIKE ... ESCAPE`. The override compares lower-cased values, which matches the SQL renderer's `LIKE`: SQLite `LIKE` and `LOWER` both fold ASCII case only. An empty prefix matches every non-NULL value:

```sql
([Extent1].[Name] IS NOT NULL) AND ((0 = (LENGTH(@p__linq__0))) OR ((CHARINDEX(LOWER(@p__linq__1), LOWER([Extent1].[Name]))) = 1))
```

Every EF6 provider supports `startswith`.

## Notes

- See [`endswith`](endswith.md) and [`contains`](contains.md) for the other `LIKE`-based predicates — all three share the same escaping rules, differing only in where `%` is placed.
