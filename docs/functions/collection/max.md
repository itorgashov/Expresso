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

## Notes

- See scalar [`max`](../arithmetic/max.md) and collection [`min`](min.md).
