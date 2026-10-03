# `lower`

Converts a string to lowercase.

## Syntax

```text
lower(text)
```

Exactly 1 argument.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Lower() function should have 1 argument."`
- **Parser coercion:** argument coerced to `string` if a quoted string token.
- **IR construction** (`LowerFunc`, via base `StringSingleArgFunction`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
LOWER(text)
```

Example: `eq(lower(email),"a@b.com")` renders as `(LOWER([email]) = @wparam_0)` on SQL Server.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.ToLower()
```

The result is NULL when `text` is NULL. Example: `eq(lower(email),"a@b.com")` builds `e => e.Email != null && e.Email.ToLower() == p0`, where `p0` is a captured parameter. Case mapping of non-ASCII characters is engine-defined: see [docs/semantics.md](../../semantics.md).

### In-memory

`text.ToLowerInvariant()` uses the invariant culture, so the result does not depend on the current culture.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
LOWER([w].[Email])
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `ToLower` function. On SQL Server:

```sql
LOWER([Extent1].[Email])
```

Every EF6 provider supports `lower`.

## Notes

- See [`upper`](upper.md) for the inverse transform.
