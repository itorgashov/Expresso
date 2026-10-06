# Integration tests — agent guide

These tests run rendered SQL and LINQ against real engines. Setup, connection strings and provider quirks are in [README.md](README.md); this file covers how to change the suite.

## Safety

- Run only against the local Docker engines from [docker/docker-compose.it.yml](../../../docker/docker-compose.it.yml) or test databases the user has configured (`expresso_it`). Never point the suite at a shared or production server.
- Fixtures drop and re-seed only the `widget*` tables. Do not add DDL or DML that touches other tables.
- Do not commit connection strings; they come from user secrets or `IntegrationTests__ConnectionStrings__*` variables.

## Running

```powershell
$env:EXPRESSO_IT = "1"
dotnet test test/Rendering/Expresso.Rendering.Integration.Test -c Release -f net8.0
```

- `net6.0`: ADO dialects plus DB2. `net8.0`: ADO plus EF Core. `net48`: ADO plus EF6 (Windows).
- Frameworks run one after another (`TestTfmsInParallel=false`); they share the same databases. Do not parallelize collections.
- With `EXPRESSO_IT=1`, an engine that cannot connect fails its collection. Do not add per-engine skip flags.

## Changing cases

- Cases live in `test/Rendering/Expresso.Rendering.TestCases`: `RendererIntegrationCases` (expected ids per engine) and `RendererDifferentialCases` (EF vs ADO, in-memory vs PostgreSQL). Seed data is `WidgetSeedData`; every engine runs the same ids.
- A seed change can move expected ids in many cases. Re-check the whole catalog on every engine, not only the case you added.
- Add a new function to [IntegrationCoverageMatrix.md](IntegrationCoverageMatrix.md).
- EF6 gaps go in `Ef6/Ef6ProviderGaps.cs` with the reason. `Throws = true` means the renderer must throw; `false` pins a known silent difference and needs a clear reason.
