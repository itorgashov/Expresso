# Sample WebAPI — Plan (executed)

## Delivered

- Project: [samples/Expresso.Sample.WebApi](samples/Expresso.Sample.WebApi) (net10.0, `IsPackable=false`)
- NuGet refs: `Expresso.Core`, `Expresso.Parsing`, `Expresso.Rendering.SqlServer` **0.1.1**
- Two layers: controllers (presentation) + ADO.NET repositories (data access)
- Controllers: `GetAll(filter, sort)` + `GetById(id)` for books, authors, publishers
- In-place filter/sort parsing and model→viewmodel mapping in controllers
- `IRepository<T>`, `RequestFieldsInfoProvider`, Swagger, user secrets for connection string
- Schema: [database/schema.sql](samples/Expresso.Sample.WebApi/database/schema.sql)
- CI: .NET 10 SDK added to [ci.yml](.github/workflows/ci.yml)

## Connection string

Key in `appsettings.json` (`ConnectionStrings:ExpressoSample`); value via user secrets.

## Field contexts

| Context | Filter/sort fields |
|---|---|
| book | title, year, isbn, publisher, price, rating, createdat; filter-only: externalid |
| author | firstname, lastname, displayname, dateofbirth, createdat |
| publisher | name, country, location, opens, closes |
