# `power`

Raises `argument1` to the power of `argument2` (`argument1 ^ argument2`).

## Syntax

```text
power(argument1, argument2)
```

Exactly 2 arguments. Alias: `pow` (identical behavior, same arity).

- **Category:** Arithmetic
- **Return type:** `double` (always, not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument1` | `byte`, `int`, or `double` (base) |
| 2 | `argument2` | `byte`, `int`, or `double` (exponent; mixed numeric types allowed) |

## Validation & exceptions

- If you pass a number of arguments other than 2, the parser throws `System.Exception` with the message `"Power() function should have 2 arguments."`. This applies to both `power` and the `pow` alias.
- The parser infers the type of each literal argument independently.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not `byte`, `int`, or `double` throws `ArgumentException("Illegal argument type", "argument1")` or `ArgumentException("Illegal argument type", "argument2")`, depending on which argument is wrong.
- When the parser builds the call, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
POWER(argument1, argument2)
```

SQL Server example: `eq(power(base,2),25)` renders as `(POWER([base], @wparam_0) = @wparam_1)`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
Math.Pow((double)argument1, (double)argument2)
```

Both arguments are converted to `double` first. The result is NULL when either argument is NULL. The `pow` alias builds the same lambda. Example: `eq(power(age,1),18)` builds `e => Math.Pow((double)e.Age, (double)p0) == (double)p1`, where `age` is a non-nullable `int` column and `p0` and `p1` are captured parameters.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

On SQL Server:

```sql
POWER(CAST([w].[Age] AS float), @__p_0)
```

The call is `POWER` on every provider, including a column base. `isnull(power(...))` keeps that call inside `NULLIF`, including on SQLite and MySQL and for a non-nullable base, so a domain result is not folded to false. A literal call stays inside `POWER` as well.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Power` function. On SQL Server:

```sql
POWER(CAST([Extent1].[Age] AS float), @p__linq__0)
```

`isnull(power(...))` compares that `POWER` result with NULL, so the call is not folded to false.

Every EF6 provider supports `power`.

## Notes

- The return type is always `double`, unlike [`add`](add.md)/[`sub`](sub.md)/[`mult`](mult.md)/[`div`](div.md), which take `argument1`'s type.
- A negative base with a non-integer exponent raises a SQL Server floating-point error (error 3623), the same caveat as [`sqrt`](sqrt.md).
- See [`sqrt`](sqrt.md) for the square-root special case.
