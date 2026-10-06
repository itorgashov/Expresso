# `sqrt`

Returns the square root of a numeric argument.

## Syntax

```text
sqrt(argument)
```

Exactly 1 argument.

- **Category:** Arithmetic
- **Return type:** `double` (always, not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |

## Validation & exceptions

- If you pass a number of arguments other than 1, the parser throws `System.Exception` with the message `"Sqrt() function should have 1 argument."`.
- The parser infers the type of a literal argument.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not `byte`, `int`, or `double` throws `ArgumentException("Illegal argument type", "argument")`.
- When the parser builds the call, these exceptions arrive wrapped in `System.Reflection.TargetInvocationException`. See [docs/error-handling.md](../../error-handling.md).
- A negative input is not rejected when the expression is parsed. See the Notes section.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

```sql
SQRT(argument)
```

SQL Server example: `lte(sqrt(area),10)` renders as `(SQRT([area]) <= @wparam_0)`. A negative input is not rejected at parse time. The engine raises an error at execution (SQL Server error 3623).

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
Math.Sqrt((double)argument)
```

A `byte` or `int` argument is converted to `double` first. The result is NULL when `argument` is NULL. Example: `gt(sqrt(age),6.0)` builds `e => Math.Sqrt((double)e.Age) > p0`, where `age` is a non-nullable `int` column and `p0` is a captured parameter.

### In-memory

Same as Queryable, except a negative argument throws `NotSupportedException`. `isnull(sqrt(...))` throws as well.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
SQRT(CAST([w].[Age] AS float))
```

On SQLite and MySQL a negative argument is NULL, so `isnull(sqrt(...))` is TRUE. On SQL Server, PostgreSQL, Oracle and DB2, `isnull(sqrt(...))` keeps `SQRT` in the SQL so a negative argument still raises. A literal argument stays inside `SQRT` as well.

## EF6 rendering

### All providers

EF6 has no canonical square root, so each supported provider calls its store `SQRT` instead of `Math.Sqrt`. An unrecognized provider keeps the Queryable lambda.

### SQL Server, MySQL / MariaDB, SQLite

`Ef6Functions.SqlServerSqrt`, `Ef6Functions.MySqlSqrt` and `Ef6Functions.SqliteSqrt` map to the store `SQRT`, for example `(double)Ef6Functions.SqlServerSqrt((double?)argument)`. On SQL Server:

```sql
SQRT(CAST([Extent1].[Age] AS float))
```

`isnull(sqrt(...))` on SQL Server compares the nullable `SQRT` with NULL, so a negative argument still raises. On SQLite and MySQL a negative argument is NULL.

### Not supported

PostgreSQL throws `NotSupportedException` because the provider exposes no store functions. Oracle throws `NotSupportedException` because the provider manifest has no SQRT.

## Notes

- Expresso does not reject negative values when it parses or builds the expression, because the sign of a field or literal usually isn't known until the query runs. At execution, SQL Server's `SQRT` raises error 3623 (`"An invalid floating point operation occurred."`) for a negative input. It does **not** return `NULL`. If negative values are possible, filter them out on the underlying column.
- The return type is always `double`, regardless of the argument's original type.
- See [`power`](power.md) for raising to an arbitrary exponent.
