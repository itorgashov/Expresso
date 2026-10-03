# `left`

Returns the leftmost characters of a string, up to the given length.

## Syntax

```text
left(text, length)
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

- Passing any number of arguments other than 2 throws `System.Exception` with the message `"Left() function should have 2 arguments."`
- The first argument is treated as a `string` literal when it is a quoted string token. The second is treated as an `int` literal when it is a literal token.
- A `null` argument throws `ArgumentNullException`.
- A first argument that does not return `string` throws `ArgumentException`.
- A second argument that does not return `int` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, PostgreSQL, MySQL / MariaDB, DB2

```sql
LEFT(text, length)
```

SQL Server example: `eq(left(isbn,3),"978")` renders as `(LEFT([isbn], @wparam_0) = @wparam_1)`.

### SQLite

```sql
substr(text, 1, length)
```

### Oracle

```sql
SUBSTR(text, 1, length)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.Substring(0, length)
```

The result is NULL when `text` or `length` is NULL. Example: `eq(left(isbn,3),"978")` builds `e => e.Isbn != null && e.Isbn.Substring(0, p0) == p1`, where `p0` and `p1` are captured parameters.

### In-memory

`ExpressoFunctions.Left(text, length)` follows PostgreSQL `left`: a `length` past the end returns the whole string, and a negative `length` drops that many characters from the end.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
SUBSTRING([w].[Isbn], 0 + 1, @__Value_0)
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Substring` function.

### SQL Server

`DbFunctions.Left(text, (long?)length)`:

```sql
LEFT([Extent1].[Isbn], @p__linq__0)
```

Every EF6 provider supports `left`.

## Notes

- See [`right`](right.md) for the mirror function, and [`substring`](substring.md) for arbitrary start positions.
