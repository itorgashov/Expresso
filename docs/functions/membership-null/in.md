# `in`

Returns true when the first argument equals any of the remaining arguments.

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
| 2..N | `candidates` | Exactly the same `ReturnType` as the probe (position 1), with no mixed-numeric leniency |

## Validation & exceptions

- If you pass fewer than 2 arguments, the parser throws `System.Exception` with the message `"In() function should have at least 2 arguments."`
- The parser infers the type of a literal probe first, then converts literal candidates to that type.
- If the argument list itself is `null`, the call throws `ArgumentNullException`.
- If the list has fewer than 2 arguments, or any argument is `null`, the call throws `ArgumentException`.
- If a candidate's `ReturnType` differs from the probe's `ReturnType`, the call throws `ArgumentException`.

Unlike [`eq`](../comparison/eq.md), `in` requires an exact `ReturnType` match. It rejects mixed numeric types, such as a `byte` probe against an `int` candidate.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
(probe IN (candidate1, candidate2, ...))
```

SQL Server example: `in(status,1,2,3)` renders as `([status] IN (@wparam_0, @wparam_1, @wparam_2))`.

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
