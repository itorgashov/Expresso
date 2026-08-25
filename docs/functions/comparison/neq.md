# `neq`

Inequality comparison.

## Syntax

```text
neq(left, right)
```

Exactly 2 arguments.

- **Category:** Comparison
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `leftOperand` | `byte`, `int`, `double`, `DateTime`, `bool`, or `string` |
| 2 | `rightOperand` | Same set; see compatibility rule below |

## Type compatibility rule

Same as [`eq`](eq.md): both `bool`, both `string`, both `DateTime`, or both numeric (mixed `byte`/`int`/`double` allowed). Anything else → `ArgumentException("Incompatible argument types")`.

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Neq() function should have 2 arguments."`
- **Parser coercion:** same literal-type inference as `eq` — first argument's type drives coercion of the second.
- **IR construction** (`NeqFunc`, built via reflection): `ArgumentNullException` / `ArgumentException` as above; surfaces wrapped in `TargetInvocationException` — see [docs/error-handling.md](../../error-handling.md).

## SQL Server rendering

```sql
(left != right)
```

Example: `neq(status,1)` renders as `([status] != @wparam_0)`.

## Notes

- Renders as SQL `!=`, **not** `<>`.
- See [`eq`](eq.md) for the positive form.
