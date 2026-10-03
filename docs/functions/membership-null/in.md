# `in`

True if the first argument equals **any** of the remaining arguments.

## Syntax

```text
in(probe, candidate1, candidate2, ...)
```

At least 2 arguments total (a probe plus at least one candidate).

- **Category:** Membership / null
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `probe` (arguments[0]) | Any supported type |
| 2..N | `candidates` | **Exactly the same `ReturnType`** as the probe (position 1) — no mixed-numeric leniency |

## Validation & exceptions

- **Parser arity check:** fewer than 2 arguments → `System.Exception`: `"In() function should have at least 2 arguments."`
- **Parser coercion:** the probe's literal type is inferred first (`GetLiteralType`); remaining literal candidates are coerced to that same type.
- **IR construction** (`InFunc`):
  - `arguments` is `null` → `ArgumentNullException`
  - Fewer than 2 arguments → `ArgumentException`
  - Any argument is `null` → `ArgumentException`
  - Any argument's `ReturnType` differs from `arguments[0].ReturnType` → `ArgumentException`

Unlike [`eq`](../comparison/eq.md), `in` requires an **exact** `ReturnType` match — mixed numeric types (e.g. `byte` probe against an `int` candidate) are rejected.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(probe IN (candidate1, candidate2, ...))
```

Example: `in(status,1,2,3)` renders as `([status] IN (@wparam_0, @wparam_1, @wparam_2))` on SQL Server.

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
probe == candidate1 || probe == candidate2 || ...   // TRUE
probe != candidate1 && probe != candidate2 && ...   // FALSE
```

Each equality is an [`eq`](../comparison/eq.md) with its own NULL check, `probe` is repeated in every one, and each literal candidate is a separate captured parameter; no list or `Contains` call is emitted. A NULL `probe` makes the result unknown, and so does a NULL candidate when no other candidate matches, exactly like SQL `IN` and `NOT IN`. The FALSE condition is what `not(in(...))` uses. `byte` operands are widened to `int`. Example: `in(name,"Alice","Eve")` builds `e => (e.Name != null && e.Name == p0) || (e.Name != null && e.Name == p1)`, where `p0` and `p1` are captured parameters.

### In-memory

Same as Queryable. String `==` is ordinal and case-sensitive in memory, while a database compares by the column collation ([docs/semantics.md](../../semantics.md)).

## EF Core rendering

### All providers

EF Core translates the Queryable lambda, so the SQL is an `OR` chain rather than `IN`. On SQL Server:

```sql
[w].[Name] = @__Value_0 OR [w].[Name] = @__Value_1
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda into the same `OR` chain.

Every EF6 provider supports `in`, except that Oracle and SQLite cannot use a time-of-day `TimeSpan` operand: the provider has no time-of-day (Edm.Time) type.

## Notes

- See [`isnull`](isnull.md) for the other membership/null function.
