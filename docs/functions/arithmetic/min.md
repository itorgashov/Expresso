# `min`

The smaller of two numeric arguments.

## Syntax

```text
min(argument1, argument2)
```

Exactly 2 arguments.

- **Category:** Arithmetic
- **Return type:** same as `argument1`'s type (`byte`, `int`, or `double`)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument1` | `byte`, `int`, or `double` |
| 2 | `argument2` | `byte`, `int`, or `double` (mixed numeric types allowed) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Min() function should have 2 arguments."`
- **Parser coercion:** each literal argument's type is inferred independently.
- **IR construction** (`MinFunc`, built via reflection): base `NumericArithFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", "argument1"|"argument2")` if either `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
(CASE WHEN argument1 < argument2 THEN argument1 ELSE argument2 END)
```

Example: `eq(min(price,cap),cap)` renders as `((CASE WHEN [price] < [cap] THEN [price] ELSE [cap] END) = [cap])`.

## Notes

- Rendered as a portable `CASE WHEN` rather than SQL Server's `LEAST` (only available from SQL Server 2022 onward), so it works on older SQL Server versions too.
- Each argument is rendered **once per occurrence** in the `CASE` expression — a parameterized literal argument therefore produces one SQL parameter per occurrence (e.g. `min(age,18)` binds `18` to two separate parameters), not a single shared one.
- `ReturnType` is copied from `argument1` only.
- See [`max`](max.md) for the counterpart.
- Collection overload: when the first argument is a collection name, `min` is [`CollectionMinFunc`](../collection/min.md), not this scalar form.
