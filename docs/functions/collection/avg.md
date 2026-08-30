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

## SQL Server rendering

```sql
(SELECT AVG(selector) FROM {FromClause} WHERE {CorrelateSql})
```

## Notes

- See [`sum`](sum.md) and [`count`](count.md).
