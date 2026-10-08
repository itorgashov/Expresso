# Render to LINQ and EF

Expresso can apply a parsed filter and sort directly to an `IQueryable<T>` from EF Core, EF6 or another LINQ provider, and to in-memory collections. You call `Where` and `OrderBy` with the Expresso transformer, and the provider turns the lambdas into SQL. Read this page after [Get started with Expresso](getting-started.md), once you have a `FilterCriteria` and a `SortDirective`.

Two terms appear throughout. The *Queryable* profile builds lambdas that LINQ providers translate to SQL. The *in-memory* profile builds lambdas for objects, with PostgreSQL-style NULL and string rules. The [LINQ rendering](linq-rendering.md) page explains both.

## Choose the packages

| You query | Install |
|---|---|
| EF Core 8 or later | `Expresso.Rendering.EntityFrameworkCore` (brings `Expresso.Rendering.Linq`) |
| EF6 on .NET Framework | `Expresso.Rendering.EntityFramework` (brings `Expresso.Rendering.Linq`) |
| Another LINQ provider, or objects in memory | `Expresso.Rendering.Linq` |

```powershell
dotnet add MyApp.DataAccess package Expresso.Rendering.EntityFrameworkCore
```

The EF packages reference the EF library they extend, so your application controls which EF version is used at run time.

## Map fields to members

A `LinqQueryMapping<T>` says where each query field lives on your entity. It is the LINQ counterpart of the SQL column dictionary. The entities below illustrate the mapping used on this page.

```csharp
public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public double Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Author> Authors { get; set; } = new();
}

public sealed class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

```csharp
using Expresso.Rendering.Linq;

public static class BookMapping
{
    public static readonly LinqQueryMapping<Author> Authors = new LinqQueryMapping<Author>()
        .Field("name", a => a.Name);

    public static readonly LinqQueryMapping<Book> Books = new LinqQueryMapping<Book>()
        .Field("title", b => b.Title)
        .Field("price", b => b.Price)
        .Field("createdAt", b => b.CreatedAt)
        .Collection("authors", b => b.Authors, Authors);
}
```

Field names are case-insensitive. Each member type must be the type declared in the field catalog or its `Nullable<T>`: a field declared as `double` can map to `double` or `double?`, but not to `decimal`.

## Filter and sort with EF Core

EF Core needs one extra step. Expresso maps a few functions to provider-specific SQL, and you register those mappings in the model.

1. Register the Expresso functions in `OnModelCreating`:

   ```csharp
   using Expresso.Rendering.EntityFrameworkCore;

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.HasExpressoFunctions(Database.ProviderName);
       // your entity configuration
   }
   ```

2. Register the context and the transformer:

   ```csharp
   builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
   builder.Services.AddRequestParametersParsers();
   builder.Services.AddEfCoreExpressionTransformations<AppDbContext>();
   ```

   The transformer is scoped and picks its overrides from the context's provider. Inject `IExpressionToLinqTransformer` where you build queries.

3. Apply the filter and sort:

   ```csharp
   using Expresso.Rendering.EntityFrameworkCore;
   using Expresso.Rendering.Linq;

   public async Task<List<Book>> GetBooksAsync(
       FilterCriteria? filter, SortDirective? sort, CancellationToken ct)
   {
       IQueryable<Book> query = _db.Books;

       if (filter is not null)
           query = query.Where(_transformer, filter, BookMapping.Books);

       if (sort is not null && sort.Items.Count > 0)
           query = query.OrderBy(_transformer, sort, BookMapping.Books);

       if (sort is not null)
           query = query.IncludeSorted(_transformer, sort, BookMapping.Books);

       return await query.ToListAsync(ct);
   }
   ```

`Where` and `OrderBy` here are Expresso extension methods on `IQueryable<T>`. They build the lambdas and then call the standard `Queryable.Where` and `Queryable.OrderBy`, so EF Core translates them as usual.

Parent sort keys order the books. A nested `sortfor` directive, such as ordering each book's authors, becomes a filtered `Include` through `IncludeSorted`. It requires each sorted collection to be mapped to a navigation property (`b => b.Authors`). If you map it any other way, `IncludeSorted` throws `NotSupportedException`; order a separate child query with `OrderByNested` instead.

Expresso supports these EF Core providers: SQL Server, PostgreSQL (Npgsql), MySQL and MariaDB (Pomelo or Oracle's `MySql.EntityFrameworkCore`), SQLite, Oracle and DB2 (IBM). With any other provider you get the plain Queryable lambdas, without Expresso's overrides.

## Filter and sort with EF6

EF6 needs no model setup. Create the transformer from the context, or pass the provider's invariant name.

```csharp
using Expresso.Rendering.EntityFramework;
using Expresso.Rendering.Linq;

