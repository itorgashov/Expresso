# Expresso.Sample.WebApi

Sample ASP.NET Core Web API demonstrating [Expresso](https://github.com/itorgashov/Expresso) filter/sort query strings rendered to parameterized SQL for SQL Server, PostgreSQL, MySQL/MariaDB, SQLite, Oracle, or Db2.

Shared models, repositories, and filtering live in [Expresso.Sample.Shared](../Expresso.Sample.Shared). A .NET Framework 4.8 counterpart is [Expresso.Sample.WebApi.NetFx](../Expresso.Sample.WebApi.NetFx) (no Db2).

For a guided walkthrough, see [docs/sample-app.md](../../docs/sample-app.md). Schema/seed: [samples/database](../database). Plan: [SAMPLESEEDPLAN.md](../../SAMPLESEEDPLAN.md).

## Prerequisites

- .NET 10 SDK
- A database created from [samples/database](../database) (`schema.sql` then `seed.sql` for the engine you choose)

## Engine and connection strings

Switch engine only in `appsettings.json` (`ExpressoSample:Engine`, default `SqlServer`). Store **all** connection strings in **user secrets** (never commit them). The host loads `ConnectionStrings:{Engine}` for the selected engine (`MariaDb` uses `MySql`). Both sample hosts share the same `UserSecretsId`, so one secrets file serves net10 and net48.

```powershell
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=YOUR_SERVER;Database=Expresso_Sample;Trusted_Connection=True;TrustServerCertificate=True" --project samples/Expresso.Sample.WebApi
dotnet user-secrets set "ConnectionStrings:PostgreSql" "Host=localhost;Database=Expresso_Sample;Username=...;Password=..." --project samples/Expresso.Sample.WebApi
dotnet user-secrets set "ConnectionStrings:MySql" "Server=localhost;Database=Expresso_Sample;User ID=...;Password=..." --project samples/Expresso.Sample.WebApi
dotnet user-secrets set "ConnectionStrings:Sqlite" "Data Source=C:\temp\expresso-sample.db" --project samples/Expresso.Sample.WebApi
dotnet user-secrets set "ConnectionStrings:Oracle" "User Id=...;Password=...;Data Source=localhost:1521/XEPDB1" --project samples/Expresso.Sample.WebApi
dotnet user-secrets set "ConnectionStrings:Db2" "Server=localhost:50000;Database=SAMPLE;UserID=...;Password=..." --project samples/Expresso.Sample.WebApi
```

Allowed engines: `SqlServer`, `PostgreSql`, `MySql` (also `MariaDb`), `Sqlite`, `Oracle`, `Db2`. Oracle uses `:` binds. **Db2:** NuGet `Net.IBM.Data.Db2` plus IBM **clidriver** on `PATH` on every machine that runs this app (Expresso only renders SQL — see [docs/packages.md](../../docs/packages.md#database-clients-not-included)). The net48 sample host does not wire Db2.

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
- `GET /api/books?filter=any(authors,eq(displayname,"Leo Tolstoy"))`
- `GET /api/books?filter=eq(count(authors),2)`

**Authors**

- `GET /api/authors?filter=eq(firstname,"George")&sort=lastname,asc`

**Publishers**

- `GET /api/publishers?filter=eq(opens,"09:00")` — time-of-day field (`TimeOnly` on this host / SQL `TIME`)

Db2 cannot `ORDER BY` a correlated collection aggregate such as `count(authors)`. Nested `sortfor` and collection filters still work.

## Architecture

- **This project:** ASP.NET Core host, Swagger, `SampleEngineSetup` (transformer + `ISampleDb`), thin controllers.
- **Expresso.Sample.Shared:** ADO.NET repositories, `ISampleSql` dialect catalog, view models, and query-parameter parsing.
