# `substring`

Returns a substring that starts at a given 1-based position and has a given length.

## Syntax

```text
substring(text, start, length)
```

Exactly 3 arguments. Alias: `substr` (identical behavior, same arity).

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `sourceString` | `string` |
| 2 | `startIndex` | `int` |
| 3 | `length` | `int` |

## Validation & exceptions

- Passing any number of arguments other than 3 throws `System.Exception` with the message `"Substring() function should have 3 arguments."` This applies to both `substring` and the `substr` alias.
- The first argument is treated as a `string` literal when it is a quoted string token. The second and third are treated as `int` literals (not `byte` or `double`) when they are literal tokens.
- A `null` argument throws `ArgumentNullException`.
- A first argument that does not return `string` throws `ArgumentException`.
- A start index or length that does not return `int` throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`start` is 1-based on every dialect, as in SQL. Expresso passes it through without converting it to 0-based.

### SQL Server, MySQL / MariaDB

```sql
SUBSTRING(text, start, length)
```

SQL Server example: `eq(substring(name,1,3),"Mar")` renders as `(SUBSTRING([name], @wparam_0, @wparam_1) = @wparam_2)`.

### PostgreSQL, Oracle, DB2

```sql
SUBSTR(text, start, length)
```

### SQLite

```sql
substr(text, start, length)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
text.Substring(start - 1, length)
```

The 1-based `start` becomes the 0-based .NET index by subtracting 1. The result is NULL when any argument is NULL. Example: `eq(substring(name,1,3),"Mar")` builds `e => e.Name != null && e.Name.Substring(p0 - 1, p1) == p2`, where `p0`, `p1` and `p2` are captured parameters.

### In-memory

`ExpressoFunctions.Substring(text, start, length)` follows PostgreSQL `SUBSTRING(text FROM start FOR length)`: `start` stays 1-based and positions outside the string are dropped, so `substring("Alice",4,10)` is `"ce"`. A negative `length` throws `ArgumentException`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. When `start` is a parameter, EF Core computes `start - 1` on the client as `@__p_0` and adds 1 back. On SQL Server:

```sql
SUBSTRING([w].[Name], @__p_0 + 1, @__Value_1)
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Substring` function. On SQL Server:

```sql
SUBSTRING([Extent1].[Name], (@p__linq__0 - 1) + 1, @p__linq__1)
```

Every EF6 provider supports `substring`.

## Notes

- `start` follows SQL Server's native 1-based `SUBSTRING` convention. Expresso passes it through unchanged and does not convert it to 0-based, so `substring(name,1,2)` returns the first two characters, as in plain T-SQL. [`indexof`](../string-inspect/indexof.md), by contrast, is 0-based.
- See [`left`](left.md) and [`right`](right.md) for fixed-anchor substrings.