var transformer = new Ef6ExpressionToLinqTransformer(db);   // provider taken from the connection
// or: new Ef6ExpressionToLinqTransformer("Npgsql")

var books = db.Books
    .Where(transformer, filter, BookMapping.Books)
    .OrderBy(transformer, sort, BookMapping.Books)
    .ToList();
```

With dependency injection, register it once: `services.AddEf6ExpressionTransformations("System.Data.SqlClient")`.

EF6 has no filtered include. To order a child collection, load it with its own query and apply `OrderByNested`:

```csharp
var authors = db.Authors
    .Where(a => authorIds.Contains(a.Id))
    .OrderByNested(transformer, sort, BookMapping.Authors, "authors")
    .ToList();
```

A function that an EF6 provider cannot render with the exact same result throws `NotSupportedException` when you build the query, and the message gives the reason. Expresso never returns an approximation. [EF6 limits](semantics.md#ef6-limits) lists the affected functions per provider.

## Filter and sort in memory

For objects, use the in-memory transformer. Create it directly, or inject the concrete type, which `AddLinqExpressionTransformations()` registers.

```csharp
using Expresso.Rendering.Linq;

var inMemory = new InMemoryExpressionToLinqTransformer();

var matches = books
    .Where(inMemory, filter, BookMapping.Books)
    .OrderBy(inMemory, sort, BookMapping.Books)
    .ToList();
```

Use the in-memory transformer for `IEnumerable<T>`. The Queryable transformer is built for providers that translate to SQL and can throw on objects. For example, `left` becomes `Substring(0, n)`, which throws in memory when the string is shorter than `n`.

In-memory strings compare ordinally, and `NULL` sorts last in ascending order.

If you register both `AddLinqExpressionTransformations()` and `AddEfCoreExpressionTransformations<T>()`, the last call decides which transformer `IExpressionToLinqTransformer` resolves to. Inject `InMemoryExpressionToLinqTransformer` by its concrete type to avoid the ambiguity.

## Build the lambdas yourself

`Where` and `OrderBy` cover most cases. If you compose the query yourself, ask the transformer for the pieces:

```csharp
Expression<Func<Book, bool>> predicate = transformer.BuildPredicate(filter, BookMapping.Books);
IReadOnlyList<LinqSortKey> keys = transformer.BuildSortKeys(sort, BookMapping.Books);

var query = _db.Books
    .Where(predicate)
    .Where(b => b.Price > 0);   // your own conditions
```

Each `LinqSortKey` has a `Key` lambda and a `Direction`. Apply them in order, with `OrderBy` for the first and `ThenBy` for the rest:

```csharp
static IOrderedQueryable<Book> OrderByKeys(IQueryable<Book> source, IReadOnlyList<LinqSortKey> keys)
{
    Expression expression = source.Expression;
    var first = true;
    foreach (var key in keys)
    {
        var descending = key.Direction == SortDirection.Descending;
        var method = (first, descending) switch
        {
            (true, false) => nameof(Queryable.OrderBy),
            (true, true) => nameof(Queryable.OrderByDescending),
            (false, false) => nameof(Queryable.ThenBy),
            _ => nameof(Queryable.ThenByDescending),
        };

        expression = Expression.Call(
            typeof(Queryable), method,
            new[] { typeof(Book), key.Key.ReturnType },
            expression, Expression.Quote(key.Key));
        first = false;
    }

    return (IOrderedQueryable<Book>)source.Provider.CreateQuery<Book>(expression);
}
```

## Limit the result

Create a `PagingDirective` for paged or offset/number results, then apply it after a deterministic order. The examples here assume `orderedSource` is an already filtered and ordered query.

For EF Core, use the provider-aware overload:

```csharp
using Expresso.Core.Paging;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;

