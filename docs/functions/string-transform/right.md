# `right`

Returns the rightmost N characters of a string.

## Syntax

```text
right(text, length)
```

Exactly 2 arguments.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `sourceString` | `string` |
| 2 | `length` | `int` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Right() function should have 2 arguments."`
- **Parser coercion:** argument 1 coerced to `string`; argument 2 coerced to `int` if a literal token.
- **IR construction** (`RightFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - `sourceString.ReturnType` is not `string` → `ArgumentException`
  - `length.ReturnType` is not `int` → `ArgumentException`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, PostgreSQL, MySQL / MariaDB, DB2

```sql
RIGHT(text, length)
```

Example: `eq(right(isbn,1),"3")` renders as `(RIGHT([isbn], @wparam_0) = @wparam_1)` on SQL Server.

### SQLite

```sql
substr(text, -(length))
```

### Oracle

```sql
SUBSTR(text, GREATEST(LENGTH(text) - (length) + 1, 1))
```

`text` is rendered twice. Oracle `SUBSTR(text, -(length))` would return `NULL` when `length` exceeds the string length and the whole string when `length` is 0.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.Length <= length ? text : text.Substring(text.Length - length, length)
```

The result is NULL when `text` or `length` is NULL. Example: `eq(right(isbn,1),"3")` builds `e => e.Isbn != null && (e.Isbn.Length <= p0 ? e.Isbn : e.Isbn.Substring(e.Isbn.Length - p0, p0)) == p1`, where `p0` and `p1` are captured parameters.

### In-memory

`ExpressoFunctions.Right(text, length)` follows PostgreSQL `right`: a negative `length` drops that many characters from the start.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
CASE WHEN CAST(LEN([w].[Isbn]) AS int) <= @n THEN [w].[Isbn]
     ELSE SUBSTRING([w].[Isbn], (CAST(LEN([w].[Isbn]) AS int) - @n) + 1, @n) END
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Length` and `Substring` functions.

### SQL Server

`DbFunctions.Right(text, (long?)length)`:

```sql
RIGHT([Extent1].[Isbn], @p__linq__0)
```

Every EF6 provider supports `right`.

## Notes

- See [`left`](left.md) for the mirror function, and [`substring`](substring.md) for arbitrary start positions.
