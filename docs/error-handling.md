# Error handling

Expresso validates aggressively and throws standard .NET exceptions rather than returning error codes. This page catalogs what to expect and catch. Function-specific validation is also listed on each function's own page under [docs/functions/](functions/README.md); this page covers the general picture.

## Recommended pattern

At the API boundary, wrap parsing (and, if you like, rendering) in a broad `try/catch (Exception)` and return `400 Bad Request` — the query string came from the caller, so any exception here means it was malformed or referenced a disallowed field/type. This is what `samples/Expresso.Sample.WebApi`'s controllers do.

```csharp
try
{
    filterCriteria = filterParser.Parse(filter, fields.GetValidFilterFields("book"));
}
catch (Exception ex)
{
    return BadRequest(ex.Message);
}
```

A broad catch is deliberately recommended here because, as detailed below, arity/syntax errors from the parser are not consistently typed (some are plain `System.Exception`).

## Parsing (`Expresso.Parsing`)

| Situation | Exception |
|---|---|
| `query` or the field-catalog array / `QueryModel` is `null` | `ArgumentNullException` |
| Unexpected token, unknown function name, illegal field name, unterminated expression | `ArgumentException` |
| A function is called with the wrong number of arguments | **`System.Exception`** (plain, not `ArgumentException`) — e.g. `"Eq() function should have 2 arguments."`, `"Startswith() function should have 2 arguments."` |
| A literal can't be parsed as its target type (e.g. bad date, non-numeric text) | `ArgumentException` |
| `IFilterParser.Parse` succeeds but the parsed root is not a boolean expression | `ArgumentException("A boolean expression is expected.")` |
| `ISortDirectiveParser.Parse`: odd token count / empty directive | `ArgumentException` |
| `ISortDirectiveParser.Parse`: `any`/`all`/`none` or a bare collection name used as a sort key | `ArgumentException` |
| `ISortDirectiveParser.Parse`: direction token is not `asc`/`desc` | `NotSupportedException` |

**Note on arity errors:** these are intentionally documented as plain `System.Exception` because that is what the current implementation throws — this is a known inconsistency (most other validation uses `ArgumentException`/`ArgumentNullException`) rather than a documentation oversight. Catch `Exception`, not just `ArgumentException`, if you want to handle all parse failures uniformly.

**Note on reflection wrapping:** comparison functions (`eq`, `neq`, `gt`, `gte`, `lt`, `lte`) and arithmetic functions (`abs`, `add`, `sub`, `mult`, `div`, `mod`, `floor`, `ceiling`/`ceil`, `sign`, `power`/`pow`, `sqrt`, `min`, `max`) are constructed by the parser via reflection (`Activator.CreateInstance`). If the expression-tree constructor itself rejects the arguments (see the next section), the resulting exception is wrapped in `System.Reflection.TargetInvocationException` — inspect `.InnerException` to get the real `ArgumentException`/`ArgumentNullException`. String functions, logical functions (`and`/`or`/`not`), `in`, `isnull`, DateTime functions, `round`, and collection functions (`any`/`all`/`none`/`count` and collection `min`/`max`/`sum`/`avg`) are constructed directly and do **not** get wrapped.

## Expression tree construction (`Expresso.Core`)

Every function validates its own arguments in its constructor:

| Situation | Exception |
|---|---|
| A required argument is `null` | `ArgumentNullException` |
| An argument's `ReturnType` doesn't match what the function requires | `ArgumentException` |
| A variadic function (`and`, `or`, `in`, `concat`) is given fewer arguments than its minimum | `ArgumentException` |

See each function's **Validation & exceptions** section under [docs/functions/](functions/README.md) for the exact allowed types and messages.

## Rendering (`Expresso.Rendering.SqlServer`)

| Situation | Exception |
|---|---|
| `filterCriteria`, `sortDirective`, `fieldToColumnMap`, `mapping`, or `paramNamePrefix` is `null` | `ArgumentNullException` |
| `filterCriteria.Expression` is `null` | `ArgumentException("The expression of the filter criteria is null.", nameof(filterCriteria))` |
| `sortDirective.Items` is `null`/empty | `ArgumentException("Sort directive must contain at least one item", nameof(sortDirective))` |
| `paramNamePrefix` doesn't match `^[A-Za-z][A-Za-z0-9_]*$` | `ArgumentException("Incorrect prefix for sql parameter names.")` |
| A field referenced in the expression has no entry in `fieldToColumnMap` | `ArgumentException($"No mapping for the {field.Name} field")` |
| A collection referenced in the expression has no entry in `SqlQueryMapping.Collections` | `ArgumentException($"No mapping for the {collection.Name} collection")` |
| `any`/`all`/`none` used as an `ORDER BY` key | `ArgumentException` |
| An unrecognized expression-tree node type is encountered (should not happen in normal use) | `NotSupportedException` |

A missing field-to-column mapping should not normally occur if `fieldToColumnMap` / `SqlQueryMapping` is kept in sync with your allow-list — see [docs/field-providers.md](field-providers.md).

## Summary: what to catch where

- **Parsing layer** (presentation/API): catch broadly (`Exception`) around `IFilterParser.Parse` / `ISortDirectiveParser.Parse` calls and return `400`.
- **Rendering layer** (data access): exceptions here almost always indicate a programming error (mismatched field-to-column map, bad prefix) rather than bad user input — treat them as `500`-worthy bugs to fix, not as request validation.
