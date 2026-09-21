# NuGet publish harness

Each packable Expresso package has a YAML file under `packages/`. Tag workflow [publish.yml](../workflows/publish.yml) packs and pushes using these manifests.

## Schema

```yaml
packageId: Expresso.Rendering.PostgreSql
project: src/Rendering/Expresso.Rendering.PostgreSql/Expresso.Rendering.PostgreSql.csproj
unitTestProject: test/Rendering/Expresso.Rendering.PostgreSql.Test/Expresso.Rendering.PostgreSql.Test.csproj  # optional
publishOrder: 20
```

| Field | Meaning |
|---|---|
| `packageId` | NuGet `PackageId` |
| `project` | csproj to `dotnet pack` |
| `unitTestProject` | Optional; solution tests already ran before pack |
| `publishOrder` | Lower numbers push first (Core=1, Parsing=2, Common=10, dialects=20) |

MariaDB is not a separate package (`Expresso.Rendering.MySql` covers MySQL and MariaDB).
