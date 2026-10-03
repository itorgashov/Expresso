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

EF Core translates the Queryable lambda. On SQL Server:

```sql
FLOOR([w].[Amount])
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Floor` function. On SQL Server:

```sql
FLOOR([Extent1].[Amount])
```

Every EF6 provider supports `floor`.

## Notes

- Unlike [`abs`](abs.md), which keeps the argument's type, `floor` always returns `double`, regardless of the argument's original type.
- See [`ceiling`](ceiling.md) for rounding up, and [`round`](round.md) for rounding to a given number of digits.
