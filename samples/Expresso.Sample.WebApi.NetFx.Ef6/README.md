# Expresso.Sample.WebApi.NetFx.Ef6

.NET Framework 4.8 OWIN sample that filters and sorts with **Entity Framework 6** and Expresso (`Where`, `OrderBy`, `OrderByNested` for nested `sortfor`). Same database and `ExpressoSample:Engine` switch as the ADO samples.

No reference to `Expresso.Sample.Shared`. Walkthrough: [docs/getting-started-linq.md](../../docs/getting-started-linq.md).

## Prerequisites

- .NET Framework 4.8 on Windows
- Database from [samples/database](../database)
- User secrets shared with [Expresso.Sample.WebApi](../Expresso.Sample.WebApi) (`UserSecretsId` below)

## Engine

Set `ExpressoSample:Engine` and `ConnectionStrings:{Engine}` (user secrets). Supported: `SqlServer`, `PostgreSql`, `MySql`, `MariaDb`, `Sqlite`, `Oracle`.

**Db2 is not supported** on this host (no EF6 provider). Startup fails with a clear message if `Engine` is `Db2`; use [Expresso.Sample.WebApi.EfCore](../Expresso.Sample.WebApi.EfCore) instead.

On **Oracle** and **SQLite**, `opens` and `closes` are not filter or sort fields (EF6 has no time-of-day store type for those columns). List and single-publisher responses still return the stored times: SQLite reads the text columns, and Oracle reads `INTERVAL` columns with a quoted `EXTRACT` query.

## Run

```powershell
dotnet run --project samples/Expresso.Sample.WebApi.NetFx.Ef6
```

Default URL: `http://localhost:5081/` (Swagger at `/swagger`).
