# Collection queries

Status: **complete**. Package version **0.7.0**.

## Scope

- Backend-neutral IR: `any` / `all` / `none`, `count`, collection `min` / `max` / `sum` / `avg`, nested collections.
- Parse-time `QueryModel` (names and CLR types only). Render-time `SqlQueryMapping` (SQL `FROM` + correlate).
- SQL Server renderer: portable `EXISTS` / `NOT EXISTS` / scalar aggregate subqueries.
- Sample: book `authors` collection. Nested collections in unit tests only.

## Not in this slice

EF Core / Mongo packages, other RDBMS renderers, `CROSS APPLY`, parent-scope fields inside collection predicates.
