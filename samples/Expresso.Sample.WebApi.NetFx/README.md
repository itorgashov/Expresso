# Expresso.Sample.WebApi.NetFx

.NET Framework 4.8 sample using OWIN self-host and ASP.NET Web API 2. Shares data access and filtering logic with the modern sample via [Expresso.Sample.Shared](../Expresso.Sample.Shared).

For architecture and endpoint examples, see [docs/sample-app.md](../../docs/sample-app.md).

## Prerequisites

- .NET Framework 4.8 targeting pack (via Visual Studio or Build Tools)
- .NET SDK (to build with `dotnet build`)
- SQL Server with database **Expresso_Sample** (see [database/schema.sql](../Expresso.Sample.WebApi/database/schema.sql))

## Connection string

The key is defined in `appsettings.json`; the value comes from **user secrets**:

```powershell
dotnet user-secrets set "ConnectionStrings:ExpressoSample" "Server=YOUR_SERVER;Database=Expresso_Sample;Trusted_Connection=True;TrustServerCertificate=True" --project samples/Expresso.Sample.WebApi.NetFx
```

## Run

```powershell
dotnet run --project samples/Expresso.Sample.WebApi.NetFx
```

Listens on `http://localhost:5080/`. Open Swagger UI at `/swagger`.

## Endpoints

Same as the ASP.NET Core sample:

| Resource | GET all | GET by id |
|---|---|---|
| Books | `GET /api/books?filter=&sort=` | `GET /api/books/{id}` |
| Authors | `GET /api/authors?filter=&sort=` | `GET /api/authors/{id}` |
| Publishers | `GET /api/publishers?filter=&sort=` | `GET /api/publishers/{id}` |

Example: `GET /api/publishers?filter=eq(opens,"09:00")` (time-of-day field mapped as `TimeSpan` on this host).
