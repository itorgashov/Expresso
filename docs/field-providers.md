# Field providers

Expresso does not ship an application field list — it doesn't know your schema, and it must not guess it. Instead, every consuming application implements one interface that declares exactly which fields exist for filtering and sorting, and what CLR type each one is.

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

Source: [src/Expresso.Core/Filtering/IRequestFieldsInfoProvider.cs](../src/Expresso.Core/Filtering/IRequestFieldsInfoProvider.cs).

## What it actually specifies

Each method returns an array of `(string fieldName, Type fieldType)` tuples — the **complete allow-list** for a given `context`. This array is what the parser uses to:

1. **Resolve field tokens.** When the query string contains an identifier (e.g. `name` in `startswith(name,"Jo")`), the parser looks it up (case-insensitively) in this array. If it's not there, parsing fails with `ArgumentException: Illegal field name: '...'` — the field simply does not exist as far as Expresso is concerned.
2. **Type-check every use of that field.** The `Type` you provide becomes the field's `ReturnType` in the expression tree. Every function's constructor validates its arguments' `ReturnType`s (see [docs/error-handling.md](error-handling.md) and each function's page under [docs/functions/](functions/README.md)), so declaring `age` as `typeof(int)` means `startswith(age, "1")` fails validation — `age` is not a `string`.
3. **Enforce a security boundary.** Because the parser can only ever resolve fields that appear in this array, **only the fields you explicitly list are queryable** — there is no way for a caller to filter or sort by an arbitrary column, regardless of what your actual database schema exposes. This is the mechanism that prevents unintended data exposure through the query string.

## The `context` parameter

`context` lets one provider serve multiple endpoints/entities from a single implementation — for example, a `"book"` context returning book fields and an `"author"` context returning author fields, both looked up by the same injected `IRequestFieldsInfoProvider`. It is an opaque string you define; there is no fixed vocabulary. The convention used by the sample app is a lower-cased entity name (`"book"`, `"author"`, `"publisher"`) matched case-insensitively, with an empty array (`[]`, i.e. nothing filterable/sortable) returned for unknown contexts — see [samples/Expresso.Sample.WebApi/Filtering/RequestFieldsInfoProvider.cs](../samples/Expresso.Sample.WebApi/Filtering/RequestFieldsInfoProvider.cs).

## Filter fields vs. sort fields

`GetValidFilterFields` and `GetValidSortFields` are separate methods, so a field can be sortable without being filterable, or vice versa, per context. In practice they're often identical (as in the sample), but the split exists so you can, for example, allow sorting by a computed/expensive column while disallowing filtering by it, or the reverse.

## Minimal example

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
        ],
        "author" =>
        [
            ("firstname", typeof(string)),
            ("lastname", typeof(string)),
            ("displayname", typeof(string)),
            ("dateofbirth", typeof(DateTime)),
        ],
        _ => []
    };

    public (string, Type)[] GetValidSortFields(string context) => GetValidFilterFields(context);
}
```

Register it once, as a singleton, and inject `IRequestFieldsInfoProvider` wherever you parse query strings:

```csharp
builder.Services.AddSingleton<IRequestFieldsInfoProvider, RequestFieldsInfoProvider>();
```

```csharp
var filterCriteria = filterParser.Parse(filterQuery, fieldsProvider.GetValidFilterFields("book"));
var sortDirective = sortParser.Parse(sortQuery, fieldsProvider.GetValidSortFields("book"));
```

## Field name exposed vs. database column name

The field name declared here (e.g. `"createdat"`) is **not** necessarily the database column name (e.g. `b.created_at`). That mapping is a separate concern, supplied to the SQL renderer as a `fieldToColumnMap` — see step 5 in [docs/getting-started.md](getting-started.md). Keeping these separate means the query-string vocabulary (what clients type) can stay stable even if you rename columns or change joins/aliases in the underlying SQL.
