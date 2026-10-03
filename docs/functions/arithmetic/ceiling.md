# `ceiling`

Rounds a numeric argument up to the nearest integer (towards positive infinity).

## Syntax

```text
ceiling(argument)
```

Exactly 1 argument. Alias: `ceil` (identical behavior, same arity).

- **Category:** Arithmetic
- **Return type:** `double` (always, not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- If you pass a number of arguments other than 1, the parser throws `System.Exception` with the message `"Ceiling() function should have 1 argument."`. This applies to both `ceiling` and the `ceil` alias.
- The parser infers the type of a literal argument.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not `byte`, `int`, or `double` throws `ArgumentException("Illegal argument type", "argument")`.
- When the parser builds the call, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### SQL Server, PostgreSQL, MySQL / MariaDB, DB2

```sql
CEILING(argument)
```

SQL Server example: `eq(ceiling(price),20)` renders as `(CEILING([price]) = @wparam_0)`.

### SQLite, Oracle

```sql
CEIL(argument)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
Math.Ceiling((double)argument)
```

A `byte` or `int` argument is converted to `double` first. The result is NULL when `argument` is NULL. The `ceil` alias builds the same lambda. Example: `eq(ceiling(amount),51.0)` builds `e => Math.Ceiling(e.Amount) == p0`, where `amount` is a non-nullable `double` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
CEILING([w].[Amount])
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Ceiling` function. On SQL Server:

```sql
CEILING([Extent1].[Amount])
```

Every EF6 provider supports `ceiling`.

## Notes

- The return type is always `double`, regardless of the argument's original type.
- See [`floor`](floor.md) for rounding down, and [`round`](round.md) for rounding to a given number of digits.
