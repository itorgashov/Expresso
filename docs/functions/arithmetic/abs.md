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

## SQL Server rendering

```sql
ABS(argument)
```

Example: `eq(abs(balance),100)` renders as `(ABS([balance]) = @wparam_0)`.

## Notes

- `ReturnType` is copied from the argument, not promoted — `abs` of a `byte` field is still typed `byte`.
- See [`add`](add.md), [`sub`](sub.md), [`mult`](mult.md), [`div`](div.md) for binary arithmetic.
