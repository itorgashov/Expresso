# `all`

True if every item in the related collection matches the predicate. An empty collection is treated as vacuously true. A one-argument call (`all(authors)`) has no item constraint and always renders as true.

## Syntax

```text
all(collection)
all(collection, predicate)
```

1 or 2 arguments.

- **Category:** Collection quantifier
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `collection` | A collection name in the current `QueryModel` |
| 2 | `predicate` | `bool` (optional). Parsed against `collection.Items`. |

## Validation & exceptions

- **Parser:** first argument is not a collection → `ArgumentException`: `"First argument of All() must be a collection."`
- **IR construction** (`AllFunc`): same null/type rules as [`any`](any.md). Constructed directly by the parser.

Not valid as a sort key.

## SQL Server rendering

No predicate:

```sql
(1 = 1)
```

With predicate (portable “no counterexample” form):

```sql
NOT EXISTS (SELECT 1 FROM {FromClause} WHERE {CorrelateSql} AND NOT (predicate))
```

## Notes

- Vacuous truth: `all(authors, pred)` is true when there are no related rows.
- See [`any`](any.md) and [`none`](none.md).
