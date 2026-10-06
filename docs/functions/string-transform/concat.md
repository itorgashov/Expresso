# `concat`

Concatenates two or more strings into one string.

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

- Parsing a call with fewer than 2 arguments throws `System.Exception` with the message `"Concat() function should have at least 2 arguments."`
- Quoted string tokens in any argument are treated as `string` literals.
- A `null` argument list throws `ArgumentNullException`.
- A list with fewer than 2 arguments throws `ArgumentException`.
- An argument that does not return `string` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, PostgreSQL, MySQL / MariaDB

```sql
CONCAT(text1, text2, ...)
```

SQL Server example: `eq(concat(firstname,lastname),"GeorgeOrwell")` renders as `(CONCAT([firstname], [lastname]) = @wparam_0)`.
SQL Server `CONCAT` requires SQL Server 2012 or later and treats `NULL` arguments as empty strings.

### SQLite, Oracle, DB2

```sql
(text1 || text2 || ...)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
string.Concat(string.Concat(text1, text2), text3)
```

Arguments are joined pairwise, left to right. The result is NULL when any argument is NULL. Example: `eq(concat(firstname,lastname),"GeorgeOrwell")` builds `e => e.Firstname != null && e.Lastname != null && string.Concat(e.Firstname, e.Lastname) == p0`, where `p0` is a captured parameter.

### In-memory

```csharp
string.Concat(text1 ?? "", text2 ?? "")
```

NULL arguments count as empty strings and the result is never NULL, like PostgreSQL `CONCAT`. So `isnull(concat(name,notes))` builds `e => false`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda, so a NULL argument makes the result NULL. This applies to MySQL / MariaDB, SQLite and DB2.

### SQL Server, PostgreSQL, Oracle

These providers use the in-memory NULL-as-empty lambda, matching the SQL renderer, whose SQL Server and PostgreSQL `CONCAT` and Oracle `||` ignore NULL. On SQL Server `eq(concat(firstname,lastname),"GeorgeOrwell")` renders:

```sql
COALESCE([w].[Firstname], N'') + COALESCE([w].[Lastname], N'') = @__Value_0
```

On SQL Server and PostgreSQL the result is never NULL. On Oracle it is NULL when every argument is NULL or empty, like Oracle `||`. `isnull(concat(firstname,lastname))` builds `e => e.Firstname == null && e.Lastname == null`. `isnull(concat("",""))` is TRUE. `isnull(concat(name,""))` is TRUE only when `name` is NULL, and `eq(concat("a",""),"a")` is TRUE.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Concat` function, so a NULL argument makes the result NULL. This applies to MySQL / MariaDB and SQLite.

### SQL Server, PostgreSQL, Oracle

These providers use the same NULL-as-empty lambda as EF Core, including Oracle's NULL when every argument is NULL. On SQL Server each `?? ""` becomes the `CASE` below, EF6 wraps it in its own NULL guard, and the operands are joined with `+`:

```sql
CASE WHEN ([Extent1].[Lastname] IS NULL) THEN N'' ELSE [Extent1].[Lastname] END
```

No EF6 provider throws for `concat`, but on Oracle a query that emits the concatenation fails in the database with ORA-12704: EF6's own NULL guard emits `N''` against `VARCHAR2` columns. That includes a function such as `right` whose argument is the concatenation. `isnull(concat(...))` works there, because it only tests the arguments for NULL.

## Notes

- On SQLite and DB2, `||` returns `NULL` if any operand is `NULL`. Oracle `||` and SQL Server `CONCAT` (2012 and later) treat `NULL` as an empty string. On Oracle the result is still `NULL` when every operand is `NULL` or empty, because Oracle treats `''` as `NULL`.
