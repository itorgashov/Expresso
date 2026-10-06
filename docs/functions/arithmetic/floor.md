# `floor`

Rounds a numeric argument down to the nearest integer (towards negative infinity).

## Syntax

```text
floor(argument)
```

Exactly 1 argument.

- **Category:** Arithmetic
- **Return type:** `double` (always, not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- If you pass a number of arguments other than 1, the parser throws `System.Exception` with the message `"Floor() function should have 1 argument."`.
- The parser infers the type of a literal argument.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not `byte`, `int`, or `double` throws `ArgumentException("Illegal argument type", "argument")`.
- When the parser builds the call, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
FLOOR(argument)
```

SQL Server example: `eq(floor(price),19)` renders as `(FLOOR([price]) = @wparam_0)`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
Math.Floor((double)argument)
```

A `byte` or `int` argument is converted to `double` first. The result is NULL when `argument` is NULL. Example: `eq(floor(amount),50.0)` builds `e => Math.Floor(e.Amount) == p0`, where `amount` is a non-nullable `double` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. A `double` argument stays `FLOOR` of that value. On SQL Server an `int` argument stays `int`, so `div(floor(power(age,1)),2)` divides the integer `FLOOR` before any float cast:

```sql
FLOOR(POWER([w].[Age], @exponent)) / @divisor
```

A `double` column is unchanged:

```sql
FLOOR([w].[Amount])
```

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Floor` function. On SQL Server an `int` argument uses store `FLOOR` and the result stays `int`, including inside a later division. A `double` column is unchanged:

```sql
FLOOR([Extent1].[Amount])
```

Every EF6 provider supports `floor`.

## Notes

- Unlike [`abs`](abs.md), which keeps the argument's type, the public result of `floor` is `double`. On SQL Server the store type stays `int` until a `double` consumer, so a later division still truncates.
- See [`ceiling`](ceiling.md) for rounding up, and [`round`](round.md) for rounding to a given number of digits.
