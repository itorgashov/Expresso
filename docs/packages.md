# Packages

Expresso ships as three independent NuGet packages. There is no metapackage in v1 — reference only what a given project needs.

| Package | Contains | Depends on |
|---|---|---|
| `Expresso.Core` | Expression tree (IR), `FilterCriteria`, `SortDirective`, `IRequestFieldsInfoProvider` | — |
| `Expresso.Parsing` | `IFilterParser`, `ISortDirectiveParser`, DI registration | `Expresso.Core` |
| `Expresso.Rendering.SqlServer` | `IExpressionToQueryClauseTransformer`, DI registration | `Expresso.Core` |

`Expresso.Core` is published on its own — separate from both parsing and rendering — so that parsers and renderers always share one type identity for the expression tree, and so an application can depend on the tree types (e.g. to pass `FilterCriteria` between layers) without pulling in a SQL renderer it doesn't need.

## `Expresso.Core`

Namespace: `Expresso.Core.CriteriaExpressions` (and `.Abstract`), `Expresso.Core.Filtering`, `Expresso.Core.Sorting`.

- The expression tree base types: `AbstractExpression`, `AbstractFunction`, and the function base classes (`BooleanFunction`, `StringFunction`, `ComparisonFunction`, etc.) — see [docs/functions/README.md](functions/README.md) for every concrete function.
- `FilterCriteria` — wraps the parsed boolean root expression.
- `SortDirective` / `SortDirectiveItem` / `SortDirection` — the parsed sort list.
- `IRequestFieldsInfoProvider` — the contract your application implements to declare which fields are filterable/sortable. See [docs/field-providers.md](field-providers.md).

Has no dependencies of its own. Reference this package alone if you only need the tree types (for example, a layer that receives an already-parsed `FilterCriteria` and just needs the type).

## `Expresso.Parsing`

Namespace: `Expresso.Parsing`.

- `IFilterParser` / `FilterParser` — turns a filter query string into a `FilterCriteria` (throws if the parsed root is not a boolean expression).
- `ISortDirectiveParser` / `SortDirectiveParser` — turns a sort query string into a `SortDirective`.
- DI registration: `services.AddRequestParametersParsers()` registers both as singletons.
- The tokenizer/recursive-descent parser (`ExpressionParser`) is internal — not part of the public API.

Typically referenced by whichever layer reads incoming query parameters (usually the **presentation/API layer** — a controller, minimal API handler, or an application-service method that accepts raw query strings).

## `Expresso.Rendering.SqlServer`

Namespace: `Expresso.SqlServer` (note: differs from the package/folder name).

- `IExpressionToQueryClauseTransformer` / `ExpressionToSqlServerQueryClauseTransformer` — renders a `FilterCriteria` to a parameterized `WHERE` fragment and a `SortDirective` to a parameterized `ORDER BY` fragment, given a `fieldToColumnMap` and a parameter-name prefix.
- DI registration: `services.AddExpressionTransformations()` registers the transformer as a singleton.

Typically referenced by the **data-access layer** (ADO.NET/Dapper repository) that builds and executes the final SQL.

## Target framework and supported types

- **Target frameworks:** `netstandard2.0` and `net6.0`. NuGet packages contain both assemblies under `lib/netstandard2.0` and `lib/net6.0`.
- **Typical consumers:** .NET Framework 4.6.1+ (via `netstandard2.0`), .NET Standard 2.0 libraries, and .NET 6+ (prefer `net6.0` when your app targets .NET 6 or later).
- **Supported CLR types:** `string`, `bool`, `byte`, `int`, `double`, `DateTime`, `Guid` on all TFMs; `DateOnly` and `TimeOnly` when referencing the **net6.0** assembly.
- **Not supported:** `float`, `decimal`.

See [docs/query-syntax.md](query-syntax.md) for literal syntax and quoting rules for each type.
