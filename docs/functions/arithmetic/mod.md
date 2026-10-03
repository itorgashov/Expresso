# `mod`

Returns the remainder of dividing `argument1` by `argument2` (`argument1 % argument2`).

## Syntax

```text
mod(argument1, argument2)
```

Exactly 2 arguments.

- **Category:** Arithmetic
- **Return type:** same as `argument1`'s type (`byte`, `int`, or `double`)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument1` | `byte`, `int`, or `double` |
| 2 | `argument2` | `byte`, `int`, or `double` (mixed numeric types allowed) |

## Validation & exceptions

- If you pass a number of arguments other than 2, the parser throws `System.Exception` with the message `"Mod() function should have 2 arguments."`.
- The parser infers the type of each literal argument independently.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not `byte`, `int`, or `double` throws `ArgumentException("Illegal argument type", "argument1")` or `ArgumentException("Illegal argument type", "argument2")`, depending on which argument is wrong.
- When the parser builds the call, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, PostgreSQL, SQLite, MySQL / MariaDB

```sql
(argument1 % argument2)
```

SQL Server example: `eq(mod(status,2),0)` renders as `(([status] % @wparam_0) = @wparam_1)`.

### Oracle, DB2

```sql
MOD(argument1, argument2)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
argument1 % argument2
```

A `byte` operand is widened to `int`, and when either operand is `double` both become `double`. The C# `%` result takes the sign of `argument1`, as SQL Server's `%` and PostgreSQL's `%` do. The result is NULL when either argument is NULL. Example: `eq(mod(age,10),0)` builds `e => e.Age % p0 == p1`, where `age` is a non-nullable `int` column and `p0` and `p1` are captured parameters.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
[w].[Age] % @__Value_0
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server:

```sql
([Extent1].[Age] % @p__linq__0)
```

Every EF6 provider supports `mod`.

## Notes

- SQL Server's `%` follows the sign of the dividend, matching C#'s `%` operator. For example, `mod(-7,3)` is `-1`, not `2`.
- The return type comes from `argument1` only.
- If `argument2` is `0`, SQL Server raises a divide-by-zero error when the query runs, the same as [`div`](div.md).
- See [`div`](div.md) for integer/float division, and [`floor`](floor.md)/[`round`](round.md) for other numeric shaping functions.
