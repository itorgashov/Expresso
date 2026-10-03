# Field providers

A field provider tells Expresso which fields clients can filter and sort by, and the CLR type of each one. Expresso does not know your schema and does not guess it, so every application implements one interface that lists the fields explicitly.

```csharp
namespace Expresso.Core.Filtering
{
    public interface IRequestFieldsInfoProvider
    {
        (string, Type)[] GetValidFilterFields(string context);
        (string, Type)[] GetValidSortFields(string context);
    }
}
```

## What the provider specifies

Each method returns an array of `(string fieldName, Type fieldType)` tuples: the complete allow-list for a `context`. The parser uses it in three ways.

- It resolves field names. When the query contains an identifier such as `name` in `startswith(name,"Jo")`, the parser looks it up, ignoring case. If the name is not in the array, parsing fails with `ArgumentException: Illegal field name: '...'`.
- It checks types. The `Type` you give becomes the field's type in the expression tree, and every function validates the types of its arguments (see [Error handling](error-handling.md) and the [function reference](functions/README.md)). If you declare `age` as `typeof(int)`, then `startswith(age, "1")` fails because `age` is not a string.
- It sets a security boundary. The parser can only resolve fields in the array, so a client cannot filter or sort by any other column, whatever your database schema exposes. This keeps the query string from exposing data you did not intend to share.

## Use the context parameter

`context` lets one provider serve several endpoints. For example, a `"book"` context returns book fields and an `"author"` context returns author fields, all from the same injected `IRequestFieldsInfoProvider`. You define the strings. The sample apps use the lower-cased entity name (`"book"`, `"author"`, `"publisher"`), compare it ignoring case, and return an empty array for an unknown context, so nothing is filterable or sortable there. Each host has its own implementation:

- [net10 host](../samples/Expresso.Sample.WebApi/Filtering/RequestFieldsInfoProvider.cs) uses `DateOnly` and `TimeOnly`.
- [net48 host](../samples/Expresso.Sample.WebApi.NetFx/Filtering/RequestFieldsInfoProvider.cs) uses `DateTime` and `TimeSpan`.

The query field names are the same in both (`opens`, for example, maps to the column `opens_at`). `externalid` (a `Guid`) is listed for filtering but not for sorting, because a `Guid` is not orderable.

## Allow different fields for filter and sort

`GetValidFilterFields` and `GetValidSortFields` are separate, so a field can be sortable but not filterable, or the reverse. Often they are identical, as in the sample. Split them when, for example, you want to allow sorting by a computed column but not filtering by it.

## See a complete example

```csharp
using Expresso.Core.Filtering;

public sealed class RequestFieldsInfoProvider : IRequestFieldsInfoProvider
{
    public (string, Type)[] GetValidFilterFields(string context) => context.ToLowerInvariant() switch
    {
        "book" =>
        [
            ("title", typeof(string)),
            ("year", typeof(int)),
            ("isbn", typeof(string)),
            ("publisher", typeof(string)),
            ("price", typeof(double)),
            ("rating", typeof(double)),
            ("createdat", typeof(DateTime)),
            ("externalid", typeof(Guid)),
        ],
        "author" =>
        [
            ("firstname", typeof(string)),
            ("lastname", typeof(string)),
            ("displayname", typeof(string)),
            ("dateofbirth", typeof(DateTime)),
            ("createdat", typeof(DateTime)),
        ],
        "publisher" =>
        [
            ("name", typeof(string)),
            ("country", typeof(string)),
            ("location", typeof(string)),
            ("opens", typeof(TimeSpan)),
            ("closes", typeof(TimeSpan)),
        ],
        _ => []
    };

    public (string, Type)[] GetValidSortFields(string context) => GetValidFilterFields(context);
}
```

Register it once as a singleton and inject `IRequestFieldsInfoProvider` wherever you parse query strings:

```csharp
builder.Services.AddSingleton<IRequestFieldsInfoProvider, RequestFieldsInfoProvider>();
```

```csharp
var filterCriteria = filterParser.Parse(filterQuery, fieldsProvider.GetValidFilterFields("book"));
var sortDirective = sortParser.Parse(sortQuery, fieldsProvider.GetValidSortFields("book"));
```

## Describe collections with a query model

Tuples cannot describe related sets. If you expose `any(authors, …)` or similar, also implement `IRequestQueryModelProvider`. The fields of a collection's items live on a nested `QueryModel`, not as extra tuples on the outer list.

```csharp
public interface IRequestQueryModelProvider
{
    QueryModel GetFilterModel(string context);
    QueryModel GetSortModel(string context);
}
```

A query model has this shape:

```text
QueryModel                  // one per API context, for example "book"
  Fields                    // scalar fields at this level only
  Collections[]
    CollectionModel
      Name                  // "authors", as used in any(authors, ...)
      Items : QueryModel    // fields and nested collections of one item
```

A name cannot be both a field and a collection on the same `QueryModel`; the constructor throws. Parse with `filterParser.Parse(filter, model)`. The overload that takes tuples is shorthand for `QueryModel.FromFields(tuples)`, a model without collections.

- One level, as in the sample books: put the author fields under `authors` on the book model. Then `displayname` is illegal at the book root and legal inside `any(authors, …)`. `GET /api/authors?filter=eq(displayname,"…")` still uses the flat `"author"` model.
- Nested collections: put another `CollectionModel` on the item model (`authors.Items` contains `awards`). Use `sortfor(authors/awards, …)` in `sort=` to order the child rows. See [sortfor](functions/collection/sortfor.md).

The renderers use a parallel tree with the same names: `SqlQueryMapping` and `CollectionSqlMapping` for SQL, `LinqQueryMapping<T>` for LINQ. The provider never contains `FROM` or `JOIN`; those belong to the mapping.

The sample hosts implement both interfaces on the same class, in the two `RequestFieldsInfoProvider.cs` files linked above.

## Keep field names separate from column names

A field name declared here, such as `"createdat"`, is not necessarily a database column such as `b.created_at`. You give the renderer that mapping separately: a field-to-column dictionary or `SqlQueryMapping` for SQL (see [Render to SQL](getting-started-sql.md#map-fields-to-columns)), or a `LinqQueryMapping<T>` for LINQ (see [Render to LINQ and EF](getting-started-linq.md#map-fields-to-members)). Keeping them apart lets the vocabulary your clients type stay stable when you rename columns or change joins.
