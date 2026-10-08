# Pagination

Expresso supports two ways to limit a query result: paged results, defined by a page number and page size, and offset/number results, defined by a starting offset and a maximum number of rows.

Both models use `PagingDirective` from `Expresso.Core.Paging`. SQL renderers turn the directive into a parameterized clause; LINQ extensions apply it to a query or an in-memory sequence. Your application chooses how to receive these values, including any endpoint parameter names. Expresso does not prescribe an HTTP contract or response format.

## Choose a limiting model

### Paged results

```csharp
using Expresso.Core.Paging;

var paging = new PagingDirective(page: 3, pageSize: 20);
// Offset = 40, Limit = 20
```

The page number is 1-based. Page 1 starts at the first matching row. The page size is the maximum number of rows in each page; the last page can contain fewer rows. Supplying a page size without a page number selects page 1.

The offset is `(page number - 1) * page size`. A page beyond the end of the result returns no rows.

### Offset/number results

```csharp
var window = new PagingDirective(skip: 40, take: 20);
var remaining = new PagingDirective(skip: 40);
var firstRows = new PagingDirective(take: 20);
```

The offset is the number of matching rows to skip, so offset 0 starts at the first row. The row count is the maximum number of rows to return after that offset. If you omit the offset, it defaults to 0. If you omit the row count, return all remaining rows. An offset beyond the end returns no rows.

The constructor arguments `page`, `pageSize`, `skip`, and `take` belong to the .NET API. They represent page number, page size, offset, and row count respectively; they do not define endpoint parameter names.

## Understand directive semantics

Renderers use the normalized `Offset` and `Limit` properties. `Limit` is `null` when there is no upper bound.

| Construction | `Offset` | `Limit` | `IsPageBased` | `IsEmpty` |
|---|---|---|---|---|
| `new PagingDirective(page: 3, pageSize: 20)` | 40 | 20 | `true` | `false` |
| `new PagingDirective(pageSize: 20)` | 0 | 20 | `true` | `false` |
| `new PagingDirective(skip: 40, take: 20)` | 40 | 20 | `false` | `false` |
| `new PagingDirective(skip: 40)` | 40 | `null` | `false` | `false` |
| `new PagingDirective(take: 20)` | 0 | 20 | `false` | `false` |
| `new PagingDirective(page: 3)` | 0 | `null` | `false` | `true` |
| `new PagingDirective(page: 3, skip: 5, take: 10)` | 5 | 10 | `false` | `false` |
| `new PagingDirective(pageSize: 20, skip: 5, take: 10)` | 0 | 20 | `true` | `false` |
| `new PagingDirective(skip: 0)` or `PagingDirective.None` | 0 | `null` | `false` | `true` |

A supplied page size selects paged results and takes precedence over the offset and row count. Without a page size, the page number is ignored and the offset/number model still applies. All supplied values are validated, including values that do not affect the result.

`HasPageAndSkipTake` is true when either page-related argument is supplied together with either offset/number argument. It reports mixed input; it does not reject it. Your application can reject mixed models or a page number without a page size if that better suits its contract.

`IsEmpty` means that the directive imposes no restriction: offset 0 and no limit. It does not mean that the query returns no rows. An empty directive produces no SQL paging clause, and `Page` returns the original source.

### Validate numeric values

The constructor throws `ArgumentOutOfRangeException` if the page number, page size, or row count is below 1, if the offset is negative, or if the computed page offset exceeds `int.MaxValue` (`2147483647`). Each supplied value must fit in an `int`. A row count of 0 is invalid; omitting it means no upper bound.

## Parse text values

Paging is independent of the filter and sort grammar. You can construct a directive directly from integers, or use `IPagingDirectiveParser` from `Expresso.Parsing` to convert text values.

Register the parsers with your dependency injection container:

```csharp
using Expresso.Parsing;

services.AddRequestParametersParsers();
```

Inject `IPagingDirectiveParser` into the component that receives the values. Pass page number, page size, offset, and row count in that order:

```csharp
using Expresso.Core.Paging;
using Expresso.Parsing;

static PagingDirective ParsePaging(
    IPagingDirectiveParser parser,
    string? pageNumberText, string? pageSizeText,
    string? offsetText, string? rowCountText)
{
    return parser.Parse(pageNumberText, pageSizeText, offsetText, rowCountText);
}
```

Null, empty, and whitespace-only strings count as omitted. Other strings must contain only digits and represent a value no greater than `2147483647`. Signs, decimal points, and surrounding whitespace are not accepted. For example, `"-1"`, `"+1"`, `"1.5"`, `" 1"`, and `"2147483648"` cause `ArgumentException`.

After parsing, the constructor applies the range rules above. A text value of `"0"` for the page size or a computed page offset that is too large causes `ArgumentOutOfRangeException`. Literal parsing culture options do not change paging's invariant integer parsing.

