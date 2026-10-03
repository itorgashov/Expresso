# Packages

Expresso ships as independent NuGet packages. There is no metapackage: reference only the parsing package, and the dialect or LINQ package you run against. This page lists the packages and shows which ones each kind of application needs.

| Package | Contains | Depends on |
|---|---|---|
| `Expresso.Core` | Expression tree, `FilterCriteria`, `SortDirective`, field catalogs | — |
| `Expresso.Parsing` | `IFilterParser`, `ISortDirectiveParser`, DI | `Expresso.Core` |
| `Expresso.Rendering.Common` | `IExpressionToQueryClauseTransformer`, `SqlQueryMapping`, shared SQL rendering base | `Expresso.Core` |
| `Expresso.Rendering.SqlServer` | SQL Server transformer + `AddSqlServerExpressionTransformations()` | Common |
| `Expresso.Rendering.PostgreSql` | PostgreSQL transformer + `AddPostgreSqlExpressionTransformations()` | Common |
| `Expresso.Rendering.Sqlite` | SQLite transformer + `AddSqliteExpressionTransformations()` | Common |
| `Expresso.Rendering.MySql` | MySQL / MariaDB transformer + `AddMySqlExpressionTransformations()` | Common |
| `Expresso.Rendering.Oracle` | Oracle transformer + `AddOracleExpressionTransformations()` | Common |
| `Expresso.Rendering.Db2` | IBM DB2 transformer + `AddDb2ExpressionTransformations()` | Common |
| `Expresso.Rendering.Linq` | LINQ lambdas (Queryable and in-memory profiles), `LinqQueryMapping<T>`, `AddLinqExpressionTransformations()` | `Expresso.Core` |
| `Expresso.Rendering.EntityFrameworkCore` | EF Core 8+ transformer and provider translations, `IncludeSorted`, `AddEfCoreExpressionTransformations<TContext>()` (net8.0) | Linq, `Microsoft.EntityFrameworkCore.Relational` |
| `Expresso.Rendering.EntityFramework` | EF6 transformer with canonical and store functions, `AddEf6ExpressionTransformations()` (net48) | Linq, `EntityFramework` |
## Which packages do I need?

| Your application | Install |
|---|---|
| Web API that reads `filter` and `sort` and queries with ADO.NET or Dapper | `Expresso.Parsing` and one dialect package, such as `Expresso.Rendering.PostgreSql` |
| Web API on EF Core 8 or later | `Expresso.Parsing` and `Expresso.Rendering.EntityFrameworkCore` |
| Web API on EF6 (.NET Framework) | `Expresso.Parsing` and `Expresso.Rendering.EntityFramework` |
| Filtering lists in memory, or another LINQ provider | `Expresso.Parsing` and `Expresso.Rendering.Linq` |
| Separate API and data-access projects | `Expresso.Core` and `Expresso.Parsing` in the API project; `Expresso.Core` and your renderer in the data-access project |

Renderer packages bring `Expresso.Core` and the packages they depend on, so you do not install those separately.
`Expresso.Core` is published on its own so parsers and renderers share one type identity for the expression tree.

The public rendering types live in the `Expresso.Rendering` namespace. Before version 0.9.0 they lived in `Expresso.SqlServer`.

## `Expresso.Core`

Namespace: `Expresso.Core.CriteriaExpressions` (and `.Abstract`), `Expresso.Core.Filtering`, `Expresso.Core.Sorting`.

- Expression tree types: see the [function reference](functions/README.md).
- `FilterCriteria`, `SortDirective` / `CollectionSort`.
- `IRequestFieldsInfoProvider` / `IRequestQueryModelProvider`: see [Field providers](field-providers.md).

## `Expresso.Parsing`

Namespace: `Expresso.Parsing`.

- `IFilterParser` / `FilterParser`, `ISortDirectiveParser` / `SortDirectiveParser`.
- `LiteralParseOptions`; DI: `AddRequestParametersParsers()`.

## Rendering

