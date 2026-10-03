# Renderer integration tests

These tests render IR with each dialect transformer and execute SQL against a real engine. They are **skipped** unless `EXPRESSO_IT=1` (or `IntegrationTests:Enabled=true` in `appsettings.json`). CI uses `--filter Category!=Integration` and does not set `EXPRESSO_IT`.

Skip is applied inside `[SkippableTheory]` methods (`Skip.IfNot`) so VSTest still discovers theory rows when the suite is off.

## Run locally

```powershell
docker compose -f docker/docker-compose.it.yml up -d
# Wait until Oracle/DB2 are healthy (first pull is slow).
$env:EXPRESSO_IT = "1"
dotnet test test/Rendering/Expresso.Rendering.Integration.Test -c Release   # net6.0, net8.0, net48 one after another
dotnet test test/Rendering/Expresso.Rendering.Integration.Test -c Release -f net8.0   # one framework (EF Core runs on net8.0 only, EF6 on net48 only)
```

Frameworks run sequentially (`TestTfmsInParallel=false`) because every fixture drops and re-seeds the same tables. net48 is Windows-only; DB2 ADO IT runs on net6.0 and net8.0.

EF Core sessions (`Ef/`) reuse each ADO fixture's connection and seed. Provider specifics: Pomelo needs `AllowUserVariables=True` (added to the MySQL/MariaDB EF connection string); IBM needs `EnableEFCaseSensitivity=true` to quote identifiers, and DB2 sort keys are lifted into a derived table because DB2 rejects correlated subqueries in `ORDER BY`. A failing differential case prints the EF SQL.

EF6 sessions (`Ef6/`, net48 only) open their own connection per query on the same seeded database. `ItEf6Configuration` registers every EF6 provider, because EF6 allows one `DbConfiguration` per AppDomain. Provider specifics: MySql.Data (MySQL and MariaDB) needs `SslMode=Disabled` instead of `None`; System.Data.SQLite needs `BinaryGUID=False` (GUIDs are seeded as text) and releases the file only after `ClearAllPools()` and a GC; Oracle's default schema is the uppercase user. Documented EF6 gaps are pinned in `Ef6ProviderGaps`: each must throw, and the MySQL 8 `TimeSpan`-parameter quirk is skipped with its reason. No DB2 (no EF6 provider on NuGet).

Connection strings: **user secrets** (`UserSecretsId` in the test `.csproj`) or [appsettings.json](appsettings.json) placeholders. Set secrets once:

```powershell
dotnet user-secrets set "IntegrationTests:ConnectionStrings:SqlServer" "Server=..." --project test/Rendering/Expresso.Rendering.Integration.Test
```

Visual Studio **secrets.json** uses the same keys under `IntegrationTests:ConnectionStrings`. Override with env vars: `IntegrationTests__ConnectionStrings__PostgreSql`, etc. (wins over secrets).

**SQL Server catalog** is `expresso_it`. Other engines use `expesso_it`. Create the empty SQL Server database once (`CREATE DATABASE expresso_it`) if the fixture cannot create it.

SQLite uses a temp file (no connection string). The test project references `SQLitePCLRaw.bundle_e_sqlite3` so `e_sqlite3` is available on net48; net48 builds use `RuntimeIdentifier` `win-x64` so native assets land in the test output.

If `EXPRESSO_IT=1` and an engine cannot connect, that collection **fails**.

Oracle IT uses `Pooling=false` and `OracleConnection.ClearAllPools()` on fixture teardown. On net48, VSTest AppDomain unload is disabled (`net48.runsettings` / `appDomain: denied`) because ODP.NET's pool manager catches `ThreadAbortException` and starts a new dedicated thread, which then throws `AppDomainUnloadedException` after tests have already passed.

## Coverage

See [IntegrationCoverageMatrix.md](IntegrationCoverageMatrix.md). Every engine runs the same `RendererIntegrationCases` catalog.
