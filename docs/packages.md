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

## Target framework and supported types

- **Target frameworks:** `netstandard2.0` and `net6.0`.
- **Supported CLR types:** `string`, `bool`, `byte`, `int`, `double`, `DateTime`, `Guid`, `TimeSpan` (time-of-day) on all TFMs; `DateOnly` and `TimeOnly` on **net6.0**.
- **Not supported:** `float`, `decimal`.

See [docs/query-syntax.md](query-syntax.md).
