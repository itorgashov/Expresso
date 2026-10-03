# `lt`

Less-than comparison.

## Syntax

```text
lt(left, right)
```

Exactly 2 arguments.

- **Category:** Comparison
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `leftOperand` | `byte`, `int`, `double`, `DateTime`, `TimeSpan`; plus `DateOnly`/`TimeOnly` on net6.0 |
| 2 | `rightOperand` | Same set; see [`gt`](gt.md) for the full compatibility rule (identical here) |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Lt() function should have 2 arguments."`
- **Parser coercion:** literal type inferred from the first operand, applied to the second.
- **IR construction** (`LtFunc`, built via reflection): `ArgumentNullException` / `ArgumentException` for `null`/incompatible/`bool`/`string` operands; wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(left < right)
```

Example: `lt(dateFrom,dateTo)` renders as `([date_from] < [date_to])` on SQL Server.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
left < right    // TRUE
left >= right   // FALSE
```

Both conditions also require `left != null && right != null`, so a NULL operand makes the result unknown, as in SQL; the check is left out for an operand that can never be NULL. The FALSE condition is what `not(lt(...))` uses. Numeric operands are promoted first: `byte` becomes `int`, and any `double` operand makes both `double`. `DateTime`, time-of-day `TimeSpan`, `DateOnly` and `TimeOnly` operands use the C# operators directly. Example: `lt(age,25)` builds `e => e.Age < p0`, where `age` is a non-nullable `int` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
[w].[Age] < @__Value_0
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server:

```sql
[Extent1].[Age] < @p__linq__0
```

Every EF6 provider supports `lt`, except that Oracle and SQLite cannot use a time-of-day `TimeSpan` operand: the provider has no time-of-day (Edm.Time) type.

## Notes

- See [`lte`](lte.md), [`gt`](gt.md), [`gte`](gte.md) for the other ordering comparisons.
