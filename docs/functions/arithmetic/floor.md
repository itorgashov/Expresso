# `floor`

Rounds a numeric argument down to the nearest integer (towards negative infinity).

## Syntax

```text
floor(argument)
```

Exactly 1 argument.

- **Category:** Arithmetic
- **Return type:** `double` (always — not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Floor() function should have 1 argument."`
- **Parser coercion:** a literal argument's type is inferred (`GetLiteralType`).
- **IR construction** (`FloorFunc`, built via reflection): base `NumericSingleArgDoubleFunction`/`NumericSingleArgFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", nameof(argument))` if the `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
FLOOR(argument)
```

Example: `eq(floor(price),19)` renders as `(FLOOR([price]) = @wparam_0)`.

## Notes

- Unlike [`abs`](abs.md), the return type is always `double`, regardless of the argument's original type — contrast with `abs`, which copies the argument's type unchanged.
- See [`ceiling`](ceiling.md) for rounding up, and [`round`](round.md) for rounding to a given number of digits.
