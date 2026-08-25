# `sqrt`

Square root of a numeric argument.

## Syntax

```text
sqrt(argument)
```

Exactly 1 argument.

- **Category:** Arithmetic
- **Return type:** `double` (always — not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Sqrt() function should have 1 argument."`
- **Parser coercion:** a literal argument's type is inferred (`GetLiteralType`).
- **IR construction** (`SqrtFunc`, built via reflection): base `NumericSingleArgDoubleFunction`/`NumericSingleArgFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", nameof(argument))` if the `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
SQRT(argument)
```

Example: `lte(sqrt(area),10)` renders as `(SQRT([area]) <= @wparam_0)`.

## Notes

- **Negative values are not rejected at parse/build time** — the negativity of a field or literal generally can't be known until query execution. At execution, SQL Server's `SQRT` raises error 3623 (`"An invalid floating point operation occurred."`) for a negative input; it does **not** return `NULL`. Guard against this with a filter on the underlying column if negative values are possible.
- Return type is always `double`, regardless of the argument's original type.
- See [`power`](power.md) for raising to an arbitrary exponent.
