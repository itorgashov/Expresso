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

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(expr1 AND expr2 AND ...)
```

Example: `and(gt(age,25),eq(status,1))` renders as `([age] > @wparam_0 AND [status] = @wparam_1)` on SQL Server.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
WhenTrue(expr1) && WhenTrue(expr2) && ...     // TRUE
WhenFalse(expr1) || WhenFalse(expr2) || ...   // FALSE
```

Every boolean argument is rendered as a TRUE condition and a FALSE condition (SQL three-valued logic); when neither holds, the argument is unknown. `and` is TRUE when every argument is TRUE and FALSE when any argument is FALSE, so an unknown argument makes the result unknown unless another argument is FALSE, exactly like SQL `AND`. The FALSE condition is what `not(and(...))` uses. Example: `and(eq(age,30),eq(name,"Alice"))` builds `e => e.Age == p0 && (e.Name != null && e.Name == p1)`, where `p0` and `p1` are captured parameters.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
[w].[Age] = @__Value_0 AND [w].[Name] = @__Value_1
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda.

Every EF6 provider supports `and`.

## Notes

- Function name is case-insensitive (`and`, `AND`, `And`).
- See [`or`](or.md) and [`not`](not.md) for the other logical functions.
