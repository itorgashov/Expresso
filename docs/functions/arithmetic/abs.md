# `abs`

Absolute value of a numeric argument.

## Syntax

```text
abs(argument)
```

Exactly 1 argument.

- **Category:** Arithmetic
- **Return type:** same as the argument's type (`byte`, `int`, or `double` — **not** widened)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Abs() function should have 1 argument."`
- **Parser coercion:** a literal argument's type is inferred (`GetLiteralType`).
- **IR construction** (`AbsFunc`, built via reflection): base `NumericSingleArgFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", nameof(argument))` if the `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
ABS(argument)
```

Example: `eq(abs(balance),100)` renders as `(ABS([balance]) = @wparam_0)` on SQL Server.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
Math.Abs(argument)
```

A `byte` argument is widened to `int` first. The result is NULL when `argument` is NULL. Example: `eq(abs(amount),12.7)` builds `e => Math.Abs(e.Amount) == p0`, where `amount` is a non-nullable `double` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
ABS([w].[Amount])
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Abs` function. On SQL Server:

```sql
ABS([Extent1].[Amount])
```

Every EF6 provider supports `abs`.

## Notes

- `ReturnType` is copied from the argument, not promoted — `abs` of a `byte` field is still typed `byte`.
- See [`add`](add.md), [`sub`](sub.md), [`mult`](mult.md), [`div`](div.md) for binary arithmetic, and [`sign`](sign.md) for the related unary numeric function.
