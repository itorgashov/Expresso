# Nested collection sort (`sortfor`)

Status: **complete**. Package version **0.8.0**.

## Scope

- Sort-only `sortfor(collectionPath, expression)` in `sort=`; fills `SortDirective.Nested` tree (parent `Items` unchanged).
- Path syntax: `authors`, `authors/awards` (no leading `/`, no `sortfor(/, …)` sugar).
- Backward compatible: 1-arg `SortDirective` ctor; parent `RenderOrderByClause` uses `Items` only.
- Sample: `dbo.award`, composite `Author`/`Award` on books and authors APIs, child `ORDER BY` from `Nested`.

## Not in this slice

- `sortfor` in `filter=` (rejected with dedicated error).
- OData-style path syntax in filters.
