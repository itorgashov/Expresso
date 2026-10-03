# `mult`

Multiplies two numeric arguments.

## Syntax

```text
mult(argument1, argument2)
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

- If you pass a number of arguments other than 2, the parser throws `System.Exception` with the message `"Mult() function should have 2 arguments."`.
- The parser infers the type of each literal argument independently.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not `byte`, `int`, or `double` throws `ArgumentException("Illegal argument type", "argument1")` or `ArgumentException("Illegal argument type", "argument2")`, depending on which argument is wrong.
- When the parser builds the call, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(argument1 * argument2)
```

SQL Server example: `gt(mult(price,quantity),1000)` renders as `(([price] * [quantity]) > @wparam_0)`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
argument1 * argument2
```

A `byte` operand is widened to `int`, and when either operand is `double` both become `double`, so the value's type can differ from the declared return type. The result is NULL when either argument is NULL. Example: `eq(mult(age,2),80)` builds `e => e.Age * p0 == p1`, where `age` is a non-nullable `int` column and `p0` and `p1` are captured parameters.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
[w].[Age] * @__Value_0
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server:

```sql
([Extent1].[Age] * @p__linq__0)
```

Every EF6 provider supports `mult`.

## Notes

- The return type comes from `argument1` only.
- See [`add`](add.md), [`sub`](sub.md), [`div`](div.md) for the other arithmetic operators.
- See also [`mod`](mod.md), [`round`](round.md), [`floor`](floor.md), [`ceiling`](ceiling.md), [`sign`](sign.md), [`power`](power.md), [`sqrt`](sqrt.md), [`min`](min.md), [`max`](max.md) for the wider numeric function set.
