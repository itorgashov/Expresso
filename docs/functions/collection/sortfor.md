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

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

`sortfor` never appears in the parent `ORDER BY`. `RenderOrderByClause(sortDirective, ...)` only reads `SortDirective.Items`; a `sortfor(path, expr)` call is parsed into `SortDirective.Nested` instead, keyed by the first path segment (`CollectionSort.Name` / `.Directive`, recursively for multi-segment paths).

To turn a `sortfor` call into SQL, the **host** — not the parent query — walks to the matching `SortDirective.Nested` entry and calls `RenderOrderByClause` **again**, passing that nested directive with the collection's own **item** field map (e.g. an `items` collection's item catalog: `label` → `i.label`). This returns a second, independent `ORDER BY` fragment that the host applies to whatever query loads that related collection — typically a separate `SELECT` for the child rows, not the parent's `SELECT`.

For `sort=name,desc,sortfor(items,label),asc`:

- Parent `ORDER BY` (from `SortDirective.Items`, rendered against the outer field map):

  ```sql
  ORDER BY [p].[name] DESC
  ```

- Nested `ORDER BY` for the `items` collection (from `SortDirective.Nested["items"]`, rendered against that collection's **item** field map) is applied to whichever query loads the related rows, alongside the correlating key used to group them back to their parent:

  ```sql
  ORDER BY {parent_key}, [i].[label] ASC
  ```

A helper that resolves `SortDirective.Nested` by path and calls `RenderOrderByClause` on the result (falling back to a default `ORDER BY` when no `sortfor` targeted that collection) is a common pattern for hosts loading related rows in a second query.

Boolean expressions in nested sort keys use `CASE WHEN ... THEN 1 ELSE 0 END` on every dialect, same as parent sort keys. With `asc`, non-matches sort first; use `desc` for "matches first" (e.g. `gt(len(label),10),desc`).

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
query.OrderBy(transformer, sort, mapping)
items.OrderByNested(transformer, sort, itemMapping, "items")
```

`BuildSortKeys` and `LinqQueryExtensions.OrderBy` read only `SortDirective.Items`, so `sortfor` never reaches the parent order. The host orders the query that loads the related rows with `OrderByNested`, which resolves `SortDirective.Nested` by path (case-insensitive, one argument per segment, for example `"authors", "awards"`) and applies `OrderBy`/`ThenBy` with keys built against the item mapping. It returns the items unchanged when no `sortfor` targeted that path.

A scalar key is the nullable value, and a boolean key is `condition ? 1 : 0`. NULL placement is the database's; see [docs/semantics.md](../../semantics.md). Example: `sort=name,desc,sortfor(items,label),asc` gives `OrderByDescending(e => e.Name)` on the parent query and `OrderBy(e => e.Label)` on the items query. `BuildSortKeys` throws `ArgumentException` for a directive without items, so skip the parent `OrderBy` when the sort has only `sortfor` calls.

### In-memory

`OrderByNested` on an `IEnumerable<TItem>` compiles the keys and orders with `InMemorySortComparer`, which follows PostgreSQL: NULL sorts last ascending and first descending, and strings compare ordinally.

## EF Core rendering

### All providers

`query.IncludeSorted(transformer, sort, mapping)` adds one filtered `Include(e => e.Nav.OrderBy(...).ThenBy(...))` per nested directive, with `ThenInclude` for deeper paths, so the loaded collections arrive in directive order. Parent keys are not applied; order the parents with `OrderBy`. On SQLite, `sortfor(tags,label),asc` loads the tags through a `LEFT JOIN "widget_tag" AS "w0"` and orders by the parent key first:

```sql
ORDER BY "w"."Id", "w0"."Label"
```

A collection mapped to anything other than a navigation property (`e => e.Nav`) throws `NotSupportedException`; order a child query with `OrderByNested` instead. A nested name without a collection mapping throws `ArgumentException`.

No provider overrides.

## EF6 rendering

### All providers

EF6 `Include` cannot filter or order a collection, so there is no `IncludeSorted`. Load the related rows with a separate child query and order it with the `IQueryable<TItem>` overload of `OrderByNested`, for example `context.Tags.Where(t => t.WidgetId == id).OrderByNested(transformer, sort, tagMapping, "tags")`. Parent keys use `OrderBy` as usual.

Every EF6 provider supports `sortfor`.

## Notes

- Multiple `sortfor` calls with the same path append to that node's `Items` in appearance order.
- `SortDirective.RemoveDuplicates()` dedupes parent `Items` and each nested `Items` list separately; parent `year` and `sortfor(authors, year)` do not collapse.
- Empty parent `Items` with only `sortfor` is valid; omit parent `ORDER BY` when `Items.Count == 0`.

See also [docs/query-syntax.md](../../query-syntax.md).
