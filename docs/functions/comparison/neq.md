# `neq`

Compares two values for inequality and returns true when they differ.

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
| 1 | `leftOperand` | `byte`, `int`, `double`, `DateTime`, `bool`, `string`, `Guid`, `TimeSpan`; plus `DateOnly`/`TimeOnly` on net6.0 |
| 2 | `rightOperand` | Same set; see compatibility rule below |

## Type compatibility rule

Same as [`eq`](eq.md): both `bool`, both `string`, both `DateTime`, or both numeric (mixed `byte`/`int`/`double` allowed). Anything else → `ArgumentException("Incompatible argument types")`.

## Validation & exceptions

- If you pass any number of arguments other than two, the parser throws `System.Exception` with the message `"Neq() function should have 2 arguments."`
- Literal types are inferred as in [`eq`](eq.md): the type of the first argument determines how the second literal is converted.
- If an operand is `null`, the call throws `ArgumentNullException`. If the operand types are incompatible, it throws `ArgumentException`.
- When the parser builds the function, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. The real error is in `InnerException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(left != right)
```

SQL Server example: `neq(status,1)` renders as `([status] != @wparam_0)`. Every dialect uses `!=`, not `<>`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
left != right   // TRUE
left == right   // FALSE
```

Both conditions also require `left != null && right != null`, so a NULL operand makes the result unknown and the row does not match, unlike plain C# `!=`; the check is left out for an operand that can never be NULL. The FALSE condition is what `not(neq(...))` uses. Numeric operands are promoted first: `byte` becomes `int`, and any `double` operand makes both `double`. Example: `neq(name,"Bob")` builds `e => e.Name != null && e.Name != p0`, where `p0` is a captured parameter.

### In-memory

Same as Queryable. String `!=` is ordinal and case-sensitive in memory, while a database compares by the column collation ([docs/semantics.md](../../semantics.md)).

## EF Core rendering

### All providers

EF Core translates the Queryable lambda and drops the redundant `IS NOT NULL` check. On SQL Server:

```sql
[w].[Name] <> @__Value_0
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda.

Every EF6 provider supports `neq`, except that Oracle and SQLite cannot use a time-of-day `TimeSpan` operand: the provider has no time-of-day (Edm.Time) type.

## Notes

- See [`eq`](eq.md) for the positive form.
