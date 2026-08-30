# `sum`

Sum of a numeric selector over a related collection. Collection-only — there is no scalar `sum`.

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

- **Parser:** first argument is not a collection → `ArgumentException`: `"First argument of Sum() must be a collection."`
- **Parser arity:** one argument → `System.Exception`: `"Sum() function should have 2 arguments."`
- **IR construction** (`CollectionSumFunc`): non-numeric selector → `ArgumentException("Illegal argument type", "selector")`. Constructed directly by the parser.

## SQL Server rendering

```sql
(SELECT SUM(selector) FROM {FromClause} WHERE {CorrelateSql})
```

## Notes

- See [`avg`](avg.md) and [`count`](count.md).
