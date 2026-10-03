# `replace`

Replaces all occurrences of a substring within a string.

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

- **Parser arity check:** argument count `!= 3` → `System.Exception`: `"Replace() function should have 3 arguments."`
- **Parser coercion:** all three arguments are coerced to `string` if they are quoted string tokens.
- **IR construction** (`ReplaceFunc`):
  - Any argument is `null` → `ArgumentNullException`
  - Any argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
REPLACE(text, oldValue, newValue)
```

Example: `eq(replace(isbn,"-",""),"9780000000000")` renders as `(REPLACE([isbn], @wparam_0, @wparam_1) = @wparam_2)` on SQL Server. Replaces every occurrence.

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

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Replace` function. On SQL Server:

```sql
REPLACE([Extent1].[Isbn], @p__linq__0, @p__linq__1)
```

Every EF6 provider supports `replace`.

## Notes

- Replaces **every** occurrence of `oldValue`, matching SQL Server's `REPLACE` semantics.
