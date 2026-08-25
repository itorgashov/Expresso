# `round`

Rounds a numeric argument to a given number of decimal digits (0 if omitted).

## Syntax

```text
round(argument)
round(argument, digits)
```

1 or 2 arguments.

- **Category:** Arithmetic
- **Return type:** `double` (always — not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |
| 2 (optional) | `digits` | `int` — zero, positive, and **negative** values are allowed |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1 and != 2` → `System.Exception`: `"Round() function should have 1 or 2 arguments."`
- **Parser coercion:** `argument` coerced via literal-type inference if a literal token; `digits` coerced to `int` if a literal token.
- **IR construction** (`RoundFunc`, constructed directly — **not** via reflection, unlike the other numeric functions on this page):
  - `argument` is `null` → `ArgumentNullException`
  - `argument.ReturnType` is not `byte`/`int`/`double` → `ArgumentException("Illegal argument type", nameof(value))`
  - `digits` is `null` (2-argument overload) → `ArgumentNullException`
  - `digits.ReturnType` is not `int` → `ArgumentException`

  Because `RoundFunc` is constructed directly rather than via `Activator.CreateInstance`, these exceptions are **not** wrapped in `TargetInvocationException` — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
ROUND(argument, 0)       -- 1-argument form
ROUND(argument, digits)  -- 2-argument form
```

Example: `lte(round(price),20)` renders as `(ROUND([price], 0) <= @wparam_0)`.

Example: `eq(round(price,-1),20)` renders as `(ROUND([price], @wparam_0) = @wparam_1)`.

## Notes

- **Zero and negative `digits` are supported** — `round(price,-1)` rounds to the nearest ten, matching SQL Server's native `ROUND` semantics.
- SQL Server's `ROUND` rounds away from zero at the midpoint (`ROUND(2.5,0) = 3`), which differs from .NET's default `Math.Round` (banker's rounding, `MidpointRounding.ToEven`). Expresso does not change this — the SQL Server behavior is what executes.
- Return type is always `double`, regardless of the argument's original type.
- See [`floor`](floor.md)/[`ceiling`](ceiling.md) for rounding to the nearest whole number in a fixed direction.
