# SQL rendering

The SQL renderers turn a `FilterCriteria` and a `SortDirective` into parameterized `WHERE` and `ORDER BY` fragments. Each dialect package sets the identifier quoting, the parameter names and the spelling of each function. This page lists those differences. To use a renderer, see [Render to SQL](getting-started-sql.md).

Each [function page](functions/README.md) shows the SQL for every dialect and groups the engines that produce the same fragment. MariaDB uses the MySQL package and produces the same SQL as MySQL.

For LINQ, EF Core and EF6, see [LINQ rendering](linq-rendering.md). The rules shared by every renderer, such as NULL logic, types and engine-defined behavior, are in [Filter behavior and database differences](semantics.md).

Renderers never connect to a database. Reference an ADO.NET provider in your application and install any native client it needs on the host. For DB2, including the .NET Framework and .NET 6+ drivers, see [Database clients](packages.md#database-clients-not-included).

## Compare dialects for one filter

For the filter `and(gt(age,30),startswith(name,"Jo"))`, a field map that sends `age` to `p.age` and `name` to `p.name`, and the parameter prefix `wparam`:

| Dialect | Rendered fragment |
|---|---|
| SQL Server | `(([p].[age] > @wparam_0) AND ([p].[name] LIKE @wparam_1 ESCAPE '\'))` |
| PostgreSQL | `(("p"."age" > @wparam_0) AND ("p"."name" LIKE @wparam_1 ESCAPE '\'))` |
| MySQL, MariaDB | ``((`p`.`age` > @wparam_0) AND (`p`.`name` LIKE @wparam_1 ESCAPE '\\'))`` |
| Oracle | `(("p"."age" > :wparam_0) AND ("p"."name" LIKE :wparam_1 ESCAPE '\'))` |

The parameter `@wparam_1` is bound to `Jo%`, with the wildcard added in C#. SQLite and DB2 render like PostgreSQL. The [startswith](functions/string-predicate/startswith.md) page shows the forms for a non-literal prefix.

## Identifier quoting

| Dialect | `p.age` is rendered as |
|---|---|
| SQL Server | `[p].[age]` |
| PostgreSQL, SQLite, Oracle, DB2 | `"p"."age"` |
| MySQL, MariaDB | `` `p`.`age` `` |

## Parameters

| Dialect | Name shape | Example for prefix `wparam` |
|---|---|---|
| SQL Server, PostgreSQL, SQLite, MySQL, MariaDB, DB2 | `@prefix_0` | `@wparam_0` |
| Oracle | `:prefix_0` | `:wparam_0` |

The parameter dictionary uses these names as keys, including the `@` or `:`.

## Collection mapping

Collection functions (`any`, `all`, `none` and the aggregates) render as portable `EXISTS` and scalar subqueries. You write `FromClause` and `CorrelateSql` on `CollectionSqlMapping` yourself, so they can use your dialect's SQL.

DB2 rejects a correlated reference inside a scalar subquery in `ORDER BY` (`SQL0206N`). The same `count(tags)` fragment is valid in a `WHERE` clause and in a `SELECT` list. To sort by it on DB2, select the fragment in a derived table or an extra column, and sort by the alias.

## Sort keys

A boolean sort key renders as `CASE WHEN … THEN 1 ELSE 0 END` on every dialect. The parent `ORDER BY` does not include a nested `sortfor` key. To order a child collection, call `RenderOrderByClause` again with the child `SortDirective` and the item mapping.
