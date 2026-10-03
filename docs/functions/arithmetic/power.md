# `power`

Raises `argument1` to the power of `argument2` (`argument1 ^ argument2`).

## Syntax

```text
power(argument1, argument2)
```

Exactly 2 arguments. Alias: `pow` (identical behavior, same arity).

- **Category:** Arithmetic
- **Return type:** `double` (always — not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument1` | `byte`, `int`, or `double` (base) |
| 2 | `argument2` | `byte`, `int`, or `double` (exponent; mixed numeric types allowed) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Power() function should have 2 arguments."` (applies to both `power` and the `pow` alias).
- **Parser coercion:** each literal argument's type is inferred independently.
- **IR construction** (`PowerFunc`, built via reflection): base `NumericArithFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", "argument1"|"argument2")` if either `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
POWER(argument1, argument2)
```

Example: `eq(power(base,2),25)` renders as `(POWER([base], @wparam_0) = @wparam_1)` on SQL Server.

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

EF Core translates the Queryable lambda. On SQL Server:

```sql
POWER(CAST([w].[Age] AS float), @__p_0)
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Power` function. On SQL Server:

```sql
POWER(CAST([Extent1].[Age] AS float), @p__linq__0)
```

Every EF6 provider supports `power`.

## Notes

- Return type is always `double`, unlike [`add`](add.md)/[`sub`](sub.md)/[`mult`](mult.md)/[`div`](div.md), which copy `argument1`'s type.
- A negative base with a non-integer exponent raises a SQL Server floating-point error (error 3623), same caveat as [`sqrt`](sqrt.md).
- See [`sqrt`](sqrt.md) for the square-root special case.
