# `startswith`

Returns `true` when a string starts with the given prefix.

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

- Passing any number of arguments other than 2 throws `System.Exception` with the message `"Startswith() function should have 2 arguments."`
- Quoted string tokens in either argument are treated as `string` literals.
- A `null` argument throws `ArgumentNullException`.
- An argument that does not return `string` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
Expresso escapes wildcards in a string literal pattern (`\`, then `%`, then `_`, in that order) in C# before binding, on every dialect.

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

When neither argument references the query row, the predicate is still `LIKE` with the same escaping. `startswith("Ab","a")` is then decided by the engine's collation, not by CLR case rules.

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

- See [`endswith`](endswith.md) and [`contains`](contains.md) for the other `LIKE`-based predicates. All three share the same escaping rules and differ only in where `%` is placed.
