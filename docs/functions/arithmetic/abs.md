# `abs`

Returns the absolute value of a numeric argument.

## Syntax

```text
abs(argument)
```

Exactly 1 argument.

- **Category:** Arithmetic
- **Return type:** same as the argument's type (`byte`, `int`, or `double`); **not** widened

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- If you pass a number of arguments other than 1, the parser throws `System.Exception` with the message `"Abs() function should have 1 argument."`.
- The parser infers the type of a literal argument.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not `byte`, `int`, or `double` throws `ArgumentException("Illegal argument type", "argument")`.
- When the parser builds the call, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
ABS(argument)
```

SQL Server example: `eq(abs(balance),100)` renders as `(ABS([balance]) = @wparam_0)`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
Math.Abs(argument)
```

A `byte` argument is widened to `int` first. The result is NULL when `argument` is NULL. Example: `eq(abs(amount),12.7)` builds `e => Math.Abs(e.Amount) == p0`, where `amount` is a non-nullable `double` column and `p0` is a captured parameter.

### In-memory

`Math.Abs` for `double`. For `int`, `ExpressoFunctions.Abs` throws `NotSupportedException` (`integer out of range`) when the argument is the minimum `int`, including when `isnull` consumes the call.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
ABS([w].[Amount])
```

A literal argument stays in `ABS`, including the minimum `int`, so command generation does not overflow in the CLR. `isnull(abs(...))` of an `int` keeps `ABS` inside the null check, so an overflow is raised by the engine instead of the predicate becoming false. SQLite evaluates that `ABS` and does not overflow.

## EF6 rendering

### All providers

An `int` argument uses the canonical `Abs` function, including a literal minimum `int`. `isnull` of that call compares `ABS` with NULL. On SQL Server:

```sql
ABS([Extent1].[Amount])
```

Every EF6 provider supports `abs`.

## Notes

- The return type is copied from the argument, not promoted. The `abs` of a `byte` field is still typed `byte`.
- See [`add`](add.md), [`sub`](sub.md), [`mult`](mult.md), [`div`](div.md) for binary arithmetic, and [`sign`](sign.md) for the related unary numeric function.
