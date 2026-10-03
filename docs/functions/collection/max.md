# `max` (collection)

Maximum of a selector expression over a related collection. The first argument must be a collection; otherwise the parser builds scalar [`max`](../arithmetic/max.md) (`MaxFunc`).

## Syntax

```text
max(collection, selector)
```

Exactly 2 arguments.

- **Category:** Collection aggregate
- **Return type:** same as `selector` (`byte`, `int`, `double`, `string`, `DateTime`, `TimeSpan`; plus `DateOnly`/`TimeOnly` on net6.0)

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `collection` | A collection name in the current `QueryModel` |
| 2 | `selector` | Item-scope expression of a min/max-capable type |

## Validation & exceptions

- **Parser (collection overload):** first argument is a `CollectionRef`; one argument → `System.Exception`: `"Max() function should have 2 arguments."`
- **IR construction** (`CollectionMaxFunc`): illegal selector type → `ArgumentException("Illegal argument type", "selector")`. Constructed directly by the parser.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`{FromClause}` and `{CorrelateSql}` come from `CollectionSqlMapping` (application-authored, so they may already be dialect-specific).

### All dialects

```sql
(SELECT MAX(selector) FROM {FromClause} WHERE {CorrelateSql})
```

**DB2 / `ORDER BY`:** DB2 rejects correlated references in `ORDER BY` scalar subqueries (`SQL0206N`). This fragment is valid in `WHERE` and in a `SELECT` list. For a sort key, select it in a derived table (or extra SELECT column) and order by that alias. See [docs/rendering.md](../../rendering.md).

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
collection.Max(item => (T?)selector)
```

`collection` is the navigation mapped with `LinqQueryMapping<T>.Collection(name, navigation, items)`, and `selector` is rendered against the `items` mapping and lifted to its nullable type (`int?`, `DateTime?`, and so on; a `string` stays `string`), without numeric promotion. NULL items are ignored, and the result is NULL when the collection has no non-NULL value. Example: `eq(max(tags,score),100)` builds `e => e.Tags.Max(tags => (int?)tags.Score) != null && e.Tags.Max(tags => (int?)tags.Score).Value == p0`, where `p0` is a captured parameter. The maximum of a string selector follows the database collation; see [docs/semantics.md](../../semantics.md).

### In-memory

`ExpressoFunctions.Max(collection, item => selector)` follows PostgreSQL `MAX`: NULL items are ignored, no values give NULL, and strings compare ordinally. Example: `eq(max(tags,score),100)` builds `e => ExpressoFunctions.Max(e.Tags, tags => (int?)tags.Score) != null && ExpressoFunctions.Max(e.Tags, tags => (int?)tags.Score).Value == p0`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server the subquery appears twice, once for the NULL check:

```sql
(SELECT MAX([w0].[Score]) FROM [widget_tag] AS [w0] WHERE [w].[Id] = [w0].[WidgetId]) IS NOT NULL
AND (SELECT MAX([w1].[Score]) FROM [widget_tag] AS [w1] WHERE [w].[Id] = [w1].[WidgetId]) = @__Value_0
```

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server it projects each subquery as a column of a derived table:

```sql
(SELECT MAX([Extent2].[Score]) AS [A1] FROM [dbo].[widget_tag] AS [Extent2] WHERE [Extent1].[Id] = [Extent2].[WidgetId]) AS [C1]
```

The filter then tests those columns: `WHERE ([Project2].[C1] IS NOT NULL) AND ([Project2].[C2] = @p__linq__0)`.

Every EF6 provider supports `max`.

## Notes

- See scalar [`max`](../arithmetic/max.md) and collection [`min`](min.md).
