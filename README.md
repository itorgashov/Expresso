# Expresso

[GitHub repository](https://github.com/itorgashov/Expresso)

Expresso is a small .NET library for **dynamic filtering and sorting**. A function-call query string is parsed into a validated expression tree, then rendered to parameterized SQL (SQL Server in v1) — a safer alternative to hand-rolling dynamic `WHERE` / `ORDER BY` string concatenation.

```text
Query string → Expresso.Parsing → expression tree (Expresso.Core) → Expresso.Rendering.SqlServer → SQL + parameters
```

**Target frameworks:** `netstandard2.0` and `net6.0` (usable from .NET Framework 4.6.1+, .NET Standard 2.0 libraries, and .NET 6+).
**License:** MIT.

## Packages

| Package | Role |
|---|---|
| `Expresso.Core` | Expression tree, filter/sort models, field-catalog contract |
| `Expresso.Parsing` | Query-string parsers + DI |
| `Expresso.Rendering.SqlServer` | SQL Server `WHERE` / `ORDER BY` rendering + DI |

There is no metapackage in v1 — reference the packages you need. See [docs/packages.md](docs/packages.md) for what each package contains and which application layer typically references it.

## Example

Filter (boolean expression required):

```text
gt(createdAt,"2021-01-01")
```

With a field-to-column map `createdAt` → `p.created_at` and parameter prefix `wparam`, SQL Server rendering produces:

```sql
([p].[created_at] > @wparam_0)
```

Sort:

```text
createdAt,desc,name,asc
```

Full grammar, literal/quoting rules, and supported types: [docs/query-syntax.md](docs/query-syntax.md).

## Documentation

- [docs/overview.md](docs/overview.md) — what Expresso is for, use cases, and when not to use it
- [docs/packages.md](docs/packages.md) — purpose of each of the 3 NuGet packages
- [docs/getting-started.md](docs/getting-started.md) — step-by-step: install, register, implement a field provider, parse, render, execute
- [docs/query-syntax.md](docs/query-syntax.md) — filter/sort grammar, literals, supported types
- [docs/field-providers.md](docs/field-providers.md) — `IRequestFieldsInfoProvider` explained
- [docs/error-handling.md](docs/error-handling.md) — exceptions thrown by parsing and rendering
- [docs/functions/README.md](docs/functions/README.md) — full function reference, one page per function, grouped by category
- [docs/sample-app.md](docs/sample-app.md) — walkthrough of the sample Web API

## Sample

- [samples/Expresso.Sample.WebApi](samples/Expresso.Sample.WebApi) — .NET 10 ASP.NET Core host with Swagger
- [samples/Expresso.Sample.WebApi.NetFx](samples/Expresso.Sample.WebApi.NetFx) — .NET Framework 4.8 OWIN + Web API 2 host

Both share [samples/Expresso.Sample.Shared](samples/Expresso.Sample.Shared) (models, ADO.NET repositories, field catalogs). See [docs/sample-app.md](docs/sample-app.md) for a guided walkthrough.

## Build

```powershell
dotnet test .\Expresso.slnx -c Release -f net6.0
dotnet test .\Expresso.slnx -c Release -f net48    # Windows; validates .NET Framework consumers
dotnet pack .\Expresso.slnx -c Release -o .\artifacts
```
