# Multi-target plan

Status: **complete**.

## Goal

Ship Expresso libraries for **`netstandard2.0`** and **`net6.0`**, run tests on **`net6.0`** (Linux CI) and **`net48`** (Windows CI), and demonstrate consumption from both modern ASP.NET Core and .NET Framework Web API 2.

## Decisions

| Area | Choice |
|---|---|
| Library TFMs | `netstandard2.0;net6.0` |
| Test TFMs | `net6.0;net48` (same test projects, not duplicates) |
| `IsExternalInit` | `src/Compatibility/IsExternalInit.cs` linked for `netstandard2.0` / `net48` |
| Portable API | Avoid `Index`/`Range`, `Contains(char)`, capacity `HashSet` ctor, `Split` char overload |
| Sample shared layer | `samples/Expresso.Sample.Shared` (`netstandard2.0`) — models, repos, filtering |
| Modern host | `samples/Expresso.Sample.WebApi` (`net10`) — thin ASP.NET Core host + Swagger |
| NetFx host | `samples/Expresso.Sample.WebApi.NetFx` (`net48`) — OWIN self-host + Web API 2 |
| CI | Linux: `dotnet test -f net6.0` + pack; Windows: `dotnet test -f net48` + NetFx sample build |
| Version bump | None (still **0.4.0**) |

## Implementation summary

- **Libraries:** `Expresso.Core`, `Expresso.Parsing`, `Expresso.Rendering.SqlServer` multi-target; conditional `Microsoft.Extensions.DependencyInjection` package versions.
- **Tests:** all three test projects multi-target `net6.0;net48`; **662** tests pass on both frameworks.
- **Samples:** shared data/filtering extracted; NetFx sample exposes the same six GET endpoints with Swashbuckle UI at `/swagger`.
- **Docs:** README, packages, getting-started, sample-app, CONTEXT, IMPLEMENTATION updated for TFMs and sample layout.

## Validation

```powershell
dotnet test Expresso.slnx -c Release -f net6.0
dotnet test Expresso.slnx -c Release -f net48          # Windows only
dotnet build samples/Expresso.Sample.WebApi.NetFx -c Release
dotnet pack Expresso.slnx -c Release -o artifacts     # expect lib/netstandard2.0 and lib/net6.0
```
