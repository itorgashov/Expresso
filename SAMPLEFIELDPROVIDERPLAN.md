# Per-host field catalogs

Status: **complete**. Sample-only; no package version bump.

## Why

Shared `RequestFieldsInfoProvider` cannot use `DateOnly`/`TimeOnly` (Shared is netstandard2.0). The net10 host loads Expresso `net6.0`; net48 loads `netstandard2.0`. Catalogs must match the loaded IR types.

Query field **`opens`** maps to SQL `opens_at` (not `opensat`). Same names on both hosts; CLR types differ.

## Layout

- Delete [samples/Expresso.Sample.Shared/Filtering/RequestFieldsInfoProvider.cs](samples/Expresso.Sample.Shared/Filtering/RequestFieldsInfoProvider.cs)
- Add [samples/Expresso.Sample.WebApi/Filtering/RequestFieldsInfoProvider.cs](samples/Expresso.Sample.WebApi/Filtering/RequestFieldsInfoProvider.cs): `TimeOnly` / `DateOnly`
- Add [samples/Expresso.Sample.WebApi.NetFx/Filtering/RequestFieldsInfoProvider.cs](samples/Expresso.Sample.WebApi.NetFx/Filtering/RequestFieldsInfoProvider.cs): `TimeSpan` / `DateTime`
- `QueryParametersParser` stays in Shared
- Publisher repo map: `opens` → `p.opens_at`, `closes` → `p.closes_at`
