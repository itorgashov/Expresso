# `power`

Raises `argument1` to the power of `argument2` (`argument1 ^ argument2`).

## Syntax

```text
power(argument1, argument2)
```

Exactly 2 arguments. Alias: `pow` (identical behavior, same arity).

- **Category:** Arithmetic
- **Return type:** `double` (always — not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument1` | `byte`, `int`, or `double` (base) |
| 2 | `argument2` | `byte`, `int`, or `double` (exponent; mixed numeric types allowed) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Power() function should have 2 arguments."` (applies to both `power` and the `pow` alias).
- **Parser coercion:** each literal argument's type is inferred independently.
- **IR construction** (`PowerFunc`, built via reflection): base `NumericArithFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", "argument1"|"argument2")` if either `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
POWER(argument1, argument2)
```

Example: `eq(power(base,2),25)` renders as `(POWER([base], @wparam_0) = @wparam_1)`.

## Notes

- Return type is always `double`, unlike [`add`](add.md)/[`sub`](sub.md)/[`mult`](mult.md)/[`div`](div.md), which copy `argument1`'s type.
- A negative base with a non-integer exponent raises a SQL Server floating-point error (error 3623), same caveat as [`sqrt`](sqrt.md).
- See [`sqrt`](sqrt.md) for the square-root special case.
