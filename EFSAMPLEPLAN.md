# EF Core and EF6 sample hosts

Two standalone Web APIs demonstrate the LINQ renderers against `samples/database` schemas and seed. They do not reference `Expresso.Sample.Shared`. ADO samples are unchanged.

## Hosts

- **Expresso.Sample.WebApi.EfCore** (`net10`): `Expresso.Rendering.EntityFrameworkCore`, EF Core **8.0.11** provider packages (aligned with integration tests).
- **Expresso.Sample.WebApi.NetFx.Ef6** (`net48`): OWIN + Web API 2, `Expresso.Rendering.EntityFramework`, EF6 providers; Db2 refused at startup.

Shared user secrets id: `expresso-sample-webapi-7f3a9c2e-4b1d-4e8a-9f6c-2d5e8a1b4c7f`.

## Configuration

`ExpressoSample:Engine` and `ConnectionStrings:{Engine}` match the ADO hosts. Connection-string tweaks in code: Pomelo `AllowUserVariables=True`; DB2 `EnableEFCaseSensitivity=true`; EF6 SQLite `BinaryGUID=False`; EF6 MySQL `SslMode=None` → `Disabled`.

## API

Same routes as ADO: `GET /api/books|authors|publishers` (+ id), `filter` and `sort`, 400/404 behavior unchanged.

## Data access

Per-host entities, `DbContext`, `LinqQueryMapping`, repositories. EF Core uses `IncludeSorted`; EF6 loads children then `OrderByNested` for `sortfor` on authors and awards.

## Verification

`dotnet build` both projects; smoke HTTP against SQL Server with existing user secrets; EF6 startup fails clearly when `Engine` is `Db2`.
