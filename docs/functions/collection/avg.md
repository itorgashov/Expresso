# `avg`

Returns the average of a numeric selector over a related collection, always as a `double`. `avg` works only on collections; there is no scalar `avg`.

## Syntax

```text
avg(collection, selector)
```

Exactly 2 arguments.

- **Category:** Collection aggregate
- **Return type:** `double`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `collection` | A collection name in the current `QueryModel` |
| 2 | `selector` | Item-scope numeric expression (`byte`, `int`, or `double`) |

## Validation & exceptions

- If the first argument is not a collection, parsing throws `ArgumentException`: `"First argument of Avg() must be a collection."`
- If you pass only one argument, parsing throws `System.Exception`: `"Avg() function should have 2 arguments."`
- A non-numeric selector throws `ArgumentException("Illegal argument type", "selector")`.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`{FromClause}` and `{CorrelateSql}` come from `CollectionSqlMapping` (application-authored, so they may already be dialect-specific).

### All dialects

```sql
(SELECT AVG(selector) FROM {FromClause} WHERE {CorrelateSql})
```

DB2 rejects correlated references in `ORDER BY` scalar subqueries (`SQL0206N`). This fragment is valid in `WHERE` and in a `SELECT` list. For a sort key, select it in a derived table (or extra SELECT column) and order by that alias. See [docs/rendering.md](../../rendering.md).

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
collection.Average(item => (int?)selector)
```

`collection` is the navigation mapped with `LinqQueryMapping<T>.Collection(name, navigation, items)`, and `selector` is rendered against the `items` mapping. The result is a `double?`; a `byte` selector is widened to `int` first, and a `double` selector uses `double?`. NULL items are ignored, and the result is NULL when the collection has no non-NULL value. The average is fractional. SQL Server and DB2 compute `AVG` over an integer as an integer (engine-defined, see [docs/semantics.md](../../semantics.md)); only the EF Core and EF6 transformers reproduce that. Example: `eq(avg(tags,score),5.0)` builds `e => e.Tags.Average(tags => (int?)tags.Score) != null && e.Tags.Average(tags => (int?)tags.Score).Value == p0`, where `p0` is a captured parameter.

### In-memory

Same as Queryable.

## EF Core rendering

### All providers

EF Core translates the Queryable lambda. On SQLite:

```sql
(SELECT AVG(CAST("w1"."Score" AS REAL)) FROM "widget_tag" AS "w1" WHERE "w"."Id" = "w1"."WidgetId") = @__Value_0
```

### SQL Server, DB2

For an integer selector the transformer casts the average to `int?`, which truncates toward zero like the engine's integer `AVG`. A `double` selector is unchanged. On SQL Server:

```sql
CAST(CAST((SELECT AVG(CAST([w1].[Score] AS float)) FROM [widget_tag] AS [w1] WHERE [w].[Id] = [w1].[WidgetId]) AS int) AS float) = @__Value_0
```

## EF6 rendering

### All providers

EF6 translates the Queryable lambda.

### SQL Server

For an integer selector the transformer casts the average to `int?`, as in EF Core. EF6 projects the average `(SELECT AVG(CAST([Extent3].[Score] AS float)) ...)` as `[C2]` of a derived table and compares:

```sql
CAST(CAST([Project2].[C2] AS int) AS float) = @p__linq__0
```

Every EF6 provider supports `avg`.

## Notes

- See [`sum`](sum.md) and [`count`](count.md).
