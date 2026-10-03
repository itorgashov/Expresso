# `len`

Length of a string, in characters.

## Syntax

```text
len(text)
```

Exactly 1 argument.

- **Category:** String inspection
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Len() function should have 1 argument."`
- **Parser coercion:** argument coerced to `string` if a quoted string token.
- **IR construction** (`LenFunc`):
  - Argument is `null` → `ArgumentNullException`
  - Argument's `ReturnType` is not `string` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server

```sql
LEN(text)
```

Example: `gt(len(title),50)` renders as `(LEN([title]) > @wparam_0)`.
SQL Server `LEN` does **not** count trailing spaces (`LEN('abc  ')` is `3`). `LEN(NULL)` is `NULL`.

### MySQL / MariaDB

```sql
CHAR_LENGTH(text)
```

### PostgreSQL, SQLite, Oracle, DB2

```sql
LENGTH(text)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.Length
```

The result is NULL when `text` is NULL. Example: `gt(len(title),50)` builds `e => e.Title != null && e.Title.Length > p0`, where `p0` is a captured parameter. Trailing spaces are engine-defined, since SQL Server `LEN` ignores them: see [docs/semantics.md](../../semantics.md).

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
CAST(LEN([w].[Title]) AS int)
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Length` function. On SQL Server:

```sql
CAST(LEN([Extent1].[Title]) AS int)
```

Every EF6 provider supports `len`.

## Notes

- Trailing-space and `NULL` behavior follow the engine (`LEN` vs `LENGTH` / `CHAR_LENGTH`). Expresso does not expose a byte-length function.
- See [`indexof`](indexof.md) for the other `int`-returning string function.
