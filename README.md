# Expresso

Expresso is a small .NET library for **dynamic filtering and sorting**. A function-call query string is parsed into an expression tree, then rendered to parameterized SQL (SQL Server in v1).

```text
Query string → Expresso.Parsing → expression tree (Expresso.Core) → Expresso.Rendering.SqlServer → SQL + parameters
```

**Target framework:** .NET 6.0 (usable from .NET 6, 7, 8, and 10).  
**License:** MIT.

## Packages

| Package | Role |
|---|---|
| `Expresso.Core` | Expression tree, filter/sort models |
| `Expresso.Parsing` | Query-string parsers + DI |
| `Expresso.Rendering.SqlServer` | SQL Server `WHERE` / `ORDER BY` + DI |

There is no metapackage in v1. Reference the packages you need. `Expresso.Core` is published on its own so parsers and renderers share one type identity, and so apps can depend on the tree without a SQL renderer.

## Example

Filter (boolean expression required):

```text
gt(createdAt,"2021-01-01")
```

With a field-to-column map `createdAt` → `p.created_at` and parameter prefix `wparam`, SQL Server rendering produces:

```sql
([p].[created_at] > @wparam_0)
```

`@wparam_0` is a `DateTime` parameter (typically midnight on that date). Date literals must be **double-quoted**. Parsing uses `DateTime.TryParse` (current culture); prefer ISO `yyyy-MM-dd`.

Sort:

```text
createdAt,desc,name,asc
```

## Supported functions (v1)

Names are case-insensitive.

| Category | Functions |
|---|---|
| Logical | `and`, `or`, `not` |
| Comparison | `eq`, `neq`, `gt`, `gte`, `lt`, `lte` |
| Membership / null | `in`, `isnull` |
| Arithmetic | `abs`, `add`, `sub`, `mult`, `div` |
| String | `startswith`, `substring` |

## Supported types (v1)

`string`, `bool`, `byte`, `int`, `double`, `DateTime`.

**Not in v1:** `DateOnly`, `TimeOnly`, `Guid`, `float`, `decimal`. Date filters use `DateTime` fields and quoted date/time literals.

## Field catalogs

The library does not ship an application field list. Implement `IRequestFieldsInfoProvider` (or pass `(name, Type)[]` into the parsers) in the consuming app.

## Sample

[samples/Expresso.Sample.WebApi](samples/Expresso.Sample.WebApi) — .NET 10 Web API with ADO.NET repositories, Swagger, and filter/sort on books/authors/publishers. See the sample [README](samples/Expresso.Sample.WebApi/README.md).

## Build

```powershell
dotnet test .\Expresso.slnx -c Release
dotnet pack .\Expresso.slnx -c Release -o .\artifacts
```
