# `sum`

Returns the sum of a numeric selector over a related collection. `sum` works only on collections; there is no scalar `sum`.

## Syntax

```text
sum(collection, selector)
```

Exactly 2 arguments.

- **Category:** Collection aggregate
- **Return type:** `double` if `selector` is `double`; otherwise `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `collection` | A collection name in the current `QueryModel` |
| 2 | `selector` | Item-scope numeric expression (`byte`, `int`, or `double`) |

## Validation & exceptions

- If the first argument is not a collection, parsing throws `ArgumentException`: `"First argument of Sum() must be a collection."`
- If you pass only one argument, parsing throws `System.Exception`: `"Sum() function should have 2 arguments."`
- A non-numeric selector throws `ArgumentException("Illegal argument type", "selector")`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`{FromClause}` and `{CorrelateSql}` come from `CollectionSqlMapping` (application-authored, so they may already be dialect-specific).

### All dialects

```sql
(SELECT SUM(selector) FROM {FromClause} WHERE {CorrelateSql})
```

DB2 rejects correlated references in `ORDER BY` scalar subqueries (`SQL0206N`). This fragment is valid in `WHERE` and in a `SELECT` list. For a sort key, select it in a derived table (or extra SELECT column) and order by that alias. See [docs/rendering.md](../../rendering.md).

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
(int)collection.Sum(item => (int?)selector)
```

`collection` is the navigation mapped with `LinqQueryMapping<T>.Collection(name, navigation, items)`, and `selector` is rendered against the `items` mapping. A `byte` selector is widened to `int` first, and a `double` selector uses `double?` and `double`. `Enumerable.Sum` returns 0 when there are no values, so the result is NULL, like SQL `SUM`, unless `collection.Any(item => selector != null)` holds (`collection.Any()` for a non-nullable selector). Example: `eq(sum(tags,score),30)` builds `e => e.Tags.Any() && (int)e.Tags.Sum(tags => (int?)tags.Score) == p0`, where `p0` is a captured parameter, and `isnull(sum(tags,score))` builds `e => !e.Tags.Any()`.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server its `COALESCE(..., 0)` is the .NET empty sum, and the `EXISTS` guard restores the SQL NULL:

```sql
EXISTS (SELECT 1 FROM [widget_tag] AS [w0] WHERE [w].[Id] = [w0].[WidgetId])
AND (SELECT COALESCE(SUM([w1].[Score]), 0) FROM [widget_tag] AS [w1] WHERE [w].[Id] = [w1].[WidgetId]) = @__Value_0
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server it projects `SUM` (without `COALESCE`) as a column of a derived table:

```sql
(SELECT SUM([Extent2].[Score]) AS [A1] FROM [dbo].[widget_tag] AS [Extent2] WHERE [Extent1].[Id] = [Extent2].[WidgetId]) AS [C1]
```

The filter then guards that column: `WHERE (EXISTS (SELECT 1 AS [C1] FROM [dbo].[widget_tag] AS [Extent3] WHERE [Project1].[Id] = [Extent3].[WidgetId])) AND ([Project1].[C1] = @p__linq__0)`.

Every EF6 provider supports `sum`.

## Notes

- See [`avg`](avg.md) and [`count`](count.md).
