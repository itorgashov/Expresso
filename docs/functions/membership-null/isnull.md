# `isnull`

True if the argument is `NULL`.

## Syntax

```text
isnull(expr)
```

Exactly 1 argument.

- **Category:** Membership / null
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `bool`, `string`, `byte`, `int`, `double`, `DateTime`, `Guid`, `TimeSpan`; plus `DateOnly`/`TimeOnly` on net6.0 |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"IsNull() function should have 1 argument."`
- **Parser coercion:** **none** — unlike most other functions, `isnull`'s argument is *not* passed through literal coercion. In practice this means `isnull` is meant to be used with a field reference (e.g. `isnull(publisher)`), not a raw quoted literal.
- **IR construction** (`IsNullFunc`):
  - `argument` is `null` → `ArgumentNullException`
  - `argument.ReturnType` is not one of the allowed types → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(expr IS NULL)
```

Example: `isnull(isbn)` renders as `([isbn] IS NULL)` on SQL Server.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
expr == null   // TRUE
expr != null   // FALSE
```

`isnull` is never unknown: exactly one of the two conditions holds. For an argument that can never be NULL, TRUE is the constant `false`. A computed argument uses its own NULL rule, so `isnull(concat(name,notes))` builds `e => e.Name == null || e.Notes == null`. Example: `isnull(notes)` builds `e => e.Notes == null`, and `not(isnull(notes))` builds `e => e.Notes != null`.

### In-memory

Same as Queryable. A computed argument follows the in-memory NULL rule, so `isnull(concat(name,notes))` builds `e => false`, because in-memory [`concat`](../string-transform/concat.md) treats NULL as an empty string.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
[w].[Notes] IS NULL
```

No provider overrides for `isnull` itself. On SQL Server, PostgreSQL and Oracle the [`concat`](../string-transform/concat.md) override makes `isnull(concat(name,notes))` constant FALSE.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server:

```sql
[Extent1].[Notes] IS NULL
```

As in EF Core, the `concat` override on SQL Server, PostgreSQL and Oracle makes `isnull(concat(name,notes))` constant FALSE.

Every EF6 provider supports `isnull`, except that Oracle and SQLite cannot use a time-of-day `TimeSpan` argument: the provider has no time-of-day (Edm.Time) type.

## Notes

- Combine with [`not`](../logical/not.md) for "is not null": `not(isnull(isbn))`.
- See [`in`](in.md) for the other membership/null function.
