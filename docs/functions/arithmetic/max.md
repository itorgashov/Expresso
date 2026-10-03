# `max`

Returns the larger of two numeric arguments.

## Syntax

```text
max(argument1, argument2)
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

- If you pass a number of arguments other than 2, the parser throws `System.Exception` with the message `"Max() function should have 2 arguments."`.
- The parser infers the type of each literal argument independently.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not `byte`, `int`, or `double` throws `ArgumentException("Illegal argument type", "argument1")` or `ArgumentException("Illegal argument type", "argument2")`, depending on which argument is wrong.
- When the parser builds the call, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(CASE WHEN argument1 > argument2 THEN argument1 ELSE argument2 END)
```

SQL Server example: `eq(max(price,floorPrice),floorPrice)` renders as `((CASE WHEN [price] > [floorPrice] THEN [price] ELSE [floorPrice] END) = [floorPrice])`. Expresso uses a portable `CASE` instead of `GREATEST`. A parameterized literal is bound once per `CASE` occurrence.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
argument1 > argument2 ? argument1 : argument2
```

Operands are promoted as in [`add`](add.md). As in the SQL `CASE`, the comparison is TRUE only when both arguments are non-NULL, so a NULL `argument1` returns `argument2`, and the result is NULL exactly when `argument2` is NULL. Example: `gt(max(age,20),30)` builds `e => (e.Age > p0 ? e.Age : p0) > p1`, where `age` is a non-nullable `int` column and `p0` and `p1` are captured parameters; `p0` is used twice.

### In-memory

Same as Queryable. The in-memory `ExpressoFunctions.Max` override applies only to the [collection form](../collection/max.md).

## EF Core rendering

### All providers

EF Core translates the Queryable lambda and binds the literal once. On SQL Server:

```sql
CASE WHEN [w].[Age] > @__Value_0 THEN [w].[Age] ELSE @__Value_0 END
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda and binds the literal once per occurrence. On SQL Server:

```sql
CASE WHEN ([Extent1].[Age] > @p__linq__0) THEN [Extent1].[Age] ELSE @p__linq__1 END
```

Every EF6 provider supports `max`.

## Notes

- The return type comes from `argument1` only.
- See [`min`](min.md) for the counterpart.
- If the first argument is a collection name, `max` is the [collection form](../collection/max.md) instead of this scalar form.