## Establish a deterministic order

Apply filtering first, then ordering, then the result limit. Both limiting models depend on the order of the matching rows. Expresso does not add an order or a unique tie-breaker automatically.

Append a unique key when other sort values can be equal:

```csharp
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Sorting;

var stableSort = (sort ?? new SortDirective(Array.Empty<SortDirectiveItem>()))
    .ThenBy(new Field("key", typeof(int)));
```

`ThenBy` returns a new directive, leaving the original unchanged and preserving nested collection sorting. Map the key in the SQL or LINQ mapping even if your parser's field catalog does not expose it to callers.

A unique order gives consistent page boundaries while the matching data is unchanged. Concurrent inserts, deletes, or changes to sort values can still cause rows to repeat or be missed between requests. If consistent results across multiple reads are required, use a database transaction with suitable isolation.

## Render a SQL clause

`RenderPagingClause(paging, paramNamePrefix)` returns a clause containing its keywords and a dictionary of parameters. Append the clause after `ORDER BY`, before executing the query. Bind every returned parameter using its name and value.

The following PostgreSQL example uses an application-defined table and field map. `filter` and `sort` are optional directives parsed separately; every field they use must be mapped. The unique key is available to the renderer even if it is not in the public field catalog.

```csharp
using System.Text;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Paging;
using Expresso.Core.Sorting;
using Expresso.Rendering;

var renderer = new ExpressionToPostgreSqlQueryClauseTransformer();
var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["key"] = "r.record_key",
    ["category"] = "r.category",
};
var paging = new PagingDirective(page: 3, pageSize: 20);
var stableSort = (sort ?? new SortDirective(Array.Empty<SortDirectiveItem>()))
    .ThenBy(new Field("key", typeof(int)));
var sql = new StringBuilder("SELECT r.record_key, r.category FROM result_row r");
var parameters = new Dictionary<string, object>();

if (filter is not null)
{
    var (whereClause, whereParameters) = renderer.RenderWhereClause(filter, columns, "flt");
    sql.Append(" WHERE ").Append(whereClause);
    foreach (var parameter in whereParameters) parameters.Add(parameter.Key, parameter.Value);
}

var (orderClause, orderParameters) = renderer.RenderOrderByClause(stableSort, columns, "ord");
sql.Append(" ORDER BY ").Append(orderClause);
foreach (var parameter in orderParameters) parameters.Add(parameter.Key, parameter.Value);

var (pagingClause, pagingParameters) = renderer.RenderPagingClause(paging, "pg");
sql.Append(' ').Append(pagingClause);
foreach (var parameter in pagingParameters) parameters.Add(parameter.Key, parameter.Value);
```

Use different prefixes for filtering, ordering, and paging to avoid parameter name collisions. A prefix must start with a letter and contain only letters, digits, and underscores. The offset is bound first, as `pg_0`, followed by the limit as `pg_1` when present. In this example their values are 40 and 20. Keep the returned `@` or `:` marker when binding parameters through ADO.NET.

For a nonempty directive, the dialect output is:

| Dialect | With a limit | Without an upper bound |
|---|---|---|
| SQL Server, PostgreSQL, DB2 11.1+ | `OFFSET @pg_0 ROWS FETCH NEXT @pg_1 ROWS ONLY` | `OFFSET @pg_0 ROWS` |
| Oracle 12c+ | `OFFSET :pg_0 ROWS FETCH NEXT :pg_1 ROWS ONLY` | `OFFSET :pg_0 ROWS` |
| MySQL, MariaDB | `LIMIT @pg_1 OFFSET @pg_0` | `LIMIT 18446744073709551615 OFFSET @pg_0` |
| SQLite | `LIMIT @pg_1 OFFSET @pg_0` | `LIMIT -1 OFFSET @pg_0` |

An empty directive returns an empty clause and no parameters. SQL Server requires `ORDER BY` before `OFFSET`, including when the offset is 0. The SQL renderer still binds a zero offset when a limit is present.

DB2 cannot correlate a subquery directly in `ORDER BY` (`SQL0206N`). Lift those sort keys into a derived table as described in [SQL rendering](rendering.md), and append the paging clause to the outer query.

## Apply the limit with LINQ

`Page` applies either model to `IQueryable<T>` or `IEnumerable<T>`. It skips rows only when `Offset` is positive and takes rows only when `Limit` is set. Queryable counts are captured values, allowing EF Core and EF6 to bind them as parameters. The plain overload relies on the query provider to translate `Skip` and `Take` correctly.

The examples below assume a filtered `source` of entities with a unique integer `Key` and a `Category` string. Use your own entity type and matching field catalog:

```csharp
using Expresso.Rendering.Linq;

public sealed class ResultRow
{
    public int Key { get; set; }
    public string Category { get; set; } = "";
}
```

