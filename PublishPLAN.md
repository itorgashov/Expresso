# Publish to NuGet.org — Plan

## Workflows

| File | Trigger | Purpose |
|---|---|---|
| `.github/workflows/ci.yml` | push, pull_request | Test + pack verify |
| `.github/workflows/publish.yml` | tag `v*` on a commit reachable from `main` | Test, pack with tag version, push to nuget.org |

## publish.yml behavior

1. Runs on tag push matching `v*` (e.g. `v0.4.0`).
2. Fetches `main` and fails unless the tagged commit is an ancestor of `origin/main` (tags on feature branches are rejected).
3. Sets package version from the tag (`v0.4.0` → `0.4.0`) via `/p:Version=`.
4. Runs tests with **`dotnet test -f net6.0`** (Linux agent; `net48` tests run in CI Windows job only).
5. Packs all three library projects; each nupkg contains **`lib/netstandard2.0`** and **`lib/net6.0`** assemblies.
6. Uses **NuGet Trusted Publishing**: `NuGet/login@v1` + `id-token: write`; no long-lived API key in the repo.

**Nothing publishes until you push a `v*` tag.** Local `dotnet pack` only writes to `artifacts/` on your machine.

## One-time setup (nuget.org + GitHub)

1. **nuget.org** → account → **Trusted Publishing** → add policy:
   - Repository owner: `itorgashov`
   - Repository: `Expresso`
   - Workflow file: `publish.yml` (filename only)
2. **GitHub repo** → Settings → Secrets → Actions → add `NUGET_USER` = your nuget.org **profile username** (not email).
3. Ensure the three package IDs are reserved/owned on nuget.org before first push.

## First release (after review)

Bump `Version` in `Directory.Build.props` if needed, merge to `main`, then:

```powershell
git checkout main
git pull
git tag v0.4.0
git push origin v0.4.0
```

Monitor: GitHub **Actions** → **Publish** workflow.

To publish manually without the workflow:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0
dotnet pack Expresso.slnx -c Release -o artifacts /p:Version=0.4.0
dotnet nuget push artifacts\Expresso.*.0.4.0.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_KEY --skip-duplicate
dotnet nuget push artifacts\Expresso.*.0.4.0.snupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_KEY --skip-duplicate
```

## Fallback

If Trusted Publishing is not configured, replace the login step with `NUGET_API_KEY` secret and pass it to `dotnet nuget push --api-key`.
