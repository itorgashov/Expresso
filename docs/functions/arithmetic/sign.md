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

## SQL Server rendering

```sql
SIGN(argument)
```

Example: `eq(sign(balance),-1)` renders as `(SIGN([balance]) = @wparam_0)`.

## Notes

- Matches C#'s `Math.Sign`: return type is `int` with values `-1`/`0`/`1`, regardless of the argument's original type.
- See [`abs`](abs.md) for the related unary numeric function.
