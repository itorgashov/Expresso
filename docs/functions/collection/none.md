# `none`

True if the related collection has no item matching the optional predicate. One argument means the collection is empty.

## Syntax

```text
none(collection)
none(collection, predicate)
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

- **Parser:** first argument is not a collection → `ArgumentException`: `"First argument of None() must be a collection."`
- **IR construction** (`NoneFunc`): same null/type rules as [`any`](any.md). Constructed directly by the parser.

Not valid as a sort key.

## SQL Server rendering

```sql
NOT EXISTS (SELECT 1 FROM {FromClause} WHERE {CorrelateSql} [AND predicate])
```

## Notes

- See [`any`](any.md) and [`all`](all.md).
