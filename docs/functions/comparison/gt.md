# `gt`

Greater-than comparison.

## Syntax

```text
gt(left, right)
```

Exactly 2 arguments.

- **Category:** Comparison
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `leftOperand` | `byte`, `int`, `double`, `DateTime`, `TimeSpan`; plus `DateOnly`/`TimeOnly` on net6.0 |
| 2 | `rightOperand` | Same set; see compatibility rule below |

## Type compatibility rule

`GtFunc` first applies the base comparison check (both `bool`, both `string`, both `DateTime`, both `Guid`, both `TimeSpan`, both `DateOnly`/`TimeOnly` on net6.0, or both numeric), then narrows further: only **numeric-vs-numeric** (mixed `byte`/`int`/`double`), **`DateTime` vs `DateTime`**, **`TimeSpan` vs `TimeSpan`**, or same-type **`DateOnly`/`TimeOnly`** on net6.0 are accepted. `Guid` and `bool`/`string` pairs pass the base check but are then rejected — ordering comparisons don't apply to those types.

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Gt() function should have 2 arguments."`
- **Parser coercion:** literal type inferred from the first operand, applied to the second.
- **IR construction** (`GtFunc`, built via reflection): `ArgumentNullException` / `ArgumentException` for `null`/incompatible/`bool`/`string` operands; wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(left > right)
```

Example: `gt(age,25)` renders as `([age] > @wparam_0)` on SQL Server.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
left > right    // TRUE
left <= right   // FALSE
```

Both conditions also require `left != null && right != null`, so a NULL operand makes the result unknown, as in SQL; the check is left out for an operand that can never be NULL. The FALSE condition is what `not(gt(...))` uses. Numeric operands are promoted first: `byte` becomes `int`, and any `double` operand makes both `double`. `DateTime`, time-of-day `TimeSpan`, `DateOnly` and `TimeOnly` operands use the C# operators directly. Example: `gt(age,30)` builds `e => e.Age > p0`, where `age` is a non-nullable `int` column and `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
[w].[Age] > @__Value_0
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server:

```sql
[Extent1].[Age] > @p__linq__0
```

Every EF6 provider supports `gt`, except that Oracle and SQLite cannot use a time-of-day `TimeSpan` operand: the provider has no time-of-day (Edm.Time) type.

## Notes

- See [`gte`](gte.md), [`lt`](lt.md), [`lte`](lte.md) for the other ordering comparisons, and [`eq`](eq.md)/[`neq`](neq.md) for equality (which do allow `bool`/`string`).
