# `any`

True if the related collection has at least one item matching the optional predicate. The predicate is parsed in the **item** catalog (fields of one related row), not the outer entity.

## Syntax

```text
any(collection)
any(collection, predicate)
```

1 or 2 arguments.

- **Category:** Collection quantifier
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `collection` | A collection name in the current `QueryModel` (becomes `CollectionRef`) |
| 2 | `predicate` | `bool` (optional). Parsed against `collection.Items`. |

## Validation & exceptions

- **Parser:** first argument is not a collection → `ArgumentException`: `"First argument of Any() must be a collection."`
- **Parser:** unknown collection/field name → `ArgumentException`: `"Illegal field name: '...'"`
- **IR construction** (`AnyFunc`): `collection` is `null` → `ArgumentNullException`; predicate present but not `bool` → `ArgumentException`. Constructed directly by the parser (not via reflection).

Not valid as a sort key (`ISortDirectiveParser` throws `ArgumentException`).

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`{FromClause}` and `{CorrelateSql}` come from `CollectionSqlMapping` (application-authored, so they may already be dialect-specific).

### All dialects

```sql
EXISTS (SELECT 1 FROM {FromClause} WHERE {CorrelateSql} [AND predicate])
```

Example: `any(authors, eq(displayname, "Leo Tolstoy"))` with the sample book mapping:

```sql
EXISTS (SELECT 1 FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id AND ([a].[display_name] = @wparam_0))
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
collection.Any()
collection.Any(item => predicate)
```

`collection` is the navigation mapped with `LinqQueryMapping<T>.Collection(name, navigation, items)`, and `predicate` is rendered against the `items` mapping. The TRUE condition is `collection.Any(item => predicate)` and the FALSE condition (used by `not(...)`) is `!collection.Any(item => predicate)`. The result is never NULL: an item whose predicate is unknown (NULL) is not a match, as in SQL `EXISTS`. Example: `any(tags, eq(label,"blue"))` builds `e => e.Tags.Any(tags => tags.Label != null && tags.Label == p0)`, where `p0` is a captured parameter. Nested collections chain: `any(tags, any(tag_meta, eq(kind,"size")))` builds `e => e.Tags.Any(tags => tags.TagMeta.Any(tag_meta => tag_meta.Kind != null && tag_meta.Kind == p0))`.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
EXISTS (SELECT 1 FROM [widget_tag] AS [w0] WHERE [w].[Id] = [w0].[WidgetId] AND [w0].[Label] = @__Value_0)
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server:

```sql
EXISTS (SELECT 1 AS [C1] FROM [dbo].[widget_tag] AS [Extent2] WHERE ([Extent1].[Id] = [Extent2].[WidgetId])
        AND ([Extent2].[Label] IS NOT NULL) AND (([Extent2].[Label] = @p__linq__0) OR (([Extent2].[Label] IS NULL) AND (@p__linq__0 IS NULL))))
```

Every EF6 provider supports `any`.

## Notes

- Inner identifiers do not see outer fields. Combine with an outer predicate: `and(gt(year, 2020), any(authors, eq(displayname, "Leo Tolstoy")))`.
- Nested collections: `any(authors, any(awards, eq(name, "Nobel Prize")))`.
- See [`all`](all.md), [`none`](none.md), and [`count`](count.md).
