# Expresso

[GitHub repository](https://github.com/itorgashov/Expresso)

Expresso is a .NET library for dynamic filtering and sorting. It parses a function-call query string into a validated expression tree, then renders the tree as parameterized SQL or as LINQ. It replaces hand-written `WHERE` and `ORDER BY` string concatenation, and piles of optional `Where` clauses, with one tested pipeline.

```text
Query string → Expresso.Parsing → expression tree (Expresso.Core) ─┬→ SQL renderer (one per dialect) → SQL + parameters
                                                                   └→ LINQ renderer (Linq, EF Core, EF6) → predicate + sort keys
```

**Target frameworks:** `netstandard2.0` and `net6.0` for the core, parsing, SQL and LINQ packages (usable from .NET Framework 4.6.1+, .NET Standard 2.0 libraries and .NET 6+). The EF Core package targets `net8.0`, and the EF6 package targets `net48`.
**License:** MIT.

## Example

A client sends a filter and a sort:

```text
gt(createdAt,"2021-01-01")
createdAt,desc,name,asc
```

With a field map that sends `createdAt` to `p.created_at` and the parameter prefix `wparam`, each SQL renderer produces the same condition in its own dialect:

| Dialect | Rendered filter |
|---|---|
| SQL Server | `([p].[created_at] > @wparam_0)` |
| PostgreSQL | `("p"."created_at" > @wparam_0)` |
| MySQL, MariaDB | `` (`p`.`created_at` > @wparam_0) `` |
| Oracle | `("p"."created_at" > :wparam_0)` |

The LINQ renderers produce a lambda instead, which EF Core or EF6 translates to SQL:

```csharp
e => e.CreatedAt > p0
```

The grammar, literal rules and supported types are in [Query syntax](docs/query-syntax.md).

## Packages

| Package | Role |
|---|---|
| `Expresso.Core` | Expression tree, filter, sort, and paging models, field-catalog contract |
| `Expresso.Parsing` | Filter, sort, and paging text parsers and dependency injection |
| `Expresso.Rendering.Common` | Shared SQL rendering contract |
| `Expresso.Rendering.SqlServer`, `PostgreSql`, `Sqlite`, `MySql`, `Oracle`, `Db2` | Filter, sort, and paging clause rendering for one dialect |
| `Expresso.Rendering.Linq` | LINQ filtering, sorting, and result limits for `IQueryable<T>` and in-memory collections |
| `Expresso.Rendering.EntityFrameworkCore` | EF Core 8+ overrides, `IncludeSorted`, and provider-aware paging |
| `Expresso.Rendering.EntityFramework` | EF6 overrides and provider-aware paging |

There is no metapackage: reference the packages you need. Database drivers and native clients are not included (DB2 needs IBM's clidriver on the host). See [Packages](docs/packages.md).

## Documentation

New to Expresso? Start with the [overview](docs/overview.md), then [Get started](docs/getting-started.md).

- [Overview](docs/overview.md): what Expresso is for, use cases, and when not to use it
- [Get started](docs/getting-started.md): install, register, describe fields, parse
  - [Render to SQL](docs/getting-started-sql.md) for ADO.NET and Dapper
  - [Render to LINQ and EF](docs/getting-started-linq.md) for EF Core, EF6 and in-memory collections
- [Packages](docs/packages.md): each NuGet package and which ones you need
- [Query syntax](docs/query-syntax.md): filter and sort grammar, literals, supported types
- [Pagination](docs/pagination.md): paged and offset/number results, ordering, and totals
- [Field providers](docs/field-providers.md): the field allow-list and query model
- [SQL rendering](docs/rendering.md): quoting and parameters per dialect
- [LINQ rendering](docs/linq-rendering.md): profiles, EF Core, EF6 and provider limits
- [Filter behavior and database differences](docs/semantics.md): NULL handling, types and engine differences
- [Error handling](docs/error-handling.md): exceptions from parsing and rendering
- [Function reference](docs/functions/README.md): one page per function, grouped by category
- [Sample app](docs/sample-app.md): a walkthrough of the sample Web API

## Samples

- [samples/Expresso.Sample.WebApi](samples/Expresso.Sample.WebApi): .NET 10 ASP.NET Core host with Swagger (ADO.NET)
- [samples/Expresso.Sample.WebApi.NetFx](samples/Expresso.Sample.WebApi.NetFx): .NET Framework 4.8 OWIN and Web API 2 host (ADO.NET)
- [samples/Expresso.Sample.WebApi.EfCore](samples/Expresso.Sample.WebApi.EfCore): .NET 10 ASP.NET Core host (EF Core 8 + LINQ renderers)
- [samples/Expresso.Sample.WebApi.NetFx.Ef6](samples/Expresso.Sample.WebApi.NetFx.Ef6): .NET Framework 4.8 OWIN host (EF6 + LINQ renderers)

The ADO pair shares [samples/Expresso.Sample.Shared](samples/Expresso.Sample.Shared). The EF pair is standalone (same routes and `ExpressoSample:Engine`, no Shared reference). Schema and seed: [samples/database](samples/database). See [Sample app](docs/sample-app.md).

## Build from source

```powershell
dotnet test .\Expresso.slnx -c Release -f net6.0 --filter "Category!=Integration"
dotnet test .\Expresso.slnx -c Release -f net48 --filter "Category!=Integration"    # Windows; validates .NET Framework consumers
dotnet pack .\Expresso.slnx -c Release -o .\artifacts
```

Tests that need a running database engine skip unless `EXPRESSO_IT=1`. See [test/Rendering/Expresso.Rendering.Integration.Test/README.md](test/Rendering/Expresso.Rendering.Integration.Test/README.md).
