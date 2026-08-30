# `any`

True if the related collection has at least one item matching the optional predicate. The predicate is parsed in the **item** catalog (fields of one related row), not the outer entity.

## Syntax

```text
any(collection)
any(collection, predicate)
```

1 or 2 arguments.

- **Category:** Collection quantifier
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `collection` | A collection name in the current `QueryModel` (becomes `CollectionRef`) |
| 2 | `predicate` | `bool` (optional). Parsed against `collection.Items`. |

## Validation & exceptions

- **Parser:** first argument is not a collection → `ArgumentException`: `"First argument of Any() must be a collection."`
- **Parser:** unknown collection/field name → `ArgumentException`: `"Illegal field name: '...'"`
- **IR construction** (`AnyFunc`): `collection` is `null` → `ArgumentNullException`; predicate present but not `bool` → `ArgumentException`. Constructed directly by the parser (not via reflection).

Not valid as a sort key (`ISortDirectiveParser` throws `ArgumentException`).

## SQL Server rendering

```sql
EXISTS (SELECT 1 FROM {FromClause} WHERE {CorrelateSql} [AND predicate])
```

`FromClause` and `CorrelateSql` come from `CollectionSqlMapping` (app-authored). Example: `any(authors, eq(displayname, "Leo Tolstoy"))` with the sample book mapping:

```sql
EXISTS (SELECT 1 FROM dbo.book_author AS ba INNER JOIN dbo.author AS a ON a.id = ba.author_id WHERE ba.book_id = b.id AND ([a].[display_name] = @wparam_0))
```

## Notes

- Inner identifiers do not see outer fields. Combine with an outer predicate: `and(gt(year, 2020), any(authors, eq(displayname, "Leo Tolstoy")))`.
- Nested collections: `any(authors, any(awards, eq(name, "Nobel Prize")))`.
- See [`all`](all.md), [`none`](none.md), and [`count`](count.md).
