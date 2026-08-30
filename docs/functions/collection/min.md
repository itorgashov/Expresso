# `min` (collection)

Minimum of a selector expression over a related collection. The first argument must be a collection; otherwise the parser builds scalar [`min`](../arithmetic/min.md) (`MinFunc`).

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

- **Parser (collection overload):** first argument is a `CollectionRef`; one argument → `System.Exception`: `"Min() function should have 2 arguments."`
- **Parser (scalar overload):** first argument is not a collection; same arity error as arithmetic `min`.
- **IR construction** (`CollectionMinFunc`): illegal selector type → `ArgumentException("Illegal argument type", "selector")`. Constructed directly by the parser.

## SQL Server rendering

```sql
(SELECT MIN(selector) FROM {FromClause} WHERE {CorrelateSql})
```

Example: `gt(min(authors, dateofbirth), "1828-01-01")`.

## Notes

- Disambiguation is by the first argument, not by a different function name.
- See scalar [`min`](../arithmetic/min.md) and collection [`max`](max.md).
