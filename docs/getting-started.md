# Getting started

Step-by-step guide to adding Expresso to an application. For a complete, runnable version of all the steps below, see [docs/sample-app.md](sample-app.md) and [samples/Expresso.Sample.WebApi](../samples/Expresso.Sample.WebApi).

## 1. Install the right packages in the right layer

Expresso has no metapackage — install only what a project needs:

| Project / layer | Packages |
|---|---|
| Presentation / API layer (reads `filter`/`sort` query params) | `Expresso.Core`, `Expresso.Parsing` |
| Data-access layer (builds/executes SQL) | `Expresso.Core`, `Expresso.Rendering.SqlServer` |

```powershell
dotnet add MyApp.Api package Expresso.Core
dotnet add MyApp.Api package Expresso.Parsing

dotnet add MyApp.DataAccess package Expresso.Core
dotnet add MyApp.DataAccess package Expresso.Rendering.SqlServer
```

If a single project does both jobs (as in the sample), install `Expresso.Parsing` and `Expresso.Rendering.SqlServer` together — both already reference `Expresso.Core` transitively.

## 2. Register services

```csharp
using Expresso.Parsing;
using Expresso.SqlServer;

builder.Services.AddRequestParametersParsers();     // IFilterParser, ISortDirectiveParser
builder.Services.AddExpressionTransformations();    // IExpressionToQueryClauseTransformer
```

## 3. Implement `IRequestFieldsInfoProvider`

This is the allow-list that tells Expresso which fields exist, what their CLR type is, and — implicitly — which fields are **not** filterable/sortable at all. See [docs/field-providers.md](field-providers.md) for the full explanation; minimal example:

```csharp
using Expresso.Core.Filtering;

public sealed class RequestFieldsInfoProvider : IRequestFieldsInfoProvider
{
    public (string, Type)[] GetValidFilterFields(string context) => context.ToLowerInvariant() switch
    {
        "book" => [("title", typeof(string)), ("year", typeof(int)), ("rating", typeof(double))],
        _ => []
    };

    public (string, Type)[] GetValidSortFields(string context) => GetValidFilterFields(context);
}

builder.Services.AddSingleton<IRequestFieldsInfoProvider, RequestFieldsInfoProvider>();
```

## 4. Parse the incoming query strings

In the layer that received `Expresso.Parsing` (typically a controller or application service):

```csharp
public async Task<IActionResult> GetBooks(
    string? filter, string? sort,
    IFilterParser filterParser, ISortDirectiveParser sortParser,
    IRequestFieldsInfoProvider fields)
{
    FilterCriteria? filterCriteria = string.IsNullOrWhiteSpace(filter)
        ? null
        : filterParser.Parse(filter, fields.GetValidFilterFields("book"));

    SortDirective? sortDirective = string.IsNullOrWhiteSpace(sort)
        ? null
        : sortParser.Parse(sort, fields.GetValidSortFields("book")).RemoveDuplicates();

    var books = await _repository.GetAllAsync(filterCriteria, sortDirective);
    return Ok(books);
}
```

Parsing throws on invalid input — see [docs/error-handling.md](error-handling.md) for exactly what to catch and how to turn it into a `400 Bad Request`.

## 5. Render to SQL in the repository

In the data-access layer, holding `IExpressionToQueryClauseTransformer`:

```csharp
private static readonly Dictionary<string, string> FieldToColumn = new(StringComparer.OrdinalIgnoreCase)
{
    ["title"] = "b.title",
    ["year"] = "b.year",
    ["rating"] = "b.rating",
};

var sql = new StringBuilder("SELECT b.id, b.title, b.year, b.rating FROM dbo.book b");
var parameters = new Dictionary<string, object>();

if (filterCriteria is not null)
{
    var (whereClause, whereParams) = _transformer.RenderWhereClause(filterCriteria, FieldToColumn, "wparam");
    sql.Append(" WHERE ").Append(whereClause);
    foreach (var (key, value) in whereParams) parameters[key] = value;
}

if (sortDirective is not null)
{
    var (orderByClause, orderParams) = _transformer.RenderOrderByClause(sortDirective, FieldToColumn, "oparam");
    sql.Append(" ORDER BY ").Append(orderByClause);
    foreach (var (key, value) in orderParams) parameters[key] = value;
}
```

Use a **case-insensitive** `fieldToColumnMap` (`StringComparer.OrdinalIgnoreCase`) so lookups by field name are robust regardless of casing used when the field catalog was declared.

## 6. Execute the SQL + parameters

Bind `parameters` as `SqlParameter`s (ADO.NET) or pass the dictionary directly (Dapper's `DynamicParameters`) and execute `sql.ToString()` as usual. Expresso only produces the fragment and the parameter values — it does not open a connection or execute anything itself.

## Where to go next

- [docs/query-syntax.md](query-syntax.md) — full filter/sort grammar and literal rules
- [docs/functions/README.md](functions/README.md) — every supported function, with syntax and validation rules
- [docs/error-handling.md](error-handling.md) — what exceptions to expect and catch
- [docs/sample-app.md](sample-app.md) — the same steps, fully wired up in a runnable Web API
