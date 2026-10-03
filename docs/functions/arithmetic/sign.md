# `sign`

Returns the sign of a numeric argument: `-1` if negative, `0` if zero, `1` if positive.

## Syntax

```text
sign(argument)
```

Exactly 1 argument.

- **Category:** Arithmetic
- **Return type:** `int` (always, not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- If you pass a number of arguments other than 1, the parser throws `System.Exception` with the message `"Sign() function should have 1 argument."`.
- The parser infers the type of a literal argument.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not `byte`, `int`, or `double` throws `ArgumentException("Illegal argument type", "argument")`.
- When the parser builds the call, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
SIGN(argument)
```

SQL Server example: `eq(sign(balance),-1)` renders as `(SIGN([balance]) = @wparam_0)`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
Math.Sign(argument)
```

A `byte` argument is widened to `int` first. The result is NULL when `argument` is NULL. Example: `eq(sign(amount),-1)` builds `e => Math.Sign(e.Amount) == p0`, where `amount` is a non-nullable `double` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
SIGN([w].[Amount])
```

No provider overrides.

## EF6 rendering

### All providers

EF6 has no canonical `SIGN`, so every provider gets a conditional instead of `Math.Sign`:

```csharp
argument > 0 ? 1 : argument < 0 ? -1 : 0
```

On SQL Server:

```sql
CASE WHEN ([Extent1].[Amount] > cast(0 as float(53))) THEN 1
     WHEN ([Extent1].[Amount] < cast(0 as float(53))) THEN -1 ELSE 0 END
```

Every EF6 provider supports `sign`.

## Notes

- `sign` matches C#'s `Math.Sign`: the return type is `int` with values `-1`, `0`, or `1`, regardless of the argument's original type.
- See [`abs`](abs.md) for the related unary numeric function.
