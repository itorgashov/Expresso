# TimeSpan time-of-day (0.6.0)

Status: **complete**. Package version **0.6.0**.

## Goal

Enable `eq(opens,"14:30")` on **netstandard2.0** / .NET Framework by treating `TimeSpan` as a **time-of-day** type (clock range `[0, 24h)`), while keeping `TimeOnly` on net6.0.

## Approach

- `TimeSpan` in `SupportedOperandTypes` and `DateTimeTypes.Time` on **all** TFMs.
- `time()` compiled on all TFMs; SQL `CAST(... AS time)` everywhere.
- `time()` return type: `TimeOnly` on net6.0; `TimeSpan` on netstandard2.0.
- Literals: `TryParseExact` with `hh:mm` / `hh:mm:ss` / fractional seconds; reject out-of-range and duration forms (`1.14:30`, `25:00`).
- `TimeOnly` and `TimeSpan` are **not** interchangeable in comparisons.

## Samples

- `dbo.publisher.opens_at` / `closes_at` `TIME(0)` mapped as `TimeSpan`; filter fields `opensat` / `closesat`. Later sample alignment: [SAMPLECOLUMNSPLAN.md](SAMPLECOLUMNSPLAN.md).

## Validate

```powershell
dotnet test .\Expresso.slnx -c Release -f net6.0
dotnet test .\Expresso.slnx -c Release -f net48
dotnet build .\samples\Expresso.Sample.WebApi.NetFx -c Release
```
