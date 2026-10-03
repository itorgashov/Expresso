# Render to SQL

Expresso renders a parsed filter and sort as a parameterized `WHERE` and `ORDER BY` fragment for your database dialect. You append the fragments to your own `SELECT` and run it with ADO.NET or Dapper. Read this page after [Get started with Expresso](getting-started.md), once you have a `FilterCriteria` and a `SortDirective`.

## Choose the dialect package

Install the package for the database you run against, in the data-access project.

| Database | Package | Registration method |
|---|---|---|
| SQL Server | `Expresso.Rendering.SqlServer` | `AddSqlServerExpressionTransformations()` |
| PostgreSQL | `Expresso.Rendering.PostgreSql` | `AddPostgreSqlExpressionTransformations()` |
| SQLite | `Expresso.Rendering.Sqlite` | `AddSqliteExpressionTransformations()` |
| MySQL, MariaDB | `Expresso.Rendering.MySql` | `AddMySqlExpressionTransformations()` |
| Oracle | `Expresso.Rendering.Oracle` | `AddOracleExpressionTransformations()` |
| IBM DB2 | `Expresso.Rendering.Db2` | `AddDb2ExpressionTransformations()` |

```powershell
dotnet add MyApp.DataAccess package Expresso.Rendering.PostgreSql
```

Expresso does not include database drivers. Add the ADO.NET provider for your engine to the project that opens the connection. See [Database clients](packages.md#database-clients-not-included).

## Register the renderer

Call the registration method for your dialect. The example uses PostgreSQL:

```csharp
using Expresso.Rendering;

builder.Services.AddPostgreSqlExpressionTransformations();
```

Each package registers one `IExpressionToQueryClauseTransformer`. Inject it where you build queries.

## See what the renderer produces

The same filter produces different SQL on each dialect, because quoting and parameter names differ. For the filter `gt(createdAt,"2021-01-01")`, a field map that sends `createdAt` to `p.created_at`, and the parameter prefix `wparam`:

| Dialect | Rendered `WHERE` fragment |
|---|---|
| SQL Server | `([p].[created_at] > @wparam_0)` |
| PostgreSQL | `("p"."created_at" > @wparam_0)` |
| MySQL, MariaDB | `` (`p`.`created_at` > @wparam_0) `` |
| Oracle | `("p"."created_at" > :wparam_0)` |

SQLite and DB2 quote like PostgreSQL. The [SQL rendering](rendering.md) page lists quoting and parameter rules per dialect.

## Map fields to columns

Give the renderer a dictionary from field name to column. Only fields in the dictionary can reach the SQL, so the dictionary is a second safety net behind the field provider. Make the lookup case-insensitive.

```csharp
private static readonly Dictionary<string, string> FieldToColumn = new(StringComparer.OrdinalIgnoreCase)
{
    ["title"] = "b.title",
    ["year"] = "b.year",
    ["rating"] = "b.rating",
};
```

## Render the WHERE and ORDER BY clauses

Append each fragment to your own `SELECT`. The renderer returns the text without the `WHERE` or `ORDER BY` keyword, and the parameter values keyed by their bind names.

```csharp
var sql = new StringBuilder("SELECT b.id, b.title, b.year, b.rating FROM book b");
var parameters = new Dictionary<string, object>();

if (filterCriteria is not null)
{
    var (whereClause, whereParams) = _transformer.RenderWhereClause(filterCriteria, FieldToColumn, "wparam");
    sql.Append(" WHERE ").Append(whereClause);
    foreach (var (key, value) in whereParams) parameters[key] = value;
}

if (sortDirective is not null && sortDirective.Items.Count > 0)
{
    var (orderByClause, orderParams) = _transformer.RenderOrderByClause(sortDirective, FieldToColumn, "oparam");
    sql.Append(" ORDER BY ").Append(orderByClause);
    foreach (var (key, value) in orderParams) parameters[key] = value;
}
```

Use different prefixes for the filter and the sort (`wparam` and `oparam` here) so the bind names never collide. A prefix must start with a letter and contain only letters, digits and underscores.

## Filter on collections

To use `any`, `all`, `none`, `count` and the other collection functions, pass a `SqlQueryMapping` instead of the dictionary. It holds the outer field map and one `CollectionSqlMapping` per related collection:

```csharp
var mapping = new SqlQueryMapping(
    FieldToColumn,
    new[]
    {
        new CollectionSqlMapping(
            name: "authors",
            fromClause: "book_author ba JOIN author a ON a.id = ba.author_id",
            correlateSql: "ba.book_id = b.id",
            itemFieldToColumn: new Dictionary<string, string> { ["name"] = "a.name" }),
    });

var (whereClause, whereParams) = _transformer.RenderWhereClause(filterCriteria, mapping, "wparam");
```

`FromClause` and `CorrelateSql` are your own SQL, so write them in your dialect. The renderer wraps them in `EXISTS` or scalar subqueries. The sample [BookRepository](../samples/Expresso.Sample.Shared/DataAccess/BookRepository.cs) shows a complete mapping, including nested collections.

To order a child collection with `sortfor`, call `RenderOrderByClause` again with the nested `SortDirective` and the item mapping. See [sortfor](functions/collection/sortfor.md).

## Run the query

Bind each entry of `parameters` and run `sql.ToString()`. Expresso only produces the fragment and the values. It never opens a connection or runs anything.

With ADO.NET:

```csharp
using var command = connection.CreateCommand();
command.CommandText = sql.ToString();
foreach (var (name, value) in parameters)
{
    var parameter = command.CreateParameter();
    parameter.ParameterName = name;   // keep the name as returned, including @ or :
    parameter.Value = value;
    command.Parameters.Add(parameter);
}

using var reader = await command.ExecuteReaderAsync();
```

With Dapper, pass the dictionary directly:

```csharp
var books = await connection.QueryAsync<Book>(sql.ToString(), parameters);
```

## Next steps

- [SQL rendering](rendering.md): quoting, parameters and sort keys per dialect
- [Function reference](functions/README.md): the SQL each function produces on every dialect
- [Filter behavior and database differences](semantics.md): NULL handling, collation and integer division
- [Render to LINQ and EF](getting-started-linq.md): the other way to run a filter
