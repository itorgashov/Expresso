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

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(expr1 OR expr2 OR ...)
```

Example: `or(eq(status,1),eq(status,2))` renders as `([status] = @wparam_0 OR [status] = @wparam_1)` on SQL Server.

## Notes

- Function name is case-insensitive.
- See [`and`](and.md) and [`not`](not.md) for the other logical functions.
