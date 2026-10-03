# `mod`

Remainder of dividing `argument1` by `argument2` (`argument1 % argument2`).

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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Mod() function should have 2 arguments."`
- **Parser coercion:** each literal argument's type is inferred independently.
- **IR construction** (`ModFunc`, built via reflection): `ArgumentNullException` for a `null` argument; `ArgumentException("Illegal argument type", "argument1"|"argument2")` for a non-numeric `ReturnType`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, PostgreSQL, SQLite, MySQL / MariaDB

```sql
(argument1 % argument2)
```

Example: `eq(mod(status,2),0)` renders as `(([status] % @wparam_0) = @wparam_1)` on SQL Server.

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

- SQL Server's `%` follows the sign of the dividend, matching C#'s `%` operator (e.g. `mod(-7,3)` is `-1`, not `2`).
- `ReturnType` is copied from `argument1` only.
- `argument2 = 0` raises a SQL Server divide-by-zero error at query execution time, same as [`div`](div.md).
- See [`div`](div.md) for integer/float division, and [`floor`](floor.md)/[`round`](round.md) for other numeric shaping functions.
