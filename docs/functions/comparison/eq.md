# `eq`

Compares two values for equality and returns true when they are equal.

## Syntax

```text
eq(left, right)
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

Like all comparison functions, `eq` accepts the two operands only when they form one of these compatible pairs:

- both `bool`
- both `string`
- both `DateTime`
- both `Guid`
- both `TimeSpan` (time-of-day)
- both `DateOnly` (net6.0)
- both `TimeOnly` (net6.0)
- both numeric (`byte`/`int`/`double`, mixed numeric types allowed, e.g. `byte` vs `int`)

Any other pairing → `ArgumentException("Incompatible argument types")`.

## Validation & exceptions

- If you pass any number of arguments other than two, the parser throws `System.Exception` with the message `"Eq() function should have 2 arguments."`
- If the first argument is a literal (quoted or unquoted), the parser infers its type and converts the second argument to that type when the second argument is also a literal.
- If the second argument is not a literal and its type still does not match after that, the call throws `ArgumentException`: `Incompatible argument types: expected {type}, got {type}.`
- If an operand is `null`, the call throws `ArgumentNullException`. If the operand types are incompatible or not allowed, it throws `ArgumentException`.
- When the parser builds the function, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. The real error is in `InnerException`. See [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(left = right)
```

SQL Server example: `eq(status,1)` renders as `([status] = @wparam_0)`.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
left == right   // TRUE
left != right   // FALSE
```

Both conditions also require `left != null && right != null`, so a NULL operand makes the result unknown, as in SQL; the check is left out for an operand that can never be NULL. The FALSE condition is what `not(eq(...))` uses. Numeric operands are promoted first: `byte` becomes `int`, and any `double` operand makes both `double`. `string`, `bool`, `Guid`, `DateTime` and time-of-day `TimeSpan` operands use C# `==` directly. Example: `eq(name,"Bob")` builds `e => e.Name != null && e.Name == p0`, and `eq(code,2)` on a non-nullable `byte` column builds `e => (int)e.Code == (int)p0`, where `p0` is a captured parameter.

### In-memory

Same as Queryable. String `==` is ordinal and case-sensitive in memory, while a database compares by the column collation ([docs/semantics.md](../../semantics.md)).

## EF Core rendering

### All providers

EF Core translates the Queryable lambda and drops the redundant `IS NOT NULL` check. On SQL Server:

```sql
[w].[Name] = @__Value_0
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server:

```sql
([Extent1].[Name] IS NOT NULL) AND (([Extent1].[Name] = @p__linq__0)
  OR (([Extent1].[Name] IS NULL) AND (@p__linq__0 IS NULL)))
```

The `OR (... IS NULL AND @p__linq__0 IS NULL)` branch is EF6's own C# null compensation; it never matches, because the Expresso guard already requires `[Name] IS NOT NULL`. On MySQL 8 (not MariaDB), comparing a computed TIME from [`time`](../datetime-getter/time.md) or a time-of-day `add*` function with a `TimeSpan` parameter never matches, because MySql.Data sends the parameter as `'0 hh:mm:ss.ffffff'`; nothing is thrown.

Every EF6 provider supports `eq`, except that Oracle and SQLite cannot use a time-of-day `TimeSpan` operand: the provider has no time-of-day (Edm.Time) type.

## Notes

- See [`neq`](neq.md) for the negated form, and [`gt`](gt.md)/[`gte`](gte.md)/[`lt`](lt.md)/[`lte`](lte.md) for ordering comparisons (which do not allow `bool` or `string` operands).
