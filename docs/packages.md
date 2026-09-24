# Packages

Expresso ships as independent NuGet packages. There is no rendering metapackage — reference only the dialect you execute against.

| Package | Contains | Depends on |
|---|---|---|
| `Expresso.Core` | Expression tree (IR), `FilterCriteria`, `SortDirective`, field catalogs | — |
| `Expresso.Parsing` | `IFilterParser`, `ISortDirectiveParser`, DI | `Expresso.Core` |
| `Expresso.Rendering.Common` | `IExpressionToQueryClauseTransformer`, `SqlQueryMapping`, walker base | `Expresso.Core` |
| `Expresso.Rendering.SqlServer` | SQL Server transformer + `AddSqlServerExpressionTransformations()` | Common |
| `Expresso.Rendering.PostgreSql` | PostgreSQL transformer + `AddPostgreSqlExpressionTransformations()` | Common |
| `Expresso.Rendering.Sqlite` | SQLite transformer + `AddSqliteExpressionTransformations()` | Common |
| `Expresso.Rendering.MySql` | MySQL / MariaDB transformer + `AddMySqlExpressionTransformations()` | Common |
| `Expresso.Rendering.Oracle` | Oracle transformer + `AddOracleExpressionTransformations()` | Common |
| `Expresso.Rendering.Db2` | IBM DB2 transformer + `AddDb2ExpressionTransformations()` | Common |

`Expresso.Core` is published on its own so parsers and renderers share one type identity for the expression tree.

Public rendering types live in namespace `Expresso.Rendering` (breaking in **0.9.0**; previously `Expresso.SqlServer`).

## `Expresso.Core`

Namespace: `Expresso.Core.CriteriaExpressions` (and `.Abstract`), `Expresso.Core.Filtering`, `Expresso.Core.Sorting`.

- Expression tree types — see [docs/functions/README.md](functions/README.md).
- `FilterCriteria`, `SortDirective` / `CollectionSort`.
- `IRequestFieldsInfoProvider` / `IRequestQueryModelProvider` — [docs/field-providers.md](field-providers.md).

## `Expresso.Parsing`

Namespace: `Expresso.Parsing`.

- `IFilterParser` / `FilterParser`, `ISortDirectiveParser` / `SortDirectiveParser`.
- `LiteralParseOptions`; DI: `AddRequestParametersParsers()`.

## Rendering

Each dialect package registers **one** `IExpressionToQueryClauseTransformer` implementation. Mapping types (`SqlQueryMapping`, `CollectionSqlMapping`) are in Common.

Typically referenced by the data-access layer. Identifier quotes and bind names: [docs/rendering.md](rendering.md). Per-function SQL (all dialects): [docs/functions/](functions/README.md).

Install example (SQL Server):

```powershell
dotnet add MyApp.DataAccess package Expresso.Rendering.SqlServer
```

```csharp
using Expresso.Rendering;
builder.Services.AddSqlServerExpressionTransformations();
```

Use `AddPostgreSqlExpressionTransformations`, `AddSqliteExpressionTransformations`, `AddMySqlExpressionTransformations`, `AddOracleExpressionTransformations`, or `AddDb2ExpressionTransformations` for other engines. MariaDB uses the **MySql** package.

## Database clients (not included)

Expresso **only** produces SQL text and parameter values. Your application must reference an **ADO.NET provider** for the database you execute against, and install any **native client** that provider requires on every machine that **runs** the app (developer workstations and servers). Expresso NuGet packages do not ship database drivers.

| Engine | Typical managed package | Notes |
|---|---|---|
| SQL Server | `Microsoft.Data.SqlClient` | |
| PostgreSQL | `Npgsql` | |
| MySQL / MariaDB | `MySqlConnector` (or Oracle’s connector) | |
| SQLite | `Microsoft.Data.Sqlite` (+ native bundle such as `SQLitePCLRaw.bundle_e_sqlite3` on some hosts) | |
| Oracle | `Oracle.ManagedDataAccess` (.NET Framework) or `Oracle.ManagedDataAccess.Core` (.NET 6+) | |
| **IBM DB2** | See below | Requires IBM **clidriver** (or full Data Server Client) on the host |

### DB2 and .NET Framework

`Expresso.Rendering.Db2` targets **`netstandard2.0`** — the **same** NuGet works on .NET Framework 4.6.1+ and on .NET 6+. You do **not** need a separate renderer package for .NET Framework.

What differs is the **IBM ADO.NET stack**, not Expresso:

| App TFM | IBM provider | Install |
|---|---|---|
| **.NET 6+** | NuGet [`Net.IBM.Data.Db2`](https://www.nuget.org/packages/Net.IBM.Data.Db2) | Also install IBM’s **clidriver** and ensure it is on `PATH` (or configure `DB2HOME` per IBM docs). The [net10 sample](../samples/Expresso.Sample.WebApi/README.md) uses this stack. |
| **.NET Framework 4.x** | **IBM Data Server Provider for .NET** (`IBM.Data.DB2.dll` from the [IBM Data Server Driver Package](https://www.ibm.com/docs/en/db2/12.1.x?topic=adonet-data-server-provider-net)) | `Net.IBM.Data.Db2` does **not** target .NET Framework. Use IBM’s Framework provider plus the same native client/driver install IBM documents for your platform. |

The [.NET Framework 4.8 sample host](../samples/Expresso.Sample.WebApi.NetFx/README.md) does **not** wire Db2 (it uses the other engines’ NuGet drivers only). You can still use `Expresso.Rendering.Db2` in your own net48 app with the IBM Framework provider.

Db2-specific SQL behavior (for example `ORDER BY` vs correlated collection aggregates) is in [docs/rendering.md](rendering.md).

## Target framework and supported types

- **Target frameworks:** `netstandard2.0` and `net6.0`.
- **Supported CLR types:** `string`, `bool`, `byte`, `int`, `double`, `DateTime`, `Guid`, `TimeSpan` (time-of-day) on all TFMs; `DateOnly` and `TimeOnly` on **net6.0**.
- **Not supported:** `float`, `decimal`.

See [docs/query-syntax.md](query-syntax.md).
