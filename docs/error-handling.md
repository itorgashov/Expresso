# Error handling

Expresso throws standard .NET exceptions when it rejects input, instead of returning error codes. This page lists which exceptions to expect from parsing, from building the expression tree, and from rendering, and what to catch where. Each [function page](functions/README.md) also lists the validation for that function.

## Recommended pattern

At the API boundary, wrap parsing in a broad `try/catch (Exception)` and return `400 Bad Request`. The query string comes from the caller, so any exception here means it was malformed or referred to a field or type you do not allow. The controllers in `samples/Expresso.Sample.WebApi` do this.

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

Catch `Exception`, not only `ArgumentException`. Some parse errors, such as a wrong argument count, are thrown as plain `System.Exception`.

## Parsing

These exceptions come from `IFilterParser.Parse` and `ISortDirectiveParser.Parse`.

| Situation | Exception |
|---|---|
| `query` or the field catalog (array or `QueryModel`) is `null` | `ArgumentNullException` |
| Unexpected token, unknown function name, illegal field name, unterminated expression | `ArgumentException` |
| A function is called with the wrong number of arguments | `System.Exception`, for example `"Eq() function should have 2 arguments."` |
| A literal cannot be read as its target type, such as a bad date or non-numeric text | `ArgumentException` |
| The filter parses but its root is not a boolean expression | `ArgumentException("A boolean expression is expected.")` |
| A sort has an odd number of tokens, or is empty | `ArgumentException` |
| A sort uses `any`, `all`, `none` or a bare collection name as a key | `ArgumentException` |
| A sort has an invalid `sortfor` path or argument count | `ArgumentException` |
| A filter contains `sortfor(...)` | `ArgumentException`: `'sortfor' is only valid in a sort directive, not in a filter.` |
| A sort direction is not `asc` or `desc` | `NotSupportedException` |

### Wrapped exceptions

For the comparison functions (`eq`, `neq`, `gt`, `gte`, `lt`, `lte`) and the arithmetic functions (`abs`, `add`, `sub`, `mult`, `div`, `mod`, `floor`, `ceiling`/`ceil`, `sign`, `power`/`pow`, `sqrt`, `min`, `max`), the parser creates the function through reflection. If the function rejects its arguments, the exception reaches you wrapped in `System.Reflection.TargetInvocationException`. Read `.InnerException` to get the real `ArgumentException` or `ArgumentNullException`.

The other functions are not wrapped: string, logical (`and`, `or`, `not`), `in`, `isnull`, date and time, `round`, and the collection functions (`any`, `all`, `none`, `count`, and collection `min`, `max`, `sum`, `avg`).

If you catch `Exception` at the API boundary as recommended above, you do not need to unwrap anything: the message of the wrapper is generic, so log or return `ex.InnerException?.Message ?? ex.Message`.

## Building the expression tree

Every function validates its arguments when it is created:

| Situation | Exception |
|---|---|
| A required argument is `null` | `ArgumentNullException` |
| An argument's type is not one the function accepts | `ArgumentException` |
| A function that takes any number of arguments (`and`, `or`, `in`, `concat`) gets fewer than its minimum | `ArgumentException` |

The **Validation & exceptions** section of each function page lists the exact types and messages.

## Rendering

These exceptions come from the SQL renderers. They almost always point to a mistake in your code, not to bad user input.

| Situation | Exception |
|---|---|
| `filterCriteria`, `sortDirective`, the field map, the mapping or `paramNamePrefix` is `null` | `ArgumentNullException` |
| `filterCriteria.Expression` is `null` | `ArgumentException("The expression of the filter criteria is null.")` |
| `sortDirective.Items` is empty | `ArgumentException("Sort directive must contain at least one item")` |
| `paramNamePrefix` does not match `^[A-Za-z][A-Za-z0-9_]*$` | `ArgumentException("Incorrect prefix for sql parameter names.")` |
| A field in the expression has no entry in the field map | `ArgumentException("No mapping for the {field} field")` |
| A collection in the expression has no entry in `SqlQueryMapping.Collections` | `ArgumentException("No mapping for the {collection} collection")` |
| `any`, `all` or `none` is used as an `ORDER BY` key | `ArgumentException` |
| The expression contains a node the renderer does not know (not expected in normal use) | `NotSupportedException` |

A missing field mapping should not occur if your field map or `SqlQueryMapping` stays in sync with your allow-list. See [Field providers](field-providers.md).

The LINQ renderers throw `ArgumentException` for an unmapped field or collection, and `NotSupportedException` where a provider cannot render a function exactly or `IncludeSorted` cannot use a collection. See [LINQ rendering](linq-rendering.md) and [EF6 limits](semantics.md#ef6-limits).

## What to catch where

- In the API layer, catch `Exception` around `IFilterParser.Parse` and `ISortDirectiveParser.Parse`, and return `400`.
- In the data-access layer, treat rendering exceptions as bugs to fix and return `500`, not as request validation.
