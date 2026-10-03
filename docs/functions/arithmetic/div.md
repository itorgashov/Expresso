# `div`

Division of two numeric arguments (`argument1 / argument2`).

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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Div() function should have 2 arguments."`
- **Parser coercion:** each literal argument's type is inferred independently.
- **IR construction** (`DivFunc`, built via reflection): `ArgumentNullException` for a `null` argument; `ArgumentException("Illegal argument type", "argument1"|"argument2")` for a non-numeric `ReturnType`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(argument1 / argument2)
```

Example: `gt(div(revenue,unitsSold),10)` renders as `(([revenue] / [unitsSold]) > @wparam_0)` on SQL Server. Integral operands follow the engine's integer-division rules.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
argument1 / argument2
```

A `byte` operand is widened to `int`, and when either operand is `double` both become `double`. When both operands are then `int`, this is C# integer division, which truncates toward zero (`div(-7,2)` is `-3`). The result is NULL when either argument is NULL. Example: `eq(div(age,2),15)` builds `e => e.Age / p0 == p1`, where `age` is a non-nullable `int` column and `p0` and `p1` are captured parameters; it matches ages 30 and 31.

The SQL renderers' integer `/` is engine-defined, while the lambda always means truncating division; see [docs/semantics.md](../../semantics.md).

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

- SQL Server performs **integer division** when both operands are integral (`byte`/`int`) — `div(sub(price,1), 2)` truncates just like plain SQL `/` would. Cast a field to `double`-typed data or use a `double` literal if you need fractional results.
- `ReturnType` is copied from `argument1` only.
- See [`add`](add.md), [`sub`](sub.md), [`mult`](mult.md) for the other arithmetic operators, and [`mod`](mod.md) for the remainder of an integer division.
- See also [`round`](round.md), [`floor`](floor.md), [`ceiling`](ceiling.md), [`sign`](sign.md), [`power`](power.md), [`sqrt`](sqrt.md), [`min`](min.md), [`max`](max.md) for the wider numeric function set.
