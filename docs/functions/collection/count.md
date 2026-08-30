# `count`

Number of items in a related collection, optionally restricted by an item-scope predicate. Returns `int`. Distinct from [`len`](../string-inspect/len.md) (string length).

## Syntax

```text
count(collection)
count(collection, predicate)
```

1 or 2 arguments.

- **Category:** Collection aggregate
- **Return type:** `int`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `collection` | A collection name in the current `QueryModel` |
| 2 | `predicate` | `bool` (optional). Parsed against `collection.Items`. |

## Validation & exceptions

- **Parser:** first argument is not a collection → `ArgumentException`: `"First argument of Count() must be a collection."`
- **IR construction** (`CollectionCountFunc`): `collection` is `null` → `ArgumentNullException`; predicate present but not `bool` → `ArgumentException`. Constructed directly by the parser.

Allowed as a sort key (renders as a scalar subquery).

## SQL Server rendering

```sql
(SELECT COUNT(*) FROM {FromClause} WHERE {CorrelateSql} [AND predicate])
```

Example: `eq(count(authors), 2)`:

```sql
((SELECT COUNT(*) FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id) = @wparam_0)
```

## Notes

- See [`any`](any.md), [`min`](min.md), [`sum`](sum.md).
