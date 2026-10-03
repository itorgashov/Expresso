# `min`

The smaller of two numeric arguments.

## Syntax

```text
min(argument1, argument2)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Min() function should have 2 arguments."`
- **Parser coercion:** each literal argument's type is inferred independently.
- **IR construction** (`MinFunc`, built via reflection): base `NumericArithFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", "argument1"|"argument2")` if either `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(CASE WHEN argument1 < argument2 THEN argument1 ELSE argument2 END)
```

Example: `eq(min(price,cap),cap)` renders as `((CASE WHEN [price] < [cap] THEN [price] ELSE [cap] END) = [cap])` on SQL Server. Portable `CASE` is used instead of `LEAST` so older engines (including SQL Server before 2022) work. A parameterized literal is bound once per `CASE` occurrence.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
argument1 < argument2 ? argument1 : argument2
```

Operands are promoted as in [`add`](add.md). As in the SQL `CASE`, the comparison is TRUE only when both arguments are non-NULL, so a NULL `argument1` returns `argument2`, and the result is NULL exactly when `argument2` is NULL. Example: `eq(min(age,18),0)` builds `e => (e.Age < p0 ? e.Age : p0) == p1`, where `age` is a non-nullable `int` column and `p0` and `p1` are captured parameters; `p0` is used twice.

### In-memory

Same as Queryable. The in-memory `ExpressoFunctions.Min` override applies only to the [collection form](../collection/min.md).

## EF Core rendering

### All providers

EF Core translates the Queryable lambda and binds the literal once. On SQL Server:

```sql
CASE WHEN [w].[Age] < @__Value_0 THEN [w].[Age] ELSE @__Value_0 END
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda and binds the literal once per occurrence. On SQL Server:

```sql
CASE WHEN ([Extent1].[Age] < @p__linq__0) THEN [Extent1].[Age] ELSE @p__linq__1 END
```

Every EF6 provider supports `min`.

## Notes

- `ReturnType` is copied from `argument1` only.
- See [`max`](max.md) for the counterpart.
- Collection overload: when the first argument is a collection name, `min` is [`CollectionMinFunc`](../collection/min.md), not this scalar form.