var paging = new PagingDirective(page: 3, pageSize: 20);
var limited = orderedSource.Page(paging, db.Database.ProviderName);
```

IBM EF Core 8 drops positive offsets. This overload throws `NotSupportedException` for such a window; it does not automatically page in memory. See [Pagination](pagination.md#ef-core) for the ordered-key fallback and its cost.

For SQLite EF6, use the overload that supplies an unlimited `LIMIT` when an offset has no row count:

```csharp
using Expresso.Core.Paging;
using Expresso.Rendering.EntityFramework;
using Expresso.Rendering.Linq;

var paging = new PagingDirective(skip: 40);
var limited = orderedSource.Page(paging, Ef6Provider.Sqlite);
```

Pass the actual provider for other EF6 databases. EF6 requires an `OrderBy` before any positive offset. On an in-memory `IEnumerable<T>`, use `orderedItems.Page(paging)` from `Expresso.Rendering.Linq`. Expresso does not add a sort or a unique tie-breaker; see [Pagination](pagination.md#establish-a-deterministic-order) for ordering and directive semantics.

## Troubleshoot

| Symptom | Cause and fix |
|---|---|
| EF Core says it cannot translate a call to an `ExpressoDbFunctions` method | The model does not call `HasExpressoFunctions(Database.ProviderName)`. Add it to `OnModelCreating`. |
| `NotSupportedException` from `IncludeSorted` | The sorted collection is not mapped to a navigation property. Map it as `b => b.Authors`, or use `OrderByNested` on a child query. |
| `NotSupportedException` when you build an EF6 query | The provider cannot render that function exactly. See [EF6 limits](semantics.md#ef6-limits). |
| EF6: `Skip` is only supported for sorted input | Call `OrderBy` before `Page` when the offset is greater than 0. See [Pagination](pagination.md). |
| SQLite EF6 rejects a bare `OFFSET` | For an offset without a row count, use `Page(paging, Ef6Provider.Sqlite)` to add an unlimited `LIMIT`. |
| IBM EF Core: provider-aware `Page` throws for a positive offset | The provider drops the offset. Retrieve ordered keys and limit them in memory, as described in [Pagination](pagination.md#ef-core). |
| A filter over objects throws for some inputs, such as `left` on a short string | You used the Queryable transformer on objects. Use `InMemoryExpressionToLinqTransformer`. |
| The wrong transformer is injected | Several registrations compete for `IExpressionToLinqTransformer`. The last one wins. Inject the concrete type. |
| An EF6 `Guid` filter on SQLite matches nothing | System.Data.SQLite binds `Guid` as binary by default. Add `BinaryGUID=False` to the connection string if the database stores GUIDs as text. |

## Supported EF Core versions

`Expresso.Rendering.EntityFrameworkCore` targets .NET 8 and requires EF Core 8 or later. It is tested with EF Core 8. If your application references a newer EF Core, the application's version is the one that runs, and it translates the Expresso lambdas. Results for the functions in the [function reference](functions/README.md) are defined for EF Core 8; a newer EF Core release can change how some SQL is produced.

## Next steps

- [Pagination](pagination.md): the two limiting models, provider-aware calls, and totals
- [LINQ rendering](linq-rendering.md): profiles, providers and mapping details
- [Filter behavior and database differences](semantics.md): NULL handling, types and EF6 limits
- [Function reference](functions/README.md): the LINQ, EF Core and EF6 form of every function
- [Render to SQL](getting-started-sql.md): the other way to run a filter
