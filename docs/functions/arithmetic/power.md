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

`ExpressoFunctions.Power` follows PostgreSQL `power`. A negative base with a fractional exponent, and zero raised to a negative exponent, throw `NotSupportedException`. A finite input whose result overflows or underflows throws as well. `isnull(power(...))` evaluates the call, so those errors are not reported as false. `power(2,2)` is `4`.

## EF Core rendering

### All providers

On SQL Server an `int` or `byte` base stays in `POWER`, and the result stays an integer until a `double` consumer needs it. `eq(power(age,0.5),2.0)` therefore uses integer arithmetic (`POWER(5,0.5)` is `2`, not about `2.236`) and only then casts:

```sql
CAST(POWER([w].[Age], @__p_0) AS float)
```

A nested call keeps that integer result. `power(power(age,0.5),0.5)` is `POWER(POWER([w].[Age], …), …)`, and `div(power(age,1),2)` divides the integer `POWER` before any float cast. `floor`, `ceiling` and `round` of that integer result stay integer too, so `div(floor(power(age,1)),2)` still truncates. A `double` base stays a floating-point `POWER`. On Oracle a double literal or parameter is cast to `NUMBER`, so `POWER(2, 1024)` raises ORA-01426 instead of returning a `BINARY_DOUBLE` infinity. A `BINARY_DOUBLE` column is left as a binary double. `isnull(power(...))` keeps that call inside `NULLIF`, including on SQLite and MySQL and for a non-nullable base, so a domain result is not folded to false. A literal call stays inside `POWER` as well.

## EF6 rendering

### All providers

On SQL Server an `int` or `byte` base stays in `POWER`, and the integer result is cast to `float` only when a later `double` consumer needs it. Nested `POWER`, integer division and `ABS` therefore see the integer result. Other providers convert both arguments to `float` first. On SQL Server:

```sql
CAST(POWER([Extent1].[Age], @p__linq__0) AS float)
```

`isnull(power(...))` compares that `POWER` result with NULL, so the call is not folded to false.

Every EF6 provider supports `power`.

## Notes

- The return type is always `double`, unlike [`add`](add.md)/[`sub`](sub.md)/[`mult`](mult.md)/[`div`](div.md), which take `argument1`'s type.
- A negative base with a non-integer exponent raises a SQL Server floating-point error (error 3623), the same caveat as [`sqrt`](sqrt.md).
- See [`sqrt`](sqrt.md) for the square-root special case.
