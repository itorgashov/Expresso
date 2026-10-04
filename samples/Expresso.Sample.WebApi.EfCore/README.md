# Expresso.Sample.WebApi.EfCore

ASP.NET Core sample that filters and sorts with **EF Core** and Expresso (`Where`, `OrderBy`, `IncludeSorted`). It uses the same `Expresso_Sample` database and the same `ExpressoSample:Engine` switch as the ADO samples.

No reference to `Expresso.Sample.Shared`. Walkthrough: [docs/getting-started-linq.md](../../docs/getting-started-linq.md). Schema: [samples/database](../database).

## Prerequisites

- .NET 10 SDK
- Database created from [samples/database](../database)
- User secrets already set for [Expresso.Sample.WebApi](../Expresso.Sample.WebApi) (same `UserSecretsId`)

## Engine

Set `ExpressoSample:Engine` in `appsettings.json` and put the matching connection string in user secrets under `ConnectionStrings:{Engine}`. Supported: `SqlServer`, `PostgreSql`, `MySql`, `MariaDb`, `Sqlite`, `Oracle`, `Db2`.

## Run

```powershell
dotnet run --project samples/Expresso.Sample.WebApi.EfCore
```

Swagger: `/swagger`. Endpoints match the ADO sample (`GET /api/books`, `/api/authors`, `/api/publishers` with `filter` and `sort`).
