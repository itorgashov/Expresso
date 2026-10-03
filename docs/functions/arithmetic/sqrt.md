# `sqrt`

Square root of a numeric argument.

## Syntax

```text
sqrt(argument)
```

Exactly 1 argument.

- **Category:** Arithmetic
- **Return type:** `double` (always — not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1` → `System.Exception`: `"Sqrt() function should have 1 argument."`
- **Parser coercion:** a literal argument's type is inferred (`GetLiteralType`).
- **IR construction** (`SqrtFunc`, built via reflection): base `NumericSingleArgDoubleFunction`/`NumericSingleArgFunction` throws `ArgumentNullException` for a `null` argument, or `ArgumentException("Illegal argument type", nameof(argument))` if the `ReturnType` isn't `byte`/`int`/`double`. Surfaces wrapped in `TargetInvocationException` when thrown from the parser — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
SQRT(argument)
```

Example: `lte(sqrt(area),10)` renders as `(SQRT([area]) <= @wparam_0)` on SQL Server. A negative input is not rejected at parse time; the engine raises an error at execution (SQL Server error 3623).

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
Math.Sqrt((double)argument)
```

A `byte` or `int` argument is converted to `double` first. The result is NULL when `argument` is NULL. Example: `gt(sqrt(age),6.0)` builds `e => Math.Sqrt((double)e.Age) > p0`, where `age` is a non-nullable `int` column and `p0` is a captured parameter.

### In-memory

Same as Queryable. A negative argument yields `NaN` in memory, while SQL Server raises error 3623; what other engines return is engine-defined, see [docs/semantics.md](../../semantics.md).

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
SQRT(CAST([w].[Age] AS float))
```

No provider overrides.

## EF6 rendering

### All providers

EF6 has no canonical square root, so each supported provider calls its store `SQRT` instead of `Math.Sqrt`. An unrecognized provider keeps the Queryable lambda.

### SQL Server, MySQL / MariaDB, SQLite

`Ef6Functions.SqlServerSqrt`, `Ef6Functions.MySqlSqrt` and `Ef6Functions.SqliteSqrt` map to the store `SQRT`, for example `(double)Ef6Functions.SqlServerSqrt((double?)argument)`. On SQL Server:

```sql
SQRT(CAST([Extent1].[Age] AS float))
```

### Not supported

PostgreSQL throws `NotSupportedException` because the provider exposes no store functions. Oracle throws `NotSupportedException` because the provider manifest has no SQRT.

## Notes

- **Negative values are not rejected at parse/build time** — the negativity of a field or literal generally can't be known until query execution. At execution, SQL Server's `SQRT` raises error 3623 (`"An invalid floating point operation occurred."`) for a negative input; it does **not** return `NULL`. Guard against this with a filter on the underlying column if negative values are possible.
- Return type is always `double`, regardless of the argument's original type.
- See [`power`](power.md) for raising to an arbitrary exponent.
