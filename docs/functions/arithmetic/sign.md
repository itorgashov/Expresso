# `sign`

Sign of a numeric argument: `-1` if negative, `0` if zero, `1` if positive.

## Syntax

```text
sign(argument)
```

Exactly 1 argument.

- **Category:** Arithmetic
- **Return type:** `int` (always — not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Sign() function should have 1 argument."`
- **Parser coercion:** a literal argument's type is inferred (`GetLiteralType`).
- **IR construction** (`SignFunc`, built via reflection): base `NumericSingleArgIntResultFunction`/`NumericSingleArgFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", nameof(argument))` if the `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
SIGN(argument)
```

Example: `eq(sign(balance),-1)` renders as `(SIGN([balance]) = @wparam_0)` on SQL Server.

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

- Matches C#'s `Math.Sign`: return type is `int` with values `-1`/`0`/`1`, regardless of the argument's original type.
- See [`abs`](abs.md) for the related unary numeric function.
