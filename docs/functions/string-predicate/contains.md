# `contains`

Returns `true` when a string contains the given substring anywhere within it.

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

- Passing any number of arguments other than 2 throws `System.Exception` with the message `"Contains() function should have 2 arguments."`
- Quoted string tokens in either argument are treated as `string` literals.
- A `null` argument throws `ArgumentNullException`.
- An argument that does not return `string` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
Expresso escapes wildcards in a string literal pattern (`\`, then `%`, then `_`, in that order) in C# before binding, on every dialect. SQL Server also escapes `[`, so a bracket is a literal character rather than a character class.
For a literal substring, it adds `%` on both sides: `contains(title,"ar")` binds `"%ar%"`, and `contains(title,"100%")` binds `"%100\%%"`.

### SQL Server, PostgreSQL, SQLite, Oracle, DB2

Literal substring:

```sql
(text LIKE @p ESCAPE '\')
```

Non-literal substring:

```sql
-- SQL Server
(text LIKE ('%' + REPLACE(REPLACE(REPLACE(REPLACE(substring, '\', '\\'), '%', '\%'), '_', '\_'), '[', '\[') + '%') ESCAPE '\')

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

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.Contains(substring)
```

The TRUE condition is `text != null && substring != null && text.Contains(substring)`. The FALSE condition, used by `not(...)`, is `text != null && substring != null && !text.Contains(substring)`. When either argument is NULL, neither holds. Example: `contains(title,"ar")` builds `e => e.Title != null && e.Title.Contains(p0)`, where `p0` is the captured parameter `"ar"`; the LINQ provider adds the `%` wildcards and the escaping. Whether the match ignores case depends on the engine and column collation: see [docs/semantics.md](../../semantics.md).

### In-memory

`text.IndexOf(substring, StringComparison.Ordinal) >= 0` is a case-sensitive, culture-independent match, like PostgreSQL `LIKE`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. The parameter carries the escaped substring with `%` on both sides. On SQL Server:

```sql
[w].[Title] IS NOT NULL AND [w].[Title] LIKE @__Value_0_contains ESCAPE N'\'
```

### SQLite

EF Core's own translation is `instr(text, substring) > 0`, which is case-sensitive, while the SQL renderer's `LIKE` ignores ASCII case. The override calls `EF.Functions.Like(text, "%" + escaped + "%", "\")`, where `escaped` doubles `\` and escapes `%` and `_` in the same order as the SQL renderer. A literal pair on every provider uses that same `LIKE`, so `contains("Ab","a")` is not decided by the CLR. A literal substring becomes one parameter holding the escaped pattern (`contains(title,"100%")` binds `'%100\%%'`):

```sql
"w"."Title" IS NOT NULL AND "w"."Title" LIKE @__Concat_1 ESCAPE '\'
```

A non-literal substring renders the same `REPLACE` chain as the SQL renderer.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server it becomes `LIKE`, escaping the parameter with `~`:

```sql
([Extent1].[Title] IS NOT NULL) AND ([Extent1].[Title] LIKE @p__linq__0 ESCAPE N'~')
```

### SQLite

The provider translates `Contains` to `CHARINDEX(substring, text) > 0`, which is case-sensitive and false for an empty substring, and it rejects `LIKE ... ESCAPE`, so the EF Core override cannot be used. The override compares lower-cased values instead, which matches the SQL renderer's `LIKE`: SQLite `LIKE` and `LOWER` both fold ASCII case only. An empty substring matches every non-NULL value:

```sql
([Extent1].[Title] IS NOT NULL) AND ((0 = (LENGTH(@p__linq__0))) OR ((CHARINDEX(LOWER(@p__linq__1), LOWER([Extent1].[Title]))) > 0))
```

Every EF6 provider supports `contains`.

## Notes

- See [`startswith`](startswith.md) and [`endswith`](endswith.md) for the other `LIKE`-based predicates.
