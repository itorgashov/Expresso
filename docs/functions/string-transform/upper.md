# `upper`

Converts a string to uppercase.

## Syntax

```text
upper(text)
```

Exactly 1 argument.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Upper() function should have 1 argument."`
- **Parser coercion:** argument coerced to `string` if a quoted string token.
- **IR construction** (`UpperFunc`, via base `StringSingleArgFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
UPPER(text)
```

Example: `eq(upper(code),"ABC")` renders as `(UPPER([code]) = @wparam_0)` on SQL Server.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.ToUpper()
```

The result is NULL when `text` is NULL. Example: `eq(upper(code),"ABC")` builds `e => e.Code != null && e.Code.ToUpper() == p0`, where `p0` is a captured parameter. Case mapping of non-ASCII characters is engine-defined: see [docs/semantics.md](../../semantics.md).

### In-memory

`text.ToUpperInvariant()` uses the invariant culture, so the result does not depend on the current culture.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
UPPER([w].[Code])
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `ToUpper` function. On SQL Server:

```sql
UPPER([Extent1].[Code])
```

Every EF6 provider supports `upper`.

## Notes

- See [`lower`](lower.md) for the inverse transform.
