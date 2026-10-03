# `div`

Divides `argument1` by `argument2` (`argument1 / argument2`).

## Syntax

```text
div(argument1, argument2)
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

- If you pass a number of arguments other than 2, the parser throws `System.Exception` with the message `"Div() function should have 2 arguments."`.
- The parser infers the type of each literal argument independently.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not `byte`, `int`, or `double` throws `ArgumentException("Illegal argument type", "argument1")` or `ArgumentException("Illegal argument type", "argument2")`, depending on which argument is wrong.
- When the parser builds the call, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(argument1 / argument2)
```

SQL Server example: `gt(div(revenue,unitsSold),10)` renders as `(([revenue] / [unitsSold]) > @wparam_0)`. Integral operands follow the engine's integer-division rules.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
argument1 / argument2
```

A `byte` operand is widened to `int`, and when either operand is `double` both become `double`. When both operands are then `int`, this is C# integer division, which truncates toward zero (`div(-7,2)` is `-3`). The result is NULL when either argument is NULL. Example: `eq(div(age,2),15)` builds `e => e.Age / p0 == p1`, where `age` is a non-nullable `int` column and `p0` and `p1` are captured parameters; it matches ages 30 and 31.

The integer `/` in SQL depends on the engine, while the lambda always means truncating division. See [docs/semantics.md](../../semantics.md).

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
[w].[Age] / @__Value_0
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server:

```sql
([Extent1].[Age] / @p__linq__0)
```

Every EF6 provider supports `div`.

## Notes

- SQL Server performs integer division when both operands are integral (`byte` or `int`). For example, `div(sub(price,1), 2)` truncates, just like plain SQL `/`. To get a fractional result, cast a field to a `double` type or use a `double` literal.
- The return type comes from `argument1` only.
- See [`add`](add.md), [`sub`](sub.md), [`mult`](mult.md) for the other arithmetic operators, and [`mod`](mod.md) for the remainder of an integer division.
- See also [`round`](round.md), [`floor`](floor.md), [`ceiling`](ceiling.md), [`sign`](sign.md), [`power`](power.md), [`sqrt`](sqrt.md), [`min`](min.md), [`max`](max.md) for the wider numeric function set.
