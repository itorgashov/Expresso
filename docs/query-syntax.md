# Query syntax

## Filter grammar

A filter is a single, possibly nested, function call:

```text
functionName(arg1, arg2, ...)
```

- The **root expression must be a boolean function** (e.g. `eq`, `and`, `startswith`, ...). `IFilterParser.Parse` throws `ArgumentException("A boolean expression is expected.")` if the parsed root is not boolean (see [docs/error-handling.md](error-handling.md)).
- Arguments can be field names, quoted literals, unquoted numeric literals, or nested function calls — nesting is unrestricted (e.g. `eq(abs(age), 1)`, `and(gt(age,25), startswith(name,"Jo"))`).
- Function names and field names are **case-insensitive** (`startswith`, `StartsWith`, `STARTSWITH` are equivalent).
- Field names must match `^[a-zA-Z_][a-zA-Z0-9_]*$` and must appear in the field catalog you supply (see [docs/field-providers.md](field-providers.md)); anything else is an `ArgumentException: Illegal field name: '...'`.

Full function reference (every supported function, grouped by category): [docs/functions/README.md](functions/README.md).

## Collections

Collections are quantified predicates over a related set, not extra scalar fields on the outer entity. Pass a `QueryModel` (with nested `CollectionModel`s) to `IFilterParser.Parse`. Inside `any(authors, …)`, identifiers resolve against the **authors** item catalog.

```text
any(authors, eq(displayname, "Leo Tolstoy"))
eq(count(authors), 2)
and(gt(year, 2020), any(authors, eq(displayname, "Leo Tolstoy")))
any(authors, any(awards, eq(name, "Nobel Prize")))
```

- `year` in the `and(…)` example stays on the outer `WHERE`; it is not pushed into the subquery.
- `min`/`max` are overloaded: `min(price, rating)` is scalar `MinFunc`; `min(authors, dateofbirth)` is `CollectionMinFunc`. `sum`/`avg`/`count` are collection-only.
- `any`/`all`/`none` are not valid sort keys. Collection aggregates (`count`, `min`, …) may be used in `sort=`.

See [docs/field-providers.md](field-providers.md) and [docs/functions/collection/any.md](functions/collection/any.md).

## Sort grammar

A sort directive is a flat, comma-separated list of alternating field and direction tokens:

```text
field1,dir1,field2,dir2,...
```

Example:

```text
createdAt,desc,name,asc
```

- `dir` is `asc` or `desc`, case-insensitive. Anything else throws `NotSupportedException`.
- An odd number of tokens, or an empty string, throws `ArgumentException`.
- `ISortDirectiveParser.Parse(...)` returns a `SortDirective`; call `.RemoveDuplicates()` to drop repeated sort keys (first occurrence wins) before rendering — see [docs/getting-started.md](getting-started.md).

## Literal syntax

| Type | Syntax | Notes |
|---|---|---|
| `string` | `"text"` | Must be double-quoted. No escaped quotes inside the literal. |
| `DateTime` | `"2021-01-01"` | Double-quoted; default ISO `yyyy-MM-dd` exact parse (invariant), then current-culture `TryParse` fallback. Overridable via `LiteralParseOptions` (see below). |
| `Guid` | `"550e8400-e29b-41d4-a716-446655440000"` | Must be double-quoted; `Guid.TryParse` after stripping quotes. |
| `TimeSpan` (time-of-day) | `"14:30:00"` or `"14:30"` | **All TFMs.** Clock time only; default invariant `hh:mm` / `hh:mm:ss` exact parse (no culture fallback). Overridable via `LiteralParseOptions`. |
| `DateOnly` | `"2021-01-01"` | **net6.0 package TFM only.** Default ISO `yyyy-MM-dd` exact, then culture fallback. Overridable via `LiteralParseOptions`. |
| `TimeOnly` | `"14:30:00"` or `"14:30"` | **net6.0 package TFM only.** Default `HH:mm:ss` / `HH:mm` exact, then culture fallback. Overridable via `LiteralParseOptions`. |
| `int` / `byte` | `25` | Unquoted. Whichever of `byte`/`int` matches the target type (usually inferred from the paired field). |
| `double` | `19.99` or `1e3` | Unquoted; a value is treated as a `double` if it contains `.`, `e`, or `E`. |
| `bool` | *(not supported)* | There is no boolean literal syntax. |

Numeric literal type inference: when a literal is compared against a field or another literal, its target type is generally taken from the first operand's `ReturnType` (or an explicit target type for single-typed functions, e.g. `substring`'s 2nd/3rd arguments always coerce to `int`). See each function's page under [docs/functions/](functions/README.md) for the exact per-argument coercion.

### Configuring date/time literal parsing

Use `LiteralParseOptions` (in `Expresso.Parsing`) to set `CultureName`, per-type format arrays (`DateTimeFormats`, `DateFormats`, `TimeFormats`, `TimeSpanFormats`), and `AllowCultureFallback`. When nothing is configured, the defaults in the table above apply.

```csharp
services.AddRequestParametersParsers(o =>
{
    o.CultureName = "nl-NL";
    o.DateTimeFormats = new[] { "dd-MM-yyyy", "yyyy-MM-dd" };
});
```

Bind from `IConfiguration` in your host (not inside the library):

```csharp
services.AddRequestParametersParsers(
    configuration.GetSection("Expresso:Parsing").Get<LiteralParseOptions>() ?? new());
```

First matching format wins when multiple patterns are listed.

## Supported types

**All TFMs:** `string`, `bool`, `byte`, `int`, `double`, `DateTime`, `Guid`, `TimeSpan` (time-of-day only — not SQL intervals).

**net6.0 package TFM only:** `DateOnly`, `TimeOnly` (not available when referencing `lib/netstandard2.0`).

**Not supported:** `float`, `decimal`.

`DateTime`, `DateOnly`, `TimeOnly`, and `TimeSpan` are **not interchangeable** in comparisons — use [`date()`](functions/datetime-getter/date.md) / [`time()`](functions/datetime-getter/time.md) to convert in SQL when needed. `TimeOnly` and `TimeSpan` are also not interchangeable with each other.

## Field, collection, and literal operands

Three non-function node types can appear as arguments:

- **Field** — a reference to a catalog field, e.g. `name`, `createdAt`. Resolved against the current `QueryModel` (or the `(string, Type)[]` you pass to the parser).
- **CollectionRef** — a reference to a nested collection, e.g. `authors` in `any(authors, …)`. Only valid as the first argument of collection functions.
- **Literal** — a constant value parsed from the query string as described above.

They are part of `Expresso.Core.CriteriaExpressions` but are not "functions" in the reference sense, so they don't have their own page under [docs/functions/](functions/README.md).
