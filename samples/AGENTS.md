# Samples — agent guide

Four Web API hosts on the same `Expresso_Sample` database. Walkthrough: [docs/sample-app.md](../docs/sample-app.md).

| Host | Stack | TFM | Notes |
|---|---|---|---|
| `Expresso.Sample.WebApi` | ASP.NET Core, ADO.NET | net10 | uses `Expresso.Sample.Shared` |
| `Expresso.Sample.WebApi.NetFx` | OWIN, ADO.NET | net48 | uses Shared; no Db2; port 5080 |
| `Expresso.Sample.WebApi.EfCore` | ASP.NET Core, EF Core 8 | net10 | standalone; `IncludeSorted` |
| `Expresso.Sample.WebApi.NetFx.Ef6` | OWIN, EF6 | net48 | standalone; `OrderByNested`; Db2 refused; port 5081 |

## Rules

- Keep routes, `filter` / `sort` behavior, status codes and JSON shape the same on all four hosts. A behavior change in one host is applied to the others or called out.
- The EF hosts do not reference `Expresso.Sample.Shared`; they keep their own field catalog, `QueryParametersParser` and engine parser. Do not "deduplicate" them into Shared.
- Every host's field catalog matches its TFM: `DateOnly` / `TimeOnly` on net10, `DateTime` / `TimeSpan` on net48.
- Engine selection is `ExpressoSample:Engine` plus `ConnectionStrings:{Engine}`. Committed `appsettings.json` keeps empty connection strings; real values live in user secrets (same `UserSecretsId` on all hosts).
- EF provider package versions match `test/Rendering/Expresso.Rendering.Integration.Test` so the samples run what the tests verify.

## Database

- `database/*/schema.sql` drops and recreates the database. Never run it, and never add seeding or migrations to a host. Give the user the command instead.
- Smoke tests are read-only `GET` requests against a database the user already set up.
- Stop any host you start before you finish; a running host locks build outputs (MSB3027 in Visual Studio).
