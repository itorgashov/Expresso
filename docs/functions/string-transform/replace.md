# `replace`

Replaces every occurrence of a substring within a string with another string.

## Syntax

```text
replace(text, oldValue, newValue)
```

Exactly 3 arguments.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `sourceString` | `string` |
| 2 | `oldValue` | `string` |
| 3 | `newValue` | `string` |

## Validation & exceptions

- Passing any number of arguments other than 3 throws `System.Exception` with the message `"Replace() function should have 3 arguments."`
- Quoted string tokens in any of the three arguments are treated as `string` literals.
- A `null` argument throws `ArgumentNullException`.
- An argument that does not return `string` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
REPLACE(text, oldValue, newValue)
```

SQL Server example: `eq(replace(isbn,"-",""),"9780000000000")` renders as `(REPLACE([isbn], @wparam_0, @wparam_1) = @wparam_2)`. Every occurrence is replaced.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.Replace(oldValue, newValue)
```

The result is NULL when any argument is NULL. Example: `eq(replace(isbn,"-",""),"9780000000000")` builds `e => e.Isbn != null && e.Isbn.Replace(p0, p1) == p2`, where `p0`, `p1` and `p2` are captured parameters. Whether `oldValue` matches case-insensitively is engine-defined: see [docs/semantics.md](../../semantics.md).

### In-memory

`ExpressoFunctions.Replace(text, oldValue, newValue)` is ordinal and, like PostgreSQL `REPLACE`, leaves the string unchanged when `oldValue` is empty, where `string.Replace` would throw.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
REPLACE([w].[Isbn], @__Value_0, @__Value_1)
```

On Oracle an empty string is NULL, and `REPLACE` gives the arguments different roles. An empty source makes the result NULL. An empty search leaves the source unchanged. An empty replacement deletes matches, and the result is NULL when that deletion leaves an empty string. `isnull(replace(name,"a",""))` and `eq(replace(name,"","x"),"Bob")` both keep `REPLACE` in the SQL.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Replace` function. On SQL Server:

```sql
REPLACE([Extent1].[Isbn], @p__linq__0, @p__linq__1)
```

Every EF6 provider supports `replace`.

On Oracle the arguments have the same roles as in EF Core. NULL comes from the source, or from a result that `REPLACE` itself returns as NULL. A NULL search leaves the source unchanged, and a NULL replacement deletes matches. `isnull(replace("Bob",name,"x"))` is FALSE when `name` is NULL.

## Notes

- `replace` replaces every occurrence of `oldValue`, matching SQL Server's `REPLACE` semantics.
