# `sortfor`

Sort-only construct for ordering related collections. **Not** an IR filter function and **not** valid in `filter=`.

## Syntax

```text
sortfor(collectionPath, expression),asc|desc
```

Exactly 2 arguments inside `sortfor`, then the direction token **outside** (same as scalar sort keys).

```text
year,desc
sortfor(authors, lastname),asc
sortfor(authors/awards, title),desc
```

- **Category:** Sort directive helper (not in the expression IR)
- **Return type:** N/A — fills `SortDirective.Nested`, not `Items`

## Arguments

| Position | Name | Description |
|---|---|---|
| 1 | `collectionPath` | One or more collection segments separated by `/` (e.g. `authors`, `authors/awards`). Must not start with `/`. |
| 2 | `expression` | Sort key parsed in the **item** catalog of the path's final collection (same scope rules as `any`). |

## Validation & exceptions

- **Sort parser:** not exactly 2 arguments → `ArgumentException`: `"sortfor() requires exactly 2 arguments."`
- **Sort parser:** empty path or leading `/` → `ArgumentException`
- **Sort parser:** unknown collection segment → `ArgumentException`: `"Illegal field name: '...'"`
- **Sort parser:** `CollectionRef` / `any`/`all`/`none` as the sort key → `ArgumentException` (collections cannot be sort keys)
- **Filter parser:** `sortfor(...)` anywhere in `filter=` → `ArgumentException`: `'sortfor' is only valid in a sort directive, not in a filter.`

## SQL Server rendering

Parent `RenderOrderByClause` uses **only** `SortDirective.Items`. Nested order is rendered by the host against the collection's `ItemFieldToColumn` map (same renderer, narrower mapping).

Boolean expressions in nested sort keys use `CASE WHEN … THEN 1 ELSE 0 END`. With `asc`, non-matches sort first; use `desc` for “matches first” (e.g. `gt(len(lastname),10),desc`).

## Notes

- Multiple `sortfor` calls with the same path append to that node's `Items` in appearance order.
- `SortDirective.RemoveDuplicates()` dedupes parent `Items` and each nested `Items` list separately; parent `year` and `sortfor(authors, year)` do not collapse.
- Empty parent `Items` with only `sortfor` is valid; omit parent `ORDER BY` when `Items.Count == 0`.

See also [docs/query-syntax.md](../../query-syntax.md).
