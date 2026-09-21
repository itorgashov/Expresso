# Expresso.Sample.WebApi.NetFx

.NET Framework 4.8 sample using OWIN self-host and ASP.NET Web API 2. Shares data access and filtering logic with the modern sample via [Expresso.Sample.Shared](../Expresso.Sample.Shared).

For architecture and endpoint examples, see [docs/sample-app.md](../../docs/sample-app.md). Schema/seed: [samples/database](../database).

## Prerequisites

- .NET Framework 4.8 targeting pack (via Visual Studio or Build Tools)
- .NET SDK (to build with `dotnet build`)
- A database from [samples/database](../database) (`schema.sql` then `seed.sql`)

Db2 is **not** supported on this host; use [Expresso.Sample.WebApi](../Expresso.Sample.WebApi) (net10).

## Engine and connection strings

Switch engine only in `appsettings.json` (`ExpressoSample:Engine`, default `SqlServer`). Connection strings live in **user secrets** under `ConnectionStrings:{Engine}` (`SqlServer`, `PostgreSql`, `MySql`, `Sqlite`, `Oracle`). This host shares `UserSecretsId` with [Expresso.Sample.WebApi](../Expresso.Sample.WebApi); set secrets once there (including optional `Db2` for the net10 host).

Allowed engines: `SqlServer`, `PostgreSql`, `MySql` / `MariaDb`, `Sqlite`, `Oracle`. SQLite on net48 needs the bundled `e_sqlite3` native library (the project sets `RuntimeIdentifier` `win-x64`).

## Run

```powershell
dotnet run --project samples/Expresso.Sample.WebApi.NetFx
```

Listens on `http://localhost:5080/`. Open Swagger UI at `/swagger`.

## Endpoints

Same as the ASP.NET Core sample:

| Resource | GET all | GET by id |
|---|---|---|
| Books | `GET /api/books?filter=&sort=` | `GET /api/books/{id}` |
| Authors | `GET /api/authors?filter=&sort=` | `GET /api/authors/{id}` |
| Publishers | `GET /api/publishers?filter=&sort=` | `GET /api/publishers/{id}` |

Example: `GET /api/publishers?filter=eq(opens,"09:00")` (time-of-day field mapped as `TimeSpan` on this host).
Collection filter: `GET /api/books?filter=any(authors,eq(displayname,"Leo Tolstoy"))`.
