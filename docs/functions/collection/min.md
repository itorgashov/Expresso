# `min` (collection)

Returns the minimum of a selector expression over a related collection. If the first argument is not a collection, the call is parsed as the scalar [`min`](../arithmetic/min.md) instead.

## Syntax

```text
min(collection, selector)
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

- If the first argument is a collection and you pass only one argument, parsing throws `System.Exception`: `"Min() function should have 2 arguments."`
- If the first argument is not a collection, the call is parsed as scalar `min` and gives the same arity error as arithmetic `min`.
- A selector of an unsupported type throws `ArgumentException("Illegal argument type", "selector")`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`{FromClause}` and `{CorrelateSql}` come from `CollectionSqlMapping` (application-authored, so they may already be dialect-specific).

### All dialects

```sql
(SELECT MIN(selector) FROM {FromClause} WHERE {CorrelateSql})
```

Example: `gt(min(authors, dateofbirth), "1828-01-01")`.

DB2 rejects correlated references in `ORDER BY` scalar subqueries (`SQL0206N`). This fragment is valid in `WHERE` and in a `SELECT` list. For a sort key, select it in a derived table (or extra SELECT column) and order by that alias. See [docs/rendering.md](../../rendering.md).

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
collection.Min(item => (T?)selector)
```

`collection` is the navigation mapped with `LinqQueryMapping<T>.Collection(name, navigation, items)`, and `selector` is rendered against the `items` mapping and lifted to its nullable type (`int?`, `DateTime?`, and so on; a `string` stays `string`), without numeric promotion. NULL items are ignored, and the result is NULL when the collection has no non-NULL value. Example: `eq(min(tags,score),5)` builds `e => e.Tags.Min(tags => (int?)tags.Score) != null && e.Tags.Min(tags => (int?)tags.Score).Value == p0`, where `p0` is a captured parameter. As a sort key, `min(tags,score),desc` becomes `OrderByDescending(e => e.Tags.Min(tags => (int?)tags.Score))`. The minimum of a string selector follows the database collation; see [docs/semantics.md](../../semantics.md).

### In-memory

`ExpressoFunctions.Min(collection, item => selector)` follows PostgreSQL `MIN`: NULL items are ignored, no values give NULL, and strings compare ordinally. Example: `eq(min(tags,label),"blue")` builds `e => ExpressoFunctions.Min(e.Tags, tags => tags.Label) != null && ExpressoFunctions.Min(e.Tags, tags => tags.Label) == p0`.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQL Server the subquery appears twice, once for the NULL check:

```sql
(SELECT MIN([w0].[Score]) FROM [widget_tag] AS [w0] WHERE [w].[Id] = [w0].[WidgetId]) IS NOT NULL
AND (SELECT MIN([w1].[Score]) FROM [widget_tag] AS [w1] WHERE [w].[Id] = [w1].[WidgetId]) = @__Value_0
```

As a sort key the correlated subquery stays in `ORDER BY`, so DB2 rejects it with `SQL0206N` as described above.

No provider overrides.

## EF6 rendering

### All providers

EF6 translates the Queryable lambda. On SQL Server it projects each subquery as a column of a derived table:

```sql
(SELECT MIN([Extent2].[Score]) AS [A1] FROM [dbo].[widget_tag] AS [Extent2] WHERE [Extent1].[Id] = [Extent2].[WidgetId]) AS [C1]
```

The filter then tests those columns: `WHERE ([Project2].[C1] IS NOT NULL) AND ([Project2].[C2] = @p__linq__0)`.

Every EF6 provider supports `min`.

## Notes

- Disambiguation is by the first argument, not by a different function name.
- See scalar [`min`](../arithmetic/min.md) and collection [`max`](max.md).
