# Expresso.Sample.WebApi.EfCore

ASP.NET Core sample that filters and sorts with **EF Core** and Expresso (`Where`, `OrderBy`, `IncludeSorted`). It uses the same `Expresso_Sample` database and the same `ExpressoSample:Engine` switch as the ADO samples.

No reference to `Expresso.Sample.Shared`. Walkthrough: [docs/getting-started-linq.md](../../docs/getting-started-linq.md). Schema: [samples/database](../database).

## Prerequisites

- .NET 10 SDK
- Database created from [samples/database](../database)
- User secrets already set for [Expresso.Sample.WebApi](../Expresso.Sample.WebApi) (same `UserSecretsId`)

## Engine

Set `ExpressoSample:Engine` in `appsettings.json` and put the matching connection string in user secrets under `ConnectionStrings:{Engine}`. Supported: `SqlServer`, `PostgreSql`, `MySql`, `MariaDb`, `Sqlite`, `Oracle`, `Db2`.

## Run

```powershell
dotnet run --project samples/Expresso.Sample.WebApi.EfCore
```

Swagger: `/swagger` (the site root redirects there). Endpoints match the ADO sample (`GET /api/books`, `/api/authors`, `/api/publishers` with `filter`, `sort`, and optional `page` / `pagesize` or `skip` / `take`). These paging parameter names belong to the sample API. A positive offset or a limit adds `X-Total-Count`, even for an empty result. Only page-based requests add `X-Total-Pages`. Requests that combine the two models or supply `page` without `pagesize` return HTTP 400. See the [sample contract](../../docs/sample-app.md#pagination-contract) and [library semantics](../../docs/pagination.md).

## Response projection (0.13.0)

Use `fields` on list or by-id endpoints. Missing `fields` returns the full response. An empty value or unknown/malformed path returns 400.

```http
GET /api/books?fields=title,year,authors.lastname
GET /api/books/1?fields=title,authors.awards.title
GET /api/authors?fields=lastname,awards
GET /api/publishers?fields=name,opens
```

A root selection such as `fields=title,year` returns objects like `{"title":"Example","year":2024}`. A selected null, such as `isbn`, remains `"isbn":null`; omitting the field removes the property. `fields=authors` selects the complete author/award subtree. `fields=` and `fields=unknown` return 400 before database access. Database projections select requested scalars plus hidden correlation keys and execute only requested collection levels. See [Field projection](../../docs/projection.md).
