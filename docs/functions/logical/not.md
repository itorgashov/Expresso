# `not`

Logical negation of a single boolean argument.

## Syntax

```text
not(expr)
```

Exactly 1 argument.

- **Category:** Logical
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `bool` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Not() function should have 1."` (see [docs/error-handling.md](../../error-handling.md)).
- **IR construction** (`NotFunc`):
  - `argument` is `null` → `ArgumentNullException`
  - `argument.ReturnType` is not `bool` → `ArgumentException`

## SQL Server rendering

```sql
NOT (expr)
```

Example: `not(eq(status,1))` renders as `NOT ([status] = @wparam_0)`.

## Notes

- Function name is case-insensitive.
- See [`and`](and.md) and [`or`](or.md) for the other logical functions.
