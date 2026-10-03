# `count`

Counts the items in a related collection, optionally only those matching a condition. The result is an `int`. To get a string's length, use [`len`](../string-inspect/len.md) instead.

## Syntax

```text
count(collection)
count(collection, predicate)
```

1 or 2 arguments.

- **Category:** Collection aggregate
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `collection` | A collection name in the current `QueryModel` |
| 2 | `predicate` | `bool` (optional). Parsed against `collection.Items`. |

## Validation & exceptions

- If the first argument is not a collection, parsing throws `ArgumentException`: `"First argument of Count() must be a collection."`
- A `null` collection throws `ArgumentNullException`. A predicate that is not `bool` throws `ArgumentException`.

You can use `count` as a sort key. It renders as a scalar subquery.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`{FromClause}` and `{CorrelateSql}` come from `CollectionSqlMapping` (application-authored, so they may already be dialect-specific).

### All dialects

```sql
(SELECT COUNT(*) FROM {FromClause} WHERE {CorrelateSql} [AND predicate])
```

SQL Server example: `eq(count(authors), 2)` renders as:

```sql
((SELECT COUNT(*) FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id) = @wparam_0)
```

DB2 rejects correlated references in `ORDER BY` scalar subqueries (`SQL0206N`). This fragment is valid in `WHERE` and in a `SELECT` list. For a sort key, select it in a derived table (or extra SELECT column) and order by that alias. See [docs/rendering.md](../../rendering.md).

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
collection.Count()
collection.Count(item => predicate)
```

`collection` is the navigation mapped with `LinqQueryMapping<T>.Collection(name, navigation, items)`, and `predicate` is rendered against the `items` mapping. The result is never NULL, and an item whose predicate is unknown (NULL) is not counted. Example: `eq(count(tags, eq(label,"red")),1)` builds `e => e.Tags.Count(tags => tags.Label != null && tags.Label == p0) == p1`, where `p0` and `p1` are captured parameters. As a sort key, `count(tags),desc` becomes `OrderByDescending(e => (int?)e.Tags.Count())`.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server, `eq(count(tags),2)` renders as:

```sql
(SELECT COUNT(*) FROM [widget_tag] AS [w0] WHERE [w].[Id] = [w0].[WidgetId]) = @__Value_0
```

As a sort key the correlated subquery stays in `ORDER BY` (`ORDER BY (SELECT COUNT(*) FROM [widget_tag] AS [w0] WHERE [w].[Id] = [w0].[WidgetId]) DESC`), so DB2 rejects it with `SQL0206N` as described above.

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server it projects the subquery as a column of a derived table:

```sql
(SELECT COUNT(1) AS [A1] FROM [dbo].[widget_tag] AS [Extent2] WHERE [Extent1].[Id] = [Extent2].[WidgetId]) AS [C1]
```

The filter then compares that column (`WHERE [Project1].[C1] = @p__linq__0`), and a sort key orders by it (`ORDER BY [Project1].[C1] DESC`).

Every EF6 provider supports `count`.

## Notes

- See [`any`](any.md), [`min`](min.md), [`sum`](sum.md).
