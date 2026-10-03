# `trim`

Removes leading and trailing whitespace from a string.

## Syntax

```text
trim(text)
```

Exactly 1 argument.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- Passing any number of arguments other than 1 throws `System.Exception` with the message `"Trim() function should have 1 argument."`
- A quoted string token is treated as a `string` literal.
- A `null` argument throws `ArgumentNullException`.
- An argument that does not return `string` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
TRIM(text)
```

SQL Server example: `eq(trim(name),"Ada")` renders as `(TRIM([name]) = @wparam_0)`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.Trim()
```

The result is NULL when `text` is NULL. Example: `eq(trim(name),"Ada")` builds `e => e.Name != null && e.Name.Trim() == p0`, where `p0` is a captured parameter. The database decides what is trimmed: SQL `TRIM` removes spaces only, while .NET `Trim()` also removes tabs and line breaks. See [docs/semantics.md](../../semantics.md).

### In-memory

`ExpressoFunctions.Trim(text)` removes leading and trailing spaces only, like SQL `TRIM`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
LTRIM(RTRIM([w].[Name]))
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Trim` function. On SQL Server:

```sql
LTRIM(RTRIM([Extent1].[Name]))
```

Every EF6 provider supports `trim`.

## Notes

- On SQL Server, `trim` requires SQL Server 2017 or later, because it uses the built-in `TRIM` function. On older versions, use [`ltrim`](ltrim.md) and [`rtrim`](rtrim.md) together instead. Both have been available since early versions.
