# `upper`

Converts a string to upper case.

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

- Passing any number of arguments other than 1 throws `System.Exception` with the message `"Upper() function should have 1 argument."`
- A quoted string token is treated as a `string` literal.
- A `null` argument throws `ArgumentNullException`.
- An argument that does not return `string` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
UPPER(text)
```

SQL Server example: `eq(upper(code),"ABC")` renders as `(UPPER([code]) = @wparam_0)`.

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

A literal argument stays in `UPPER`, so the engine's case mapping applies. `eq(upper("ä"),"ä")` keeps `UPPER`.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `ToUpper` function. On SQL Server:

```sql
UPPER([Extent1].[Code])
```

Every EF6 provider supports `upper`.

## Notes

- See [`lower`](lower.md) for the inverse transform.
