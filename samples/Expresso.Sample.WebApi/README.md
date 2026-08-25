# Expresso.Sample.WebApi

Sample ASP.NET Core Web API demonstrating [Expresso](https://github.com/itorgashov/Expresso) filter/sort query strings rendered to parameterized SQL Server queries.

## Prerequisites

- .NET 10 SDK
- SQL Server with database **Expresso_Sample** (see [database/schema.sql](database/schema.sql))
- NuGet packages `Expresso.Core`, `Expresso.Parsing`, `Expresso.Rendering.SqlServer` **0.1.1**

## Connection string

The key is defined in `appsettings.json`; the value comes from **user secrets**:

```powershell
dotnet user-secrets set "ConnectionStrings:ExpressoSample" "Server=YOUR_SERVER;Database=Expresso_Sample;Trusted_Connection=True;TrustServerCertificate=True" --project samples/Expresso.Sample.WebApi
```

## Run

```powershell
dotnet run --project samples/Expresso.Sample.WebApi
```

Open Swagger UI at `/swagger`.

## Endpoints

| Controller | GET all | GET by id |
|---|---|---|
| Books | `GET /api/books?filter=&sort=` | `GET /api/books/{id}` |
| Authors | `GET /api/authors?filter=&sort=` | `GET /api/authors/{id}` |
| Publishers | `GET /api/publishers?filter=&sort=` | `GET /api/publishers/{id}` |

## Example queries

**Books**

- `GET /api/books?filter=gt(year,2000)&sort=rating,desc,title,asc`
- `GET /api/books?filter=startswith(publisher,"North")`
- `GET /api/books?filter=contains(title,"War")`
- `GET /api/books?filter=gte(createdat,"2020-01-01")`

**Authors**

- `GET /api/authors?filter=eq(firstname,"George")&sort=lastname,asc`

## Architecture

- **Presentation:** controllers parse `filter` / `sort`, map models to view models in-place.
- **Data access:** ADO.NET repositories implement `IRepository<T>`, use Expresso SQL renderer for `WHERE` / `ORDER BY`.

Filter/sort field catalogs are defined in `Filtering/RequestFieldsInfoProvider.cs`.
