# `ltrim`

Trims leading (left-side) whitespace from a string.

## Syntax

```text
ltrim(text)
```

Exactly 1 argument.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Ltrim() function should have 1 argument."`
- **Parser coercion:** argument coerced to `string` if a quoted string token.
- **IR construction** (`LTrimFunc`, via base `StringSingleArgFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
LTRIM(text)
```

Example: `eq(ltrim(name),"Ada")` renders as `(LTRIM([name]) = @wparam_0)` on SQL Server.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.TrimStart()
```

On `netstandard2.0`, the build EF6 uses, the call is `text.TrimStart(new char[0])` because that target has no parameterless overload. The result is NULL when `text` is NULL. Example: `eq(ltrim(name),"Ada")` builds `e => e.Name != null && e.Name.TrimStart() == p0`, where `p0` is a captured parameter. The database decides what is trimmed: SQL `LTRIM` removes spaces only, while .NET `TrimStart()` also removes tabs and line breaks. See [docs/semantics.md](../../semantics.md).

### In-memory

`ExpressoFunctions.LTrim(text)` removes leading spaces only, like SQL `LTRIM`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
LTRIM([w].[Name])
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates `TrimStart(new char[0])` with the canonical `LTrim` function. The empty array is an array-creation expression in the tree, which is the form EF6 needs. On SQL Server:

```sql
LTRIM([Extent1].[Name])
```

Every EF6 provider supports `ltrim`.

## Notes

- Available on all supported SQL Server versions (unlike [`trim`](trim.md), which needs SQL Server 2017+).
- See [`rtrim`](rtrim.md) for the right-side equivalent.
