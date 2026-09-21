# `avg`

Average of a numeric selector over a related collection. Collection-only — there is no scalar `avg`. Return type is always `double`.

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

- **Parser:** first argument is not a collection → `ArgumentException`: `"First argument of Avg() must be a collection."`
- **Parser arity:** one argument → `System.Exception`: `"Avg() function should have 2 arguments."`
- **IR construction** (`CollectionAvgFunc`): non-numeric selector → `ArgumentException("Illegal argument type", "selector")`. Constructed directly by the parser.

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`{FromClause}` and `{CorrelateSql}` come from `CollectionSqlMapping` (application-authored, so they may already be dialect-specific).

### All dialects

```sql
(SELECT AVG(selector) FROM {FromClause} WHERE {CorrelateSql})
```

**DB2 / `ORDER BY`:** DB2 rejects correlated references in `ORDER BY` scalar subqueries (`SQL0206N`). This fragment is valid in `WHERE` and in a `SELECT` list. For a sort key, select it in a derived table (or extra SELECT column) and order by that alias. See [docs/rendering.md](../../rendering.md).

## Notes

- See [`sum`](sum.md) and [`count`](count.md).
