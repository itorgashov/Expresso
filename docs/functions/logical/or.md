# `or`

Logical OR. True if **any** argument evaluates to true.

## Syntax

```text
or(expr1, expr2, ...)
```

At least 2 arguments; no upper bound.

- **Category:** Logical
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1..N | `arguments` | `bool` (each) |

## Validation & exceptions

- **Parser arity check:** fewer than 2 arguments → `System.Exception`: `"Or() function should at least 2 arguments."` (see [docs/error-handling.md](../../error-handling.md)).
- **IR construction** (`OrFunc`):
  - `arguments` is `null` → `ArgumentNullException`
  - Fewer than 2 arguments → `ArgumentException`
  - Any argument is `null` → `ArgumentException`
  - Any argument's `ReturnType` is not `bool` → `ArgumentException`

## SQL Server rendering

```sql
(expr1 OR expr2 OR ...)
```

Example: `or(startswith(name,"Jo"),eq(status,1))` renders as `([name] LIKE @wparam_0 ESCAPE '\' OR [status] = @wparam_1)`.

## Notes

- Function name is case-insensitive.
- See [`and`](and.md) and [`not`](not.md) for the other logical functions.
