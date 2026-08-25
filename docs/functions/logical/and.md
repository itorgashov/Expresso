# `and`

Logical AND. True only if **every** argument evaluates to true.

## Syntax

```text
and(expr1, expr2, ...)
```

At least 2 arguments; no upper bound.

- **Category:** Logical
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1..N | `arguments` | `bool` (each) |

## Validation & exceptions

- **Parser arity check:** fewer than 2 arguments → `System.Exception`: `"And() function should at least 2 arguments."` (see the note on plain-`Exception` arity errors in [docs/error-handling.md](../../error-handling.md)).
- **IR construction** (`AndFunc`):
  - `arguments` is `null` → `ArgumentNullException`
  - Fewer than 2 arguments → `ArgumentException` ("contains less elements than expected: 2")
  - Any argument is `null` → `ArgumentException`
  - Any argument's `ReturnType` is not `bool` → `ArgumentException`

## SQL Server rendering

```sql
(expr1 AND expr2 AND ...)
```

Example: `and(gt(age,25),eq(status,1))` renders as `([age] > @wparam_0 AND [status] = @wparam_1)`.

## Notes

- Function name is case-insensitive (`and`, `AND`, `And`).
- See [`or`](or.md) and [`not`](not.md) for the other logical functions.
