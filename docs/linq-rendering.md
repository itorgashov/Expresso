# LINQ rendering

Expresso can render a filter and a sort as LINQ lambdas for `IQueryable<T>` (EF Core, EF6 or another LINQ provider) and for in-memory collections. The lambdas follow the same rules as the SQL renderers for NULL logic, types and parameters; see [Filter behavior and database differences](semantics.md). To set up a project, start with [Render to LINQ and EF](getting-started-linq.md). Each [function page](functions/README.md) has LINQ, EF Core and EF6 sections.

## Choose a package

| Package | Target | Use for |
|---|---|---|
| `Expresso.Rendering.Linq` | `netstandard2.0`, `net6.0` | Any `IQueryable<T>` provider (Queryable profile) and in-memory `IEnumerable<T>` (in-memory profile). |
| `Expresso.Rendering.EntityFrameworkCore` | `net8.0` (EF Core 8+) | EF Core queries. Adds provider overrides so that results match the SQL renderer for the same engine. |
| `Expresso.Rendering.EntityFramework` | `net48` (EF 6.5) | EF6 queries on .NET Framework. Adds provider overrides; functions a provider cannot render exactly throw. |

Both EF packages depend on `Expresso.Rendering.Linq`. On .NET Framework, use the EF6 package for database queries and the Linq package for objects; the ADO dialect renderers work there too.

## Mapping

`LinqQueryMapping<T>` is the LINQ counterpart of `SqlQueryMapping`: it maps query field names to member lambdas and collection names to navigation lambdas. Names are case-insensitive, and a member type must be the field's catalog type or its `Nullable<T>`.

```csharp
var authors = new LinqQueryMapping<Author>()
    .Field("name", a => a.Name);

var books = new LinqQueryMapping<Book>()
    .Field("title", b => b.Title)
    .Field("price", b => b.Price)          // double or double?
    .Collection("authors", b => b.Authors, authors);
```

The field catalog used by the parser ([docs/field-providers.md](field-providers.md)) stays the allow-list; the mapping only says where each field lives.

## Profiles

- The Queryable profile (`QueryableExpressionToLinqTransformer`) emits the BCL members that LINQ providers translate (`string.Substring`, `DateTime.Year`, `Enumerable.Any`, …). Use it with a LINQ provider that translates to SQL. It is not meant for LINQ to objects: for example, `left` becomes `Substring(0, n)`, which throws in memory when the string is shorter than `n`.
- The in-memory profile (`InMemoryExpressionToLinqTransformer`) follows PostgreSQL semantics through `ExpressoFunctions`, compares strings ordinally, and sorts with `InMemorySortComparer` (NULL last in ascending order).

```csharp
using Expresso.Rendering.Linq;

var transformer = new QueryableExpressionToLinqTransformer();
var page = db.Books
    .Where(transformer, filter, books)      // Expression<Func<Book, bool>>
    .OrderBy(transformer, sort, books)      // OrderBy / ThenBy from the parent sort keys
    .ToList();

var inMemory = new InMemoryExpressionToLinqTransformer();
var matches = list.Where(inMemory, filter, books).OrderBy(inMemory, sort, books);
```

`BuildPredicate` and `BuildSortKeys` return the raw lambdas if you compose queries yourself. `OrderByNested` on `IQueryable<T>` orders a child query by the `sortfor` directive at that path. The `IEnumerable<T>` overloads of `OrderBy` and `OrderByNested` compile the lambda and run it in memory: use `InMemoryExpressionToLinqTransformer`, not an EF Core or EF6 transformer (those emit provider-only functions).

DI: `services.AddLinqExpressionTransformations()` registers the Queryable profile as `IExpressionToLinqTransformer` and the in-memory profile as its concrete type (both singletons).

## EF Core

Register the provider's translations in the model, then use the transformer for the context's provider:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasExpressoFunctions(Database.ProviderName);
    // entity configuration …
}
```

```csharp
using Expresso.Rendering.EntityFrameworkCore;

var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
var query = db.Books
    .Where(transformer, filter, books)
    .OrderBy(transformer, sort, books)
    .IncludeSorted(transformer, sort, books);   // nested sortfor → filtered Include/ThenInclude
```

- Supported providers: SQL Server, PostgreSQL (Npgsql), MySQL / MariaDB (Pomelo or Oracle's `MySql.EntityFrameworkCore`), SQLite, Oracle and DB2 (IBM). Other providers get the plain Queryable lambdas.
- Overrides are placeholder methods in `ExpressoDbFunctions` that `HasExpressoFunctions` maps to provider SQL. They exist only where the provider's own translation differs from the SQL renderer. Each function page lists them.
- `IncludeSorted` needs each sorted collection mapped to a navigation property (`b => b.Authors`). Otherwise it throws `NotSupportedException`; order a child query with `OrderByNested` instead.
- DB2 cannot correlate sort keys in `ORDER BY` (`SQL0206N`). Use `EfCoreLiftedSort.OrderedKeys` to project keys, order the outer query, then load entities by id and restore order (see the EF Core sample repositories).
- DI: `services.AddEfCoreExpressionTransformations<AppDbContext>()` registers a scoped transformer for that context's provider.

## EF6

```csharp
using Expresso.Rendering.EntityFramework;

var transformer = new Ef6ExpressionToLinqTransformer(db);   // provider from the context's connection
var ids = db.Books.Where(transformer, filter, books).OrderBy(transformer, sort, books).ToList();
```

- The provider is resolved from the context's connection, or pass the provider invariant name (`new Ef6ExpressionToLinqTransformer("Npgsql")`). DI: `services.AddEf6ExpressionTransformations("System.Data.SqlClient")`.
- Supported providers: `System.Data.SqlClient` / `Microsoft.Data.SqlClient` (SQL Server), `Npgsql` (EntityFramework6.Npgsql), `MySql.Data.MySqlClient` (MySql.Data.EntityFramework, also for MariaDB), `System.Data.SQLite.EF6`, and `Oracle.ManagedDataAccess.Client`. There is no DB2 EF6 provider on NuGet.
- EF6 cannot register SQL translations, so overrides use canonical `DbFunctions` or the provider's built-in store functions.
- EF6 has no filtered include: order child collections with a separate child query and `OrderByNested`.
- System.Data.SQLite binds `Guid` as binary by default; add `BinaryGUID=False` to the connection string when the database stores GUIDs as text.

### EF6 provider limits

A function that a provider cannot render exactly throws `NotSupportedException` when the lambda is built, with the reason in the message. Expresso never approximates.

| Provider | Throws for |
|---|---|
| PostgreSQL | `round`, `sqrt` |
| MySQL / MariaDB | `addyears`, `addmonths` |
| SQLite | `addyears` … `addseconds`, `date`, `time` |
| Oracle | `addyears` … `addseconds`, `time`, `sqrt`, `power` with integer-to-double promotion |

Oracle `concat` fails in the database instead (ORA-12704: EF6's own NULL guard emits `N''` against `VARCHAR2`). Two silent differences are listed under [EF6 limits](semantics.md#ef6-limits). The function pages give the reasons per provider.