```csharp
var mapping = new LinqQueryMapping<ResultRow>()
    .Field("key", row => row.Key)
    .Field("category", row => row.Category);
```

Use the `stableSort` defined above and the appropriate transformer for your provider.

### EF Core

```csharp
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;

var rows = await source
    .OrderBy(transformer, stableSort, mapping)
    .Page(paging, db.Database.ProviderName)
    .ToListAsync();
```

The provider-aware overload delegates to the normal LINQ window for providers that support it. For IBM EF Core 8, a positive offset throws `NotSupportedException`: the provider drops the offset and translates a bounded window to `FETCH FIRST`. The first page and a row limit without an offset remain supported. ADO.NET with the DB2 SQL renderer uses the `OFFSET`/`FETCH` clause in the table above.

For an IBM EF Core positive offset, you can retrieve the ordered keys first, apply the limit in memory, load the selected entities, and restore their order:

```csharp
var orderedKeys = await source
    .OrderedKeys(transformer, stableSort, mapping, row => row.Key)
    .ToListAsync();
var selectedKeys = orderedKeys.Page(paging).ToList();

var selectedRows = selectedKeys.Count == 0
    ? new List<ResultRow>()
    : await source.Where(row => selectedKeys.Contains(row.Key)).ToListAsync();
var rows = EfCoreLiftedSort.OrderByKeys(selectedKeys, selectedRows, row => row.Key);
```

This fallback materializes every matching key before limiting the list. Consider that memory and transfer cost for large results. The provider-aware `Page` call only rejects an unsupported offset; it does not run this fallback automatically. The key and entity queries must observe consistent data if rows can be removed or changed between them.

### EF6

For SQLite EF6, use the provider-aware overload:

```csharp
using Expresso.Rendering.EntityFramework;
using Expresso.Rendering.Linq;
using System.Data.Entity;

var rows = await source
    .OrderBy(transformer, stableSort, mapping)
    .Page(paging, Ef6Provider.Sqlite)
    .ToListAsync();
```

For an offset without a row count, this overload supplies SQLite's unlimited `LIMIT -1`, bound as a parameter. Plain `Page(paging)` can produce a bare `OFFSET` that SQLite rejects. Pass the actual provider; `Ef6Providers.Resolve(providerInvariantName)` converts an EF6 provider's invariant name to the enum.

EF6 requires an ordered query before any positive offset. An offset of 0 omits `Skip`, so a row limit without an offset is also legal on unordered input. An explicit order is still needed for predictable results.

### In-memory sequences

```csharp
using Expresso.Rendering.Linq;

var rows = items.OrderBy(row => row.Key).Page(paging).ToList();
```

Here `items` is an `IEnumerable<ResultRow>`. The limit is applied as the sequence is enumerated. Both limiting models use the same directive semantics as SQL and EF.

## Calculate totals

`PagingDirective.TotalPages(totalCount, pageSize)` returns the ceiling of the matching row count divided by the page size. A total count of 0 gives 0 pages. A negative count or a page size below 1 causes `ArgumentOutOfRangeException`.

Count the filtered query before applying the result limit. For EF Core:

```csharp
using Expresso.Core.Paging;
using Microsoft.EntityFrameworkCore;

var paging = new PagingDirective(page: 3, pageSize: 20);
var totalCount = await source.LongCountAsync();
var totalPages = PagingDirective.TotalPages(totalCount, paging.PageSize!.Value);
```

For SQLite EF6, use `CountAsync()` instead: its provider translates `LongCount` to `BigCount`, which SQLite does not provide. `CountAsync()` returns a 32-bit count; converting it to `long` does not remove that limit. Choose a provider-supported counting method suitable for your expected result size.

You can report totals with either limiting model. A page count requires a page size, so an offset/number directive does not by itself define a total number of pages. A request beyond the final row can return an empty result while the total count is positive. Expresso leaves the transport and representation of totals to your application.

There are three common approaches:

1. Run a separate count query over the same filtered result. For SQL, use the same source and filtering conditions and choose the database's appropriate count aggregate. This approach also reports totals when the limited result is empty. Separate reads can observe different data unless you use a transaction with suitable isolation.
2. If your database supports `COUNT(*) OVER ()`, include it in your own SQL statement to return the count on each row. An empty result has no row carrying the total. `RenderPagingClause` and `Page` do not add this window function.
3. Omit totals and retrieve one more row than the requested count. Remove that extra row before returning the result; its presence indicates that more rows are available. This avoids the count query and suits incremental loading.

## Next steps

- [Get started](getting-started.md): register parsers and choose a renderer
- [Render to SQL](getting-started-sql.md): build and execute a parameterized query
- [Render to LINQ and EF](getting-started-linq.md): map entities and configure providers
- [Error handling](error-handling.md): input validation and provider exceptions
