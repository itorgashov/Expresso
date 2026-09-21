# SQL rendering

Expresso renderers turn a `FilterCriteria` / `SortDirective` into parameterized `WHERE` / `ORDER BY` fragments. The walker lives in `Expresso.Rendering.Common`; each dialect package overrides quoting, bind names, and function spellings.

Each function page under [docs/functions/](functions/README.md) documents dialect SQL, grouping engines that emit the same fragment. MariaDB uses the MySQL package and the same SQL as MySQL.

## Identifier quoting

| Dialect | Example `p.age` |
|---|---|
| SQL Server | `[p].[age]` |
| PostgreSQL, SQLite, Oracle, DB2 | `"p"."age"` |
| MySQL / MariaDB | `` `p`.`age` `` |

## Parameters

| Dialect | Name shape |
|---|---|
| SQL Server, PostgreSQL, SQLite, MySQL, DB2 | `@prefix_0` |
| Oracle | `:prefix_0` |

## Collection mapping

Collection `any` / `all` / `none` / aggregates use portable `EXISTS` and scalar subqueries. `FromClause` and `CorrelateSql` on `CollectionSqlMapping` are **application-authored** (dialect-specific SQL is allowed there).

DB2 rejects correlated references inside `ORDER BY` scalar subqueries (`SQL0206N`). The same `count(tags)` fragment is valid in `WHERE` and in a `SELECT` list. For `ORDER BY`, hosts should select that fragment (derived table or extra SELECT column) and sort by the alias. IT does this wrap only for DB2.

## Sort keys

Boolean sort keys render as `CASE WHEN … THEN 1 ELSE 0 END` on every dialect. Nested `sortfor` is not rendered on the parent `ORDER BY`; hosts pass a child `SortDirective` into `RenderOrderByClause` with the item mapping.

## Integration tests

Local Docker engines and the exhaustive case catalog: [test/Rendering/Expresso.Rendering.Integration.Test/README.md](../test/Rendering/Expresso.Rendering.Integration.Test/README.md).
