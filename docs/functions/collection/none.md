# `none`

True if the related collection has no item matching the optional predicate. One argument means the collection is empty.

## Syntax

```text
none(collection)
none(collection, predicate)
```

1 or 2 arguments.

- **Category:** Collection quantifier
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `collection` | A collection name in the current `QueryModel` |
| 2 | `predicate` | `bool` (optional). Parsed against `collection.Items`. |

## Validation & exceptions

- **Parser:** first argument is not a collection → `ArgumentException`: `"First argument of None() must be a collection."`
- **IR construction** (`NoneFunc`): same null/type rules as [`any`](any.md). Constructed directly by the parser.

Not valid as a sort key.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`{FromClause}` and `{CorrelateSql}` come from `CollectionSqlMapping` (application-authored, so they may already be dialect-specific).

### All dialects

```sql
NOT EXISTS (SELECT 1 FROM {FromClause} WHERE {CorrelateSql} [AND predicate])
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
!collection.Any()
!collection.Any(item => predicate)
```

`collection` is the navigation mapped with `LinqQueryMapping<T>.Collection(name, navigation, items)`, and `predicate` is rendered against the `items` mapping. The TRUE condition is `!collection.Any(item => predicate)` and the FALSE condition (used by `not(...)`) is `collection.Any(item => predicate)`. The result is never NULL: an item whose predicate is unknown (NULL) is not a match, so it does not make `none` false. Example: `none(tags, eq(label,"blue"))` builds `e => !e.Tags.Any(tags => tags.Label != null && tags.Label == p0)`, where `p0` is a captured parameter, and `none(tags)` builds `e => !e.Tags.Any()`.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
NOT EXISTS (SELECT 1 FROM [widget_tag] AS [w0] WHERE [w].[Id] = [w0].[WidgetId] AND [w0].[Label] = @__Value_0)
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server, `none(tags)` renders as:

```sql
NOT EXISTS (SELECT 1 AS [C1] FROM [dbo].[widget_tag] AS [Extent2] WHERE [Extent1].[Id] = [Extent2].[WidgetId])
```

Every EF6 provider supports `none`.

## Notes

- See [`any`](any.md) and [`all`](all.md).
