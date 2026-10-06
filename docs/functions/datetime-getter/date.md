# `date`

Converts a value to a calendar date (day only). The database does the conversion; the function itself performs no conversion in C#.

## Syntax

```text
date(value)
```

Exactly 1 argument.

- **Category:** DateTime getter / conversion
- **Return type:** `DateOnly` on **net6.0**; `DateTime` on **netstandard2.0**

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `value` | `DateTime`, `string`, or `DateOnly` (net6.0) |

On **netstandard2.0**, only `DateTime` is accepted.

## Validation & exceptions

- If you pass any number of arguments other than 1, parsing fails with `System.Exception`: `"Date() function should have 1 argument."`
- The parser treats a quoted value as a string literal. It does not parse it as a `DateTime` in C#.
- A `null` argument throws `ArgumentNullException`.
- An argument whose type is not listed in the Arguments table throws `ArgumentException`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
The database does the conversion; the function itself does not convert in C#.

### SQL Server, PostgreSQL, MySQL / MariaDB, DB2

```sql
CAST(value AS date)
```

SQL Server example: `eq(date(createdat),"2020-01-01")` renders as `(CAST([created_at] AS date) = @wparam_0)`. On net6.0, the parameter is a `DateOnly` literal.

### SQLite

```sql
date(value)
```

### Oracle

```sql
TRUNC(value)
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
DateOnly.FromDateTime(value)
```

On netstandard2.0 the shape is `value.Date`. A `DateOnly` argument is passed through unchanged. Unlike SQL rendering, a string literal is parsed in C# with the invariant culture (`DateOnly.FromDateTime(DateTime.Parse(value))`, or `DateTime.Parse(value).Date` on netstandard2.0) and sent as a parameter; any other string argument throws `NotSupportedException` ("DateFunc accepts a string argument only as a literal in LINQ rendering."). The result is NULL when `value` is NULL. Example: `eq(date(createdat),"2020-01-01")` builds `e => DateOnly.FromDateTime(e.CreatedAt) == p0`, where `p0` is a captured `DateOnly` parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
CAST([w].[created_at] AS date)
```

### Oracle

`ExpressoDbFunctions.Date`:

```sql
TRUNC(value)
```

The result is a `DATE`, so `year(date(createdat))` stays on a date. A comparison with a `DateOnly` literal binds the parameter as `DateTime`. A `DateOnly` column mapped as text (`NVARCHAR2`) still throws `NotSupportedException`; map it to `DATE`. See [docs/semantics.md](../../semantics.md).

### DB2

`ExpressoDbFunctions.Date`:

```sql
DATE(value)
```

## EF6 rendering

### All providers

EF6 uses `DbFunctions.TruncateTime(value)`. EF6 runs on net48, where `date` returns `DateTime`, so the comparison uses a `DateTime` parameter. On SQL Server:

```sql
cast(cast([Extent1].[created_at] as date) as datetime2)
```

### MySQL / MariaDB

`Ef6Functions.MySqlDate(value)`, the store function `DATE(value)`.

SQLite throws `NotSupportedException`: "the provider does not translate TruncateTime".

## Notes

- On net6.0, compare `date(...)` results to `DateOnly` fields or literals, not to raw `DateTime` fields. Use `eq(date(createdat), date(other))` or compare to a `DateOnly` literal.
- When the underlying column is `datetime` or `datetime2`, this is equivalent to comparing "same calendar day".
