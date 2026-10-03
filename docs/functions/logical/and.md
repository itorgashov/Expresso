# `and`

Returns true only when every argument is true.

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

- If you pass fewer than 2 arguments, the parser throws `System.Exception` with the message `"And() function should at least 2 arguments."` (see the note on plain-`Exception` arity errors in [docs/error-handling.md](../../error-handling.md)).
- If the argument list itself is `null`, the call throws `ArgumentNullException`.
- If the list has fewer than 2 arguments, the call throws `ArgumentException` ("contains less elements than expected: 2").
- If any argument is `null`, or any argument does not return `bool`, the call throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(expr1 AND expr2 AND ...)
```

SQL Server example: `and(gt(age,25),eq(status,1))` renders as `([age] > @wparam_0 AND [status] = @wparam_1)`.

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

- The function name is case-insensitive (`and`, `AND`, `And`).
- See [`or`](or.md) and [`not`](not.md) for the other logical functions.
