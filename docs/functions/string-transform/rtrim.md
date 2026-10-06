# `rtrim`

Removes trailing (right-side) whitespace from a string.

## Syntax

```text
rtrim(text)
```

Exactly 1 argument.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- Passing any number of arguments other than 1 throws `System.Exception` with the message `"Rtrim() function should have 1 argument."`
- A quoted string token is treated as a `string` literal.
- A `null` argument throws `ArgumentNullException`.
- An argument that does not return `string` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
RTRIM(text)
```

SQL Server example: `eq(rtrim(name),"Ada")` renders as `(RTRIM([name]) = @wparam_0)`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.TrimEnd()
```

On `netstandard2.0`, the build EF6 uses, the call is `text.TrimEnd(new char[0])` because that target has no parameterless overload. The result is NULL when `text` is NULL. Example: `eq(rtrim(name),"Ada")` builds `e => e.Name != null && e.Name.TrimEnd() == p0`, where `p0` is a captured parameter. The database decides what is trimmed: SQL `RTRIM` removes spaces only, while .NET `TrimEnd()` also removes tabs and line breaks. See [docs/semantics.md](../../semantics.md).

### In-memory

`ExpressoFunctions.RTrim(text)` removes trailing spaces only, like SQL `RTRIM`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
RTRIM([w].[Name])
```

A literal argument stays in `RTRIM`. SQL removes spaces only, so a trailing tab is kept. On Oracle, `isnull` of that call checks `RTRIM` and is FALSE.

## EF6 rendering

### All providers

EF6 translates `TrimEnd(new char[0])` with the canonical `RTrim` function. The empty array is an array-creation expression in the tree, which is the form EF6 needs. On SQL Server:

```sql
RTRIM([Extent1].[Name])
```

Every EF6 provider supports `rtrim`.

## Notes

- Available on all supported SQL Server versions (unlike [`trim`](trim.md), which needs SQL Server 2017+).
- See [`ltrim`](ltrim.md) for the left-side equivalent.
