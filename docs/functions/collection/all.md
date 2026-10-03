# `all`

True if every item in the related collection matches the predicate. An empty collection is treated as vacuously true. A one-argument call (`all(authors)`) has no item constraint and always renders as true.

## Syntax

```text
all(collection)
all(collection, predicate)
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

- **Parser:** first argument is not a collection → `ArgumentException`: `"First argument of All() must be a collection."`
- **IR construction** (`AllFunc`): same null/type rules as [`any`](any.md). Constructed directly by the parser.

Not valid as a sort key.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`{FromClause}` and `{CorrelateSql}` come from `CollectionSqlMapping` (application-authored, so they may already be dialect-specific).

### All dialects

No predicate:

```sql
(1 = 1)
```

With predicate (portable "no counterexample" form):

```sql
NOT EXISTS (SELECT 1 FROM {FromClause} WHERE {CorrelateSql} AND NOT (predicate))
```

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
!collection.Any(item => predicateIsFalse)
```

`collection` is the navigation mapped with `LinqQueryMapping<T>.Collection(name, navigation, items)`, and `predicateIsFalse` is the predicate's FALSE condition rendered against the `items` mapping (for `gt(score,0)` it is `score <= 0`). The TRUE condition is `!collection.Any(item => predicateIsFalse)` and the FALSE condition (used by `not(...)`) is `collection.Any(item => predicateIsFalse)`. The result is never NULL: an item whose predicate is unknown (NULL) is not a counterexample, as in the SQL `NOT (predicate)` form, and an empty collection is TRUE. Without a predicate the TRUE condition is the constant `true` and the FALSE condition is `false`. Example: `all(tags, gt(score,0))` builds `e => !e.Tags.Any(tags => tags.Score <= p0)`, where `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server:

```sql
NOT EXISTS (SELECT 1 FROM [widget_tag] AS [w0] WHERE [w].[Id] = [w0].[WidgetId] AND [w0].[Score] <= @__Value_0)
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server:

```sql
NOT EXISTS (SELECT 1 AS [C1] FROM [dbo].[widget_tag] AS [Extent2] WHERE ([Extent1].[Id] = [Extent2].[WidgetId]) AND ([Extent2].[Score] <= @p__linq__0))
```

Every EF6 provider supports `all`.

## Notes

- Vacuous truth: `all(authors, pred)` is true when there are no related rows.
- See [`any`](any.md) and [`none`](none.md).
