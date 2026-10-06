# `right`

Returns the rightmost characters of a string, up to the given length.

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

- Passing any number of arguments other than 2 throws `System.Exception` with the message `"Right() function should have 2 arguments."`
- The first argument is treated as a `string` literal when it is a quoted string token. The second is treated as an `int` literal when it is a literal token.
- A `null` argument throws `ArgumentNullException`.
- A first argument that does not return `string` throws `ArgumentException`.
- A second argument that does not return `int` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, PostgreSQL, MySQL / MariaDB, DB2

```sql
RIGHT(text, length)
```

SQL Server example: `eq(right(isbn,1),"3")` renders as `(RIGHT([isbn], @wparam_0) = @wparam_1)`.

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

### SQL Server and PostgreSQL

`RIGHT(text, length)` for a field or a literal length:

```sql
RIGHT([w].[Isbn], @n)
```

### SQLite

`substr(text, -(length))` for a field or a literal length, including zero and negative lengths:

```sql
substr([w].[Name], -@n)
```

### MySQL / MariaDB and DB2

`RIGHT(text, length)` when neither operand references the query row, for example `right("abc",2)`. A column operand, including `right(name,2)`, uses the Queryable `CASE` / `SUBSTRING` form.

### Oracle

`SUBSTR(text, GREATEST(LENGTH(text) - length + 1, 1))` when neither operand references the query row, for example `right("abc",2)`. A column operand, including `right(name,2)`, uses the Queryable form.

## EF6 rendering

### SQL Server

`DbFunctions.Right(text, (long?)length)`:

```sql
RIGHT([Extent1].[Isbn], @p__linq__0)
```

### PostgreSQL

A `CASE` on `substr` that keeps the length non-negative. A negative length drops characters from the start.

### SQLite

`substr(text, -(length))`, including zero and negative lengths.

### Other providers

The Queryable `Length` and `Substring` form.

Every EF6 provider supports `right`.

## Notes

- See [`left`](left.md) for the mirror function, and [`substring`](substring.md) for arbitrary start positions.
