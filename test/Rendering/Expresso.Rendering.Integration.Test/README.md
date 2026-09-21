# Renderer integration tests

These tests render IR with each dialect transformer and execute SQL against a real engine. They are **skipped** unless `EXPRESSO_IT=1` (or `IntegrationTests:Enabled=true` in `appsettings.json`). CI uses `--filter Category!=Integration` and does not set `EXPRESSO_IT`.

Skip is applied inside `[SkippableTheory]` methods (`Skip.IfNot`) so VSTest still discovers theory rows when the suite is off.

## Run locally

```powershell
docker compose -f docker/docker-compose.it.yml up -d
# Wait until Oracle/DB2 are healthy (first pull is slow).
$env:EXPRESSO_IT = "1"
dotnet test test/Rendering/Expresso.Rendering.Integration.Test -c Release -f net6.0
dotnet test test/Rendering/Expresso.Rendering.Integration.Test -c Release -f net48   # Windows; DB2 IT is net6.0-only
```

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
