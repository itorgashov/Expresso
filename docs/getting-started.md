# Get started with Expresso

Expresso turns a `filter` and `sort` query string into a validated expression tree, then renders that tree as parameterized SQL or as LINQ. The first steps are the same for both: install the parsing packages, register the parser, describe which fields clients may use, and parse the incoming query strings. Then you choose how to run the result.

## Install the parsing packages

Expresso has no metapackage. Install only what each project needs.

| Layer | Packages |
|---|---|
| API layer, where the `filter` and `sort` query parameters arrive | `Expresso.Core`, `Expresso.Parsing` |
| Data-access layer, where the query runs | `Expresso.Core` plus the renderer you choose in [Choose how to run the filter](#choose-how-to-run-the-filter) |

```powershell
dotnet add MyApp.Api package Expresso.Core
dotnet add MyApp.Api package Expresso.Parsing
```

If one project does both jobs, install the parsing package and the renderer package in that project. Both bring `Expresso.Core` with them.

## Register the parser

```csharp
using Expresso.Parsing;

builder.Services.AddRequestParametersParsers();
```

To change how date and time literals are read, pass options:

```csharp
builder.Services.AddRequestParametersParsers(o =>
{
    o.CultureName = "nl-NL";
    o.DateTimeFormats = new[] { "dd-MM-yyyy", "yyyy-MM-dd" };
});
```

`Expresso.Parsing` does not read configuration on its own. To bind the options from `appsettings.json`, do it in the host:

```csharp
builder.Services.AddRequestParametersParsers(
    builder.Configuration.GetSection("Expresso:Parsing").Get<LiteralParseOptions>() ?? new());
```

## Describe the fields clients can use

Implement `IRequestFieldsInfoProvider`. It is the allow-list: it names each field, gives its CLR type, and implicitly rejects every other field. A client can never filter or sort on a column you did not list. The [field providers](field-providers.md) page covers the details.

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

## Parse the query strings

Parse in the layer that received the request, typically a controller or an application service:

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

Parsing throws on invalid input. The [error handling](error-handling.md) page lists what to catch and how to return a `400 Bad Request`.

## Limit the result

Expresso supports paged results (page number and page size) and offset/number results (rows to skip and maximum rows to return). Create a `PagingDirective` separately from the filter and sort:

```csharp
using Expresso.Core.Paging;

var paged = new PagingDirective(page: 3, pageSize: 20);
var offsetNumber = new PagingDirective(skip: 40, take: 20);
```

Both directives skip 40 matching rows and return at most 20. Pass the chosen directive to the SQL renderer or LINQ extension described in [Pagination](pagination.md). Use `PagingDirective.None` when you do not want a result limit.

If the values arrive as text, inject `IPagingDirectiveParser`, which `AddRequestParametersParsers` also registers. Its `Parse` method accepts page number, page size, offset, and row count in that order. Your application chooses how to receive and name these inputs; the .NET argument names shown above do not impose an endpoint contract.

## Choose how to run the filter

`FilterCriteria` and `SortDirective` are independent of the database. Pick the renderer that matches how your application reads data.

| If you use | Read | Packages |
|---|---|---|
| ADO.NET or Dapper | [Render to SQL](getting-started-sql.md) | `Expresso.Rendering.SqlServer`, `PostgreSql`, `Sqlite`, `MySql`, `Oracle` or `Db2` |
| EF Core, EF6, another LINQ provider, or objects in memory | [Render to LINQ and EF](getting-started-linq.md) | `Expresso.Rendering.Linq`, plus `EntityFrameworkCore` or `EntityFramework` |

You can use both in the same application, for example SQL for reports and EF Core for the rest.

## Next steps

- [Render to SQL](getting-started-sql.md)
- [Render to LINQ and EF](getting-started-linq.md)
- [Pagination](pagination.md): paged and offset/number results, ordering, and totals
- [Query syntax](query-syntax.md): the filter and sort grammar and literal rules
- [Function reference](functions/README.md): every supported function
