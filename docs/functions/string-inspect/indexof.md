# `indexof`

0-based index of the first occurrence of a substring within a string, or `-1` if not found.

## Syntax

```text
indexof(text, find)
```

Exactly 2 arguments.

- **Category:** String inspection
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `sourceString` | `string` |
| 2 | `find` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Indexof() function should have 2 arguments."`
- **Parser coercion:** both arguments coerced to `string` if they are quoted string tokens.
- **IR construction** (`IndexOfFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - Either argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
Result is **0-based** on every dialect (`-1` if not found), matching C# `string.IndexOf`. An empty `find` returns `0` (on Oracle `NULL`, because `''` is `NULL` there), and a `NULL` argument returns `NULL`. Contrast with [`substring`](../string-transform/substring.md), which stays 1-based.

### SQL Server

Native `CHARINDEX` is 1-based and returns `0` both when `find` is missing and when it is empty. Expresso returns `-1` for missing and `0` for empty, testing emptiness with `DATALENGTH` because SQL Server's `=` and `LEN` ignore trailing spaces:

```sql
(CASE WHEN DATALENGTH(find) = 0 AND text IS NOT NULL THEN 0 ELSE CHARINDEX(find, text) - 1 END)
```

Both arguments are rendered twice, so a literal `find` binds two parameters. Example: `eq(indexof(title,"War"),0)` renders as `((CASE WHEN DATALENGTH(@wparam_0) = 0 AND [title] IS NOT NULL THEN 0 ELSE CHARINDEX(@wparam_1, [title]) - 1 END) = @wparam_2)`.

### PostgreSQL

```sql
(STRPOS(text, find) - 1)
```

### SQLite, Oracle

```sql
(INSTR(text, find) - 1)
```

### MySQL / MariaDB, DB2

```sql
(LOCATE(find, text) - 1)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.IndexOf(find)
```

The result is NULL when `text` or `find` is NULL. Example: `eq(indexof(title,"War"),0)` builds `e => e.Title != null && e.Title.IndexOf(p0) == p1`, where `p0` and `p1` are captured parameters. Whether the match ignores case is engine-defined (SQL Server `CHARINDEX` follows the collation): see [docs/semantics.md](../../semantics.md).

### In-memory

`text.IndexOf(find, StringComparison.Ordinal)` is a case-sensitive, culture-independent match, like PostgreSQL `STRPOS`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQLite:

```sql
instr("w"."Title", @__Value_0) - 1
```

### SQL Server

EF Core's own translation tests `@find = N''`, which is also true for `' '` because `=` ignores trailing spaces. The marker `ExpressoDbFunctions.IndexOf(text, find)` registers the SQL renderer's form instead:

```sql
CASE WHEN DATALENGTH(find) = 0 AND text IS NOT NULL THEN 0 ELSE CHARINDEX(find, text) - 1 END
```

EF Core drops `text IS NOT NULL` when the query already guarantees it, and binds a literal `find` once.

### Oracle

EF Core's own translation is `CASE WHEN find IS NULL THEN 0 ELSE INSTR(text, find) - 1 END`, so an empty `find` (bound as `NULL`, because `''` is `NULL` on Oracle) gives `0` instead of the SQL renderer's `NULL`. The marker `ExpressoDbFunctions.IndexOf(text, find)` translates to:

```sql
INSTR(text, find) - 1
```

### DB2

The marker `ExpressoDbFunctions.IndexOf(text, find)` translates to:

```sql
LOCATE(find, text) - 1
```

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `IndexOf` function.

### SQL Server

Canonical `IndexOf` is `CHARINDEX(find, text) - 1`, which gives `-1` for an empty `find`. The override wraps it in a conditional on the store function `Ef6Functions.SqlServerDataLength` (`DATALENGTH`):

```sql
CASE WHEN ((0 = ( CAST(DATALENGTH(@p__linq__0) AS int))) AND ([Extent1].[Title] IS NOT NULL)) THEN 0 ELSE ( CAST(CHARINDEX(@p__linq__1, [Extent1].[Title]) AS int)) - 1 END
```

### SQLite

The provider also translates `IndexOf` to `CHARINDEX(find, text) - 1`. The override tests `find.Length` (`LENGTH`) instead:

```sql
CASE WHEN ((0 = (LENGTH(@p__linq__0))) AND ([Extent1].[Title] IS NOT NULL)) THEN 0 ELSE (CHARINDEX(@p__linq__1, [Extent1].[Title])) - 1 END
```

Every EF6 provider supports `indexof`.

## Notes

- See [`len`](len.md) for the other `int`-returning string function.
