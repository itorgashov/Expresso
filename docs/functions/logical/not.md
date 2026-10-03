# `not`

Negates a boolean argument: returns true when the argument is false, and false when it is true.

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

- If you pass any number of arguments other than one, the parser throws `System.Exception` with the message `"Not() function should have 1."` (see [docs/error-handling.md](../../error-handling.md)).
- If the argument is `null`, the call throws `ArgumentNullException`.
- If the argument does not return `bool`, the call throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
NOT (expr)
```

SQL Server example: `not(eq(status,1))` renders as `NOT ([status] = @wparam_0)`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
WhenFalse(expr)   // TRUE
WhenTrue(expr)    // FALSE
```

The argument is rendered as a TRUE condition and a FALSE condition (SQL three-valued logic), and `not` swaps them instead of emitting `!`. An unknown argument therefore stays unknown, exactly like SQL `NOT`. A `bool` field argument is TRUE when it is not NULL and true, and FALSE when it is not NULL and false. Example: `not(eq(name,"Alice"))` builds `e => e.Name != null && e.Name != p0`, where `p0` is a captured parameter, so rows with a NULL `name` do not match.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
[w].[Name] <> @__Value_0
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda.

Every EF6 provider supports `not`.

## Notes

- The function name is case-insensitive.
- See [`and`](and.md) and [`or`](or.md) for the other logical functions.
