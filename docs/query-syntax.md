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
| `DateTime` | `"2021-01-01"` | Double-quoted; parsed with `DateTime.TryParse` using the **current culture** — prefer ISO `yyyy-MM-dd` (or a full ISO timestamp) to avoid locale-dependent parsing. |
| `int` / `byte` | `25` | Unquoted. Whichever of `byte`/`int` matches the target type (usually inferred from the paired field). |
| `double` | `19.99` or `1e3` | Unquoted; a value is treated as a `double` if it contains `.`, `e`, or `E`. |
| `bool` | *(not supported)* | There is no boolean literal syntax. A `bool` field can only be compared against another boolean **expression** (e.g. another bool field, or a boolean function's result is never itself comparable since boolean functions return the top-level predicate, not a value you can `eq` against) — in practice, v1 has no way to write `eq(isActive, true)`. |

Numeric literal type inference: when a literal is compared against a field or another literal, its target type is generally taken from the first operand's `ReturnType` (or an explicit target type for single-typed functions, e.g. `substring`'s 2nd/3rd arguments always coerce to `int`). See each function's page under [docs/functions/](functions/README.md) for the exact per-argument coercion.

## Supported types (v1)

`string`, `bool`, `byte`, `int`, `double`, `DateTime`.

**Not supported in v1:** `DateOnly`, `TimeOnly`, `Guid`, `float`, `decimal`. Model dates/times as `DateTime` fields.

## Field and literal "operands"

Two non-function node types can appear as arguments anywhere a function expects an expression:

- **Field** — a reference to a catalog field, e.g. `name`, `createdAt`. Resolved against the `(string, Type)[]` you pass to the parser (or that your `IRequestFieldsInfoProvider` returns).
- **Literal** — a constant value parsed from the query string as described above.

Both are part of `Expresso.Core.CriteriaExpressions` but are not "functions" in the reference sense, so they don't have their own page under [docs/functions/](functions/README.md).
