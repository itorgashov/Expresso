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

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).
`{FromClause}` and `{CorrelateSql}` come from `CollectionSqlMapping` (application-authored, so they may already be dialect-specific).

### All dialects

```sql
(SELECT COUNT(*) FROM {FromClause} WHERE {CorrelateSql} [AND predicate])
```

Example: `eq(count(authors), 2)`:

```sql
((SELECT COUNT(*) FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id) = @wparam_0)
```

**DB2 / `ORDER BY`:** DB2 rejects correlated references in `ORDER BY` scalar subqueries (`SQL0206N`). This fragment is valid in `WHERE` and in a `SELECT` list. For a sort key, select it in a derived table (or extra SELECT column) and order by that alias. See [docs/rendering.md](../../rendering.md).

## Notes

- See [`any`](any.md), [`min`](min.md), [`sum`](sum.md).