Each dialect package registers one `IExpressionToQueryClauseTransformer` implementation. Mapping types (`SqlQueryMapping`, `CollectionSqlMapping`) are in Common.

Reference it from the data-access layer. [SQL rendering](rendering.md) lists identifier quotes and parameter names, and the [function reference](functions/README.md) shows the SQL for every dialect.

To install a dialect package, for example SQL Server:

```powershell
dotnet add MyApp.DataAccess package Expresso.Rendering.SqlServer
```

```csharp
using Expresso.Rendering;
builder.Services.AddSqlServerExpressionTransformations();
```

Use `AddPostgreSqlExpressionTransformations`, `AddSqliteExpressionTransformations`, `AddMySqlExpressionTransformations`, `AddOracleExpressionTransformations`, or `AddDb2ExpressionTransformations` for other engines. MariaDB uses the MySql package.

## LINQ, EF Core and EF6

`Expresso.Rendering.Linq` renders lambdas instead of SQL text; the EF packages add provider overrides on top of it. To set it up, see [Render to LINQ and EF](getting-started-linq.md). Mapping and provider limits are in [LINQ rendering](linq-rendering.md).

## Database clients (not included)

Expresso produces only SQL text and parameter values. Your application must reference an ADO.NET provider for the database you run against, and install any native client that the provider requires on every machine that runs the application, including developer workstations and servers. Expresso packages do not ship database drivers.

| Engine | Typical managed package | Notes |
|---|---|---|
| SQL Server | `Microsoft.Data.SqlClient` | |
| PostgreSQL | `Npgsql` | |
| MySQL / MariaDB | `MySqlConnector` (or Oracle’s connector) | |
| SQLite | `Microsoft.Data.Sqlite` (+ native bundle such as `SQLitePCLRaw.bundle_e_sqlite3` on some hosts) | |
| Oracle | `Oracle.ManagedDataAccess` (.NET Framework) or `Oracle.ManagedDataAccess.Core` (.NET 6+) | |
| IBM DB2 | See below | Requires IBM's clidriver (or the full Data Server Client) on the host |

### DB2 and .NET Framework

`Expresso.Rendering.Db2` targets `netstandard2.0`, so the same NuGet package works on .NET Framework 4.6.1+ and on .NET 6+. You do not need a separate renderer package for .NET Framework.

The IBM ADO.NET stack differs, not Expresso:

| App TFM | IBM provider | Install |
|---|---|---|
| .NET 6+ | NuGet [`Net.IBM.Data.Db2`](https://www.nuget.org/packages/Net.IBM.Data.Db2) | Also install IBM’s clidriver and ensure it is on `PATH` (or configure `DB2HOME` per IBM docs). The [net10 sample](../samples/Expresso.Sample.WebApi/README.md) uses this stack. |
| .NET Framework 4.x | IBM Data Server Provider for .NET (`IBM.Data.DB2.dll` from the [IBM Data Server Driver Package](https://www.ibm.com/docs/en/db2/12.1.x?topic=adonet-data-server-provider-net)) | `Net.IBM.Data.Db2` does not target .NET Framework. Use IBM’s Framework provider plus the same native client/driver install IBM documents for your platform. |

The [.NET Framework 4.8 sample host](../samples/Expresso.Sample.WebApi.NetFx/README.md) does not wire Db2 (it uses the other engines’ NuGet drivers only). You can still use `Expresso.Rendering.Db2` in your own net48 app with the IBM Framework provider.

For a DB2 limitation on sorting by a collection aggregate, see [SQL rendering](rendering.md#collection-mapping).

## Target framework and supported types

- Target frameworks: `netstandard2.0` and `net6.0`; `Expresso.Rendering.EntityFrameworkCore` targets `net8.0` and `Expresso.Rendering.EntityFramework` targets `net48`.
- Supported CLR types: `string`, `bool`, `byte`, `int`, `double`, `DateTime`, `Guid`, `TimeSpan` (time-of-day) on all TFMs; `DateOnly` and `TimeOnly` on net6.0.
- Not supported: `float`, `decimal`.

See [Query syntax](query-syntax.md).
