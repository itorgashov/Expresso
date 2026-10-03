# `trim`

Trims leading and trailing whitespace from a string.

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

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Trim() function should have 1 argument."`
- **Parser coercion:** argument coerced to `string` if a quoted string token.
- **IR construction** (`TrimFunc`, via base `StringSingleArgFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
TRIM(text)
```

Example: `eq(trim(name),"Ada")` renders as `(TRIM([name]) = @wparam_0)` on SQL Server.

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

- **Requires SQL Server 2017 or later** (`TRIM` as a built-in function). For older SQL Server versions, use [`ltrim`](ltrim.md) and [`rtrim`](rtrim.md) together instead, which have been available since early versions.
