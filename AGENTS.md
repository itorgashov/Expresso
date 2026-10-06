# Expresso — agent guide

Expresso parses a function-call filter/sort string into an expression tree and renders parameterized SQL or LINQ (EF Core / EF6). It is a set of NuGet libraries plus sample hosts, not an application with its own database.

Folder-specific guides add rules for their area: [src/Rendering](src/Rendering/AGENTS.md), [integration tests](test/Rendering/Expresso.Rendering.Integration.Test/AGENTS.md), [samples](samples/AGENTS.md), [docs](docs/AGENTS.md).

## Read first

- [CONTEXT.md](CONTEXT.md) — target layout and domain concepts. Update it only when structure changes. Keep entries short and point at code or docs.
- [IMPLEMENTATION.md](IMPLEMENTATION.md) — status of what is already built. After a task, add a brief note with references. Do not paste code or restate docs.
- The feature plan for the area you are touching (`*PLAN.md` in the repo root), then the matching pages under [docs/](docs/). Function behavior lives on the function page, not in these files.
- [docs/overview.md](docs/overview.md) if you do not already know the pipeline.

## Workflow

1. Discover the existing types, callers, and tests before proposing a design.
2. Write a plan as `<FEATURE>PLAN.md` in the repo root: a few options, the chosen one, interfaces and signatures, and the file/class/method sequence. Stop there.
3. Implement only after the user explicitly approves that plan.
4. Change as little as possible. Extend the current pattern. If a new pattern replaces an old one, remove the old one in the same change.
5. The solution must compile and the relevant tests must pass.
6. Update `CONTEXT.md` and/or `IMPLEMENTATION.md` with a few lines. Update the feature plan if the approved design changed.
7. End with a short summary of what changed and how it was verified.

Use the plan–review–execute–validate task list for multi-step work.

## Product rules

- Public library types ship XML documentation (`GenerateDocumentationFile` in [Directory.Build.props](Directory.Build.props)). Package version is `Version` in that file.
- A new IR function touches every layer: a Core node that validates in its constructor, parser registration, each SQL dialect walker, the LINQ visitor, EF Core and EF6 overrides where they diverge, unit tests, and a function page. See [src/Rendering/AGENTS.md](src/Rendering/AGENTS.md) and [docs/AGENTS.md](docs/AGENTS.md).
- Libraries target `netstandard2.0;net6.0` (EF Core package `net8.0`, EF6 package `net48`). `DateOnly` / `TimeOnly` code sits behind `#if NET6_0_OR_GREATER`.
- `samples/database/*/schema.sql` drops and recreates databases. Never run those scripts. Never generate or run DDL or DML against a real server. Integration tests own their own `widget*` tables.
- SQL Server MCP is read-only. If a change needs a write, print the statement for the user to run.
- `RefSrc/`, when present, is upstream NuGet source for reading. Depend on package references. Do not copy that code into `src/`.
- Shell is Windows PowerShell. Verify the working directory before a relative path.
- Stop sample hosts (`Expresso.Sample.WebApi*`) before a solution build. A running host locks `Expresso.Core.dll` and MSBuild then fails with MSB3027.

## Code

- Search for an existing implementation before adding a type or a helper.
- Prefer a small, local change over a new abstraction. Files should stay under about 300 lines; split them (partial classes are the existing pattern) when they pass that.
- Nullable reference types are on. Public APIs stay stable unless the task is an explicit breaking change (version bump and call-site updates).
- Mermaid and other diagrams set explicit light and dark fills so text stays readable in both themes.

## Tests

Unit tests do not open a database. Integration tests are marked `Category=Integration` and run only when `EXPRESSO_IT=1`. CI always excludes them.

```powershell
dotnet test .\Expresso.slnx -c Release -f net6.0 --filter "Category!=Integration"
dotnet test .\Expresso.slnx -c Release -f net48 --filter "Category!=Integration"
```

Run `net48` on Windows when the change touches parsing, SQL rendering, LINQ, or EF6. The EF Core test project always builds `net8.0` (`TreatAsLocalProperty="TargetFramework"`), so it runs in either leg.

- New renderer code targets at least 85% line coverage in that package's test project.
- Complex tests start with a comment: scenario, setup, expected result.
- In unit tests, mock external I/O (HTTP, ADO clients, SQL). Do not mock types the test can construct.

## Docs

User-facing behavior changes update the matching page under `docs/` in the same change. Guides open with one scope sentence. Do not duplicate function SQL into `IMPLEMENTATION.md`.

## Git

Commit, push, and open pull requests only when the user asks. Do not commit secrets (user secrets, connection strings, `appsettings` with credentials).
