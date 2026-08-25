# `ceiling`

Rounds a numeric argument up to the nearest integer (towards positive infinity).

## Syntax

```text
ceiling(argument)
```

Exactly 1 argument. Alias: `ceil` (identical behavior, same arity).

- **Category:** Arithmetic
- **Return type:** `double` (always — not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Ceiling() function should have 1 argument."` (applies to both `ceiling` and the `ceil` alias).
- **Parser coercion:** a literal argument's type is inferred (`GetLiteralType`).
- **IR construction** (`CeilingFunc`, built via reflection): base `NumericSingleArgDoubleFunction`/`NumericSingleArgFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", nameof(argument))` if the `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
CEILING(argument)
```

Example: `eq(ceiling(price),20)` renders as `(CEILING([price]) = @wparam_0)`.

## Notes

- Return type is always `double`, regardless of the argument's original type.
- See [`floor`](floor.md) for rounding down, and [`round`](round.md) for rounding to a given number of digits.
