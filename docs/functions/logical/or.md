# `or`

Returns true when at least one argument is true.

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

- If you pass fewer than 2 arguments, the parser throws `System.Exception` with the message `"Or() function should at least 2 arguments."` (see [docs/error-handling.md](../../error-handling.md)).
- If the argument list itself is `null`, the call throws `ArgumentNullException`.
- If the list has fewer than 2 arguments, the call throws `ArgumentException`.
- If any argument is `null`, or any argument does not return `bool`, the call throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(expr1 OR expr2 OR ...)
```

SQL Server example: `or(eq(status,1),eq(status,2))` renders as `([status] = @wparam_0 OR [status] = @wparam_1)`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
WhenTrue(expr1) || WhenTrue(expr2) || ...     // TRUE
WhenFalse(expr1) && WhenFalse(expr2) && ...   // FALSE
```

Every boolean argument is rendered as a TRUE condition and a FALSE condition (SQL three-valued logic); when neither holds, the argument is unknown. `or` is TRUE when any argument is TRUE and FALSE when every argument is FALSE, so an unknown argument makes the result unknown unless another argument is TRUE, exactly like SQL `OR`. The FALSE condition is what `not(or(...))` uses. Example: `or(eq(name,"Alice"),eq(name,"Bob"))` builds `e => (e.Name != null && e.Name == p0) || (e.Name != null && e.Name == p1)`, where `p0` and `p1` are captured parameters.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
[w].[Name] = @__Value_0 OR [w].[Name] = @__Value_1
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda.

Every EF6 provider supports `or`.

## Notes

- The function name is case-insensitive.
- See [`and`](and.md) and [`not`](not.md) for the other logical functions.
