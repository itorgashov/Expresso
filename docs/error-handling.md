# Error handling

Expresso throws .NET exceptions when it rejects input, including structured exceptions for query policies. This page lists which exceptions to expect from parsing, from building the expression tree, and from rendering, and what to catch where. Each [function page](functions/README.md) also lists the validation for that function.

## Recommended pattern

At the API boundary, wrap parsing in a broad `try/catch (Exception)` and return `400 Bad Request`. The query string comes from the caller, so any exception here means it was malformed or referred to a field or type you do not allow.

```csharp
try
{
    filterCriteria = filterParser.Parse(filter, fields.GetValidFilterFields("book"));
}
catch (Exception ex)
{
    // Log server-side details; keep the response empty.
    return BadRequest();
}
```

Catch `Exception`, not only `ArgumentException`. Some parse errors, such as a wrong argument count, are thrown as plain `System.Exception`.

## Parsing

These exceptions come from `IFilterParser.Parse`, `ISortDirectiveParser.Parse`, and `IPagingDirectiveParser.Parse`. Paging rules are in [Pagination](pagination.md).

| Situation | Exception |
|---|---|
| The filter or sort query, or its field catalog (array or `QueryModel`), is `null` | `ArgumentNullException` |
| Unexpected token, unknown function name, illegal field name, unterminated expression | `ArgumentException` |
| A function is called with the wrong number of arguments | `System.Exception`, for example `"Eq() function should have 2 arguments."` |
| A literal cannot be read as its target type, such as a bad date or non-numeric text | `ArgumentException` |
| The filter parses but its root is not a boolean expression | `ArgumentException("A boolean expression is expected.")` |
| A sort has an odd number of tokens, or is empty | `ArgumentException` |
| A sort uses `any`, `all`, `none` or a bare collection name as a key | `ArgumentException` |
| A sort has an invalid `sortfor` path or argument count | `ArgumentException` |
| A filter contains `sortfor(...)` | `ArgumentException`: `'sortfor' is only valid in a sort directive, not in a filter.` |
| A sort direction is not `asc` or `desc` | `NotSupportedException` |

### Paging values

The paging parser accepts text for page number, page size, offset, and row count. These are library inputs; your application chooses any endpoint names. Null, empty, and whitespace-only text values are treated as omitted.

| Situation | Exception |
|---|---|
| A supplied text value contains signs, non-digits, surrounding whitespace, or a number greater than `2147483647` | `ArgumentException` from `IPagingDirectiveParser.Parse` |
| A parsed page number, page size, or row count is below 1, or the computed page offset exceeds `int.MaxValue` | `ArgumentOutOfRangeException` from directive validation |
| An integer passed directly to `PagingDirective` has an invalid range, including a negative offset | `ArgumentOutOfRangeException` |
| `PagingDirective.TotalPages` receives a negative total count or a page size below 1 | `ArgumentOutOfRangeException` |

For example, the text `"-1"` fails parsing with `ArgumentException`; an integer offset of `-1` passed directly to the constructor causes `ArgumentOutOfRangeException`. Mixed limiting models are valid library input: page size takes precedence when supplied. Your application decides whether to reject mixed input or a page number without a page size. See [Pagination](pagination.md#understand-directive-semantics).

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
| `filterCriteria`, `sortDirective`, `paging`, the field map, the mapping or `paramNamePrefix` is `null` | `ArgumentNullException` |
| `filterCriteria.Expression` is `null` | `ArgumentException("The expression of the filter criteria is null.")` |
| `sortDirective.Items` is empty | `ArgumentException("Sort directive must contain at least one item")` |
| `paramNamePrefix` does not match `^[A-Za-z][A-Za-z0-9_]*$` | `ArgumentException("Incorrect prefix for sql parameter names.")` |
| A field in the expression has no entry in the field map | `ArgumentException("No mapping for the {field} field")` |
| A collection in the expression has no entry in `SqlQueryMapping.Collections` | `ArgumentException("No mapping for the {collection} collection")` |
| `any`, `all` or `none` is used as an `ORDER BY` key | `ArgumentException` |
| The expression contains a node the renderer does not know (not expected in normal use) | `NotSupportedException` |

A missing field mapping should not occur if your field map or `SqlQueryMapping` stays in sync with your allow-list. See [Field providers](field-providers.md).

The LINQ renderers throw `ArgumentException` for an unmapped field or collection, and `NotSupportedException` where a provider cannot render a function exactly or `IncludeSorted` cannot use a collection. See [LINQ rendering](linq-rendering.md) and [EF6 limits](semantics.md#ef6-limits).

Paging has two additional provider restrictions:

- EF6 requires ordered input for a positive offset. On SQLite EF6, use `Page(paging, Ef6Provider.Sqlite)` so an offset without a row count has an unlimited `LIMIT` rather than an invalid bare `OFFSET`.
- `Page(paging, db.Database.ProviderName)` throws `NotSupportedException` for a positive offset on IBM EF Core 8. The provider drops that offset, so the overload rejects the query. It does not automatically retrieve and limit keys in memory. See [Pagination](pagination.md#ef-core) for the explicit fallback.

## Query policies

`QueryPolicyCompiler.Compile` throws `QueryPolicyCompileException` for invalid startup configuration. Its `Diagnostics` provide codes, severity, one-based line/column positions, and explanations. Do not treat compilation failures as request errors; fail startup and fix the policy.

A parser can throw `QueryPolicyException` (an `ArgumentException`) after parsing a valid expression. `Kind` distinguishes `DenyRuleMatched`, `NotAllowed`, and `LimitExceeded`. Log `Target`, `Path`, `RuleText`, and `LimitName` where available. The default exception message is generic; detailed messages are opt-in. Keep policy diagnostics out of public error responses.

A policy/catalog mismatch throws `InvalidOperationException` and indicates a host wiring error. Use the models returned by the compiler. See [Query policy](query-policy.md) for limits, matching, and startup examples.

## What to catch where

- In the API layer, catch `Exception` around `IFilterParser.Parse`, `ISortDirectiveParser.Parse`, and `IPagingDirectiveParser.Parse`, and return `400`.
- In the data-access layer, fix mapping and translation errors. Handle known paging provider restrictions through the documented provider-aware calls or an explicit fallback; parsing successfully does not guarantee that a provider can execute the query.

## Next steps

- [Pagination](pagination.md): limiting semantics and provider-aware calls
- [Get started](getting-started.md): register parsers and parse input
- [Function reference](functions/README.md): validation for individual functions
