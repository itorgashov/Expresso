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

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.EndsWith(suffix)
```

The TRUE condition is `text != null && suffix != null && text.EndsWith(suffix)`. The FALSE condition, used by `not(...)`, is `text != null && suffix != null && !text.EndsWith(suffix)`. When either argument is NULL, neither holds. Example: `endswith(name,"hn")` builds `e => e.Name != null && e.Name.EndsWith(p0)`, where `p0` is the captured parameter `"hn"`; the LINQ provider adds the `%` wildcard and the escaping. Whether the match ignores case depends on the engine and column collation: see [docs/semantics.md](../../semantics.md).

### In-memory

`text.EndsWith(suffix, StringComparison.Ordinal)` is a case-sensitive, culture-independent match, like PostgreSQL `LIKE`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. The parameter carries the escaped suffix with `%` prepended. On SQL Server:

```sql
[w].[Name] LIKE @__Value_0_endswith ESCAPE N'\'
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server it becomes `LIKE`, escaping the parameter with `~`:

```sql
([Extent1].[Name] IS NOT NULL) AND ([Extent1].[Name] LIKE @p__linq__0 ESCAPE N'~')
```

### SQLite

The provider translates `EndsWith` to `CHARINDEX(Reverse(suffix), Reverse(text)) = 1`, which is case-sensitive and false for an empty suffix, and it rejects `LIKE ... ESCAPE`. The override compares lower-cased values, which matches the SQL renderer's `LIKE`: SQLite `LIKE` and `LOWER` both fold ASCII case only. An empty suffix matches every non-NULL value:

```sql
([Extent1].[Name] IS NOT NULL) AND ((0 = (LENGTH(@p__linq__0))) OR ((CHARINDEX(Reverse(LOWER(@p__linq__1)), Reverse(LOWER([Extent1].[Name])))) = 1))
```

Every EF6 provider supports `endswith`.

## Notes

- See [`startswith`](startswith.md) and [`contains`](contains.md) for the other `LIKE`-based predicates.
