# `sortfor`

Orders the items of a related collection. You can use `sortfor` only in a sort directive, not in `filter=`.

## Syntax

```text
sortfor(collectionPath, expression),asc|desc
```

`sortfor` takes exactly 2 arguments. The direction token goes outside the parentheses, as with scalar sort keys.

```text
year,desc
sortfor(authors, lastname),asc
sortfor(authors/awards, title),desc
```

- **Category:** Sort directive helper
- **Return type:** N/A. `sortfor` fills `SortDirective.Nested`, not `Items`.

## Arguments

| Position | Name | Description |
|---|---|---|
| 1 | `collectionPath` | One or more collection segments separated by `/` (for example `authors` or `authors/awards`). Must not start with `/`. |
| 2 | `expression` | Sort key, parsed against the item fields of the last collection in the path (same scope rules as `any`). |

## Validation & exceptions

- If you pass any number of arguments other than 2, parsing the sort throws `ArgumentException`: `"sortfor() requires exactly 2 arguments."`
- If the path is empty or starts with `/`, parsing the sort throws `ArgumentException`.
- If a path segment is not a known collection, parsing the sort throws `ArgumentException`: `"Illegal field name: '...'"`
- If the sort key is a collection or an `any`, `all` or `none` call, parsing the sort throws `ArgumentException`. Collections can't be sort keys.
- If `sortfor(...)` appears anywhere in `filter=`, parsing the filter throws `ArgumentException`: `'sortfor' is only valid in a sort directive, not in a filter.`

## SQL rendering

Quotes and bind names: [docs/rendering.md](../../rendering.md).

### All dialects

`sortfor` never appears in the parent `ORDER BY`. `RenderOrderByClause(sortDirective, ...)` reads only `SortDirective.Items`. A `sortfor(path, expr)` call is parsed into `SortDirective.Nested` instead, keyed by the first path segment (`CollectionSort.Name` and `.Directive`, recursively for multi-segment paths).

To turn a `sortfor` call into SQL, your application walks to the matching `SortDirective.Nested` entry and calls `RenderOrderByClause` again. You pass the nested directive together with the collection's own item field map (for example the item fields of an `items` collection: `label` maps to `i.label`). This returns a second, independent `ORDER BY` fragment. You apply it to the query that loads that related collection, typically a separate `SELECT` for the child rows rather than the parent's `SELECT`.

For `sort=name,desc,sortfor(items,label),asc`, the SQL Server output is:

- The parent `ORDER BY`, from `SortDirective.Items` and rendered against the outer field map:

  ```sql
  ORDER BY [p].[name] DESC
  ```

- The nested `ORDER BY` for the `items` collection, from `SortDirective.Nested["items"]` and rendered against that collection's item field map. You apply it to whichever query loads the related rows, together with the correlating key that groups them back to their parent:

  ```sql
  ORDER BY {parent_key}, [i].[label] ASC
  ```

When you load related rows in a second query, a common pattern is a helper that resolves `SortDirective.Nested` by path and calls `RenderOrderByClause` on the result. The helper falls back to a default `ORDER BY` when no `sortfor` targeted that collection.

Boolean expressions in nested sort keys use `CASE WHEN ... THEN 1 ELSE 0 END` on every dialect, the same as parent sort keys. With `asc`, non-matches sort first. Use `desc` to put matches first (for example `gt(len(label),10),desc`).

## LINQ rendering

Profiles and setup: [docs/linq-rendering.md](../../linq-rendering.md). Null logic and parameters: [docs/semantics.md](../../semantics.md).

### Queryable

```csharp
query.OrderBy(transformer, sort, mapping)
items.OrderByNested(transformer, sort, itemMapping, "items")
```

`BuildSortKeys` and `LinqQueryExtensions.OrderBy` read only `SortDirective.Items`, so `sortfor` never reaches the parent order. You order the query that loads the related rows with `OrderByNested`, which resolves `SortDirective.Nested` by path (case-insensitive, one argument per segment, for example `"authors", "awards"`) and applies `OrderBy`/`ThenBy` with keys built against the item mapping. It returns the items unchanged when no `sortfor` targeted that path.

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

- Multiple `sortfor` calls with the same path append to that node's `Items` in the order they appear.
- `SortDirective.RemoveDuplicates()` removes duplicates from the parent `Items` and from each nested `Items` list separately. A parent `year` and `sortfor(authors, year)` don't collapse into one.
- A sort with only `sortfor` calls is valid and leaves the parent `Items` empty. Omit the parent `ORDER BY` when `Items.Count == 0`.

See also [docs/query-syntax.md](../../query-syntax.md).
