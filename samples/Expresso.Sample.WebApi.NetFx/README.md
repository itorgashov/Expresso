# Expresso.Sample.WebApi.NetFx

.NET Framework 4.8 sample using OWIN self-host and ASP.NET Web API 2. Shares data access and filtering logic with the modern sample via [Expresso.Sample.Shared](../Expresso.Sample.Shared).

For architecture and endpoint examples, see [docs/sample-app.md](../../docs/sample-app.md). Schema/seed: [samples/database](../database).

## Prerequisites

- .NET Framework 4.8 targeting pack (via Visual Studio or Build Tools)
- .NET SDK (to build with `dotnet build`)
- A database from [samples/database](../database) (`schema.sql` then `seed.sql`)

Db2 is **not** supported on this host; use [Expresso.Sample.WebApi](../Expresso.Sample.WebApi) (net10) to try Db2 against Docker or your own server. That is a **sample** limitation: `Expresso.Rendering.Db2` still works on .NET Framework 4.x if you add IBM’s **Data Server Provider for .NET** and client install (not `Net.IBM.Data.Db2`). See [docs/packages.md — DB2 and .NET Framework](../../docs/packages.md#db2-and-net-framework).

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
| Books | `GET /api/books?filter=&sort=&page=&pagesize=&skip=&take=` | `GET /api/books/{id}` |
| Authors | `GET /api/authors?filter=&sort=&page=&pagesize=&skip=&take=` | `GET /api/authors/{id}` |
| Publishers | `GET /api/publishers?filter=&sort=&page=&pagesize=&skip=&take=` | `GET /api/publishers/{id}` |

This host chooses `page` / `pagesize` for paged results and `skip` / `take` for offset/number results. `page` is 1-based and requires `pagesize`; `pagesize` alone selects page 1. Requests that combine the two models or supply `page` without `pagesize` return HTTP 400. A positive offset or a limit adds `X-Total-Count`, even for an empty result. Only page-based requests add `X-Total-Pages`. See the [sample contract](../../docs/sample-app.md#pagination-contract) and [library semantics](../../docs/pagination.md).

Example: `GET /api/publishers?filter=eq(opens,"09:00")` (time-of-day field mapped as `TimeSpan` on this host).
Collection filter: `GET /api/books?filter=any(authors,eq(displayname,"Leo Tolstoy"))`.
