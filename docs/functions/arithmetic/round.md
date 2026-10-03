# `round`

Rounds a numeric argument to a given number of decimal digits (0 if omitted).

## Syntax

```text
round(argument)
round(argument, digits)
```

1 or 2 arguments.

- **Category:** Arithmetic
- **Return type:** `double` (always — not the argument's original type)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `argument` | `byte`, `int`, or `double` |
| 2 (optional) | `digits` | `int` — zero, positive, and **negative** values are allowed |

## Validation & exceptions

- **Parser arity check:** argument count `!= 1 and != 2` → `System.Exception`: `"Round() function should have 1 or 2 arguments."`
- **Parser coercion:** `argument` coerced via literal-type inference if a literal token; `digits` coerced to `int` if a literal token.
- **IR construction** (`RoundFunc`, constructed directly — **not** via reflection, unlike the other numeric functions on this page):
  - `argument` is `null` → `ArgumentNullException`
  - `argument.ReturnType` is not `byte`/`int`/`double` → `ArgumentException("Illegal argument type", nameof(value))`
  - `digits` is `null` (2-argument overload) → `ArgumentNullException`
  - `digits.ReturnType` is not `int` → `ArgumentException`

  Because `RoundFunc` is constructed directly rather than via `Activator.CreateInstance`, these exceptions are **not** wrapped in `TargetInvocationException` — see [docs/error-handling.md](../../error-handling.md).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
The 1-argument form always passes digits `0`.

### SQL Server, SQLite, MySQL / MariaDB, Oracle, DB2

```sql
ROUND(argument, 0)       -- 1-argument form
ROUND(argument, digits)  -- 2-argument form
```

Example: `lte(round(price),20)` renders as `(ROUND([price], 0) <= @wparam_0)` on SQL Server.
Example: `eq(round(price,-1),20)` renders as `(ROUND([price], @wparam_0) = @wparam_1)`.

### PostgreSQL

PostgreSQL has `round(double precision)` and `round(numeric, int)`, but not `round(double precision, int)`. The first argument is cast to `numeric`:

```sql
ROUND(CAST(argument AS numeric), 0)
ROUND(CAST(argument AS numeric), digits)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
Math.Round((double)argument)          // 1-argument form
Math.Round((double)argument, digits)  // 2-argument form
```

A `byte` or `int` argument is converted to `double` first. The result is NULL when `argument` or `digits` is NULL. `Math.Round` is only a translation target: the provider's SQL `ROUND` does the rounding, so negative `digits` work. Example: `eq(round(mult(amount,10.0),-1),510.0)` builds `e => Math.Round(e.Amount * p0, p1) == p2`, where `amount` is a non-nullable `double` column and `p0` to `p2` are captured parameters.

### In-memory

`ExpressoFunctions.Round(argument, digits)`, with `digits` `0` for the 1-argument form, follows PostgreSQL `round(numeric, int)`: it rounds half away from zero (`round(2.5)` is `3`, not .NET's banker's `2`), and a negative `digits` rounds to the left of the decimal point.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
ROUND([w].[Amount], 0)
```

The overrides below call the `ExpressoDbFunctions.Round(argument, digits)` marker, with `digits` `0` for the 1-argument form.

### PostgreSQL

The cast to `numeric` makes the midpoint round away from zero. Npgsql prints the cast in PostgreSQL `::` form:

```sql
ROUND(argument::numeric, digits)
```

### DB2

```sql
ROUND(argument, digits)
```

## EF6 rendering

### All providers

EF6 translates the Queryable lambda with the canonical `Round` function. On SQL Server:

```sql
ROUND([Extent1].[Amount], 0)
```

### Not supported

PostgreSQL throws `NotSupportedException` because EF6 cannot cast to numeric, and PostgreSQL rounds double precision half to even.

## Notes

- **Zero and negative `digits` are supported** — `round(price,-1)` rounds to the nearest ten, matching SQL Server's native `ROUND` semantics.
- SQL Server's `ROUND` rounds away from zero at the midpoint (`ROUND(2.5,0) = 3`), which differs from .NET's default `Math.Round` (banker's rounding, `MidpointRounding.ToEven`). Expresso does not change this — the SQL Server behavior is what executes.
- Return type is always `double`, regardless of the argument's original type.
- See [`floor`](floor.md)/[`ceiling`](ceiling.md) for rounding to the nearest whole number in a fixed direction.
