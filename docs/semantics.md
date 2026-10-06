# Filter behavior and database differences

A filter or sort returns the same rows whichever renderer you use, with a few results that belong to the database. This page explains NULL handling, value types and collection rules, and lists the cases where the answer depends on your engine.

A filter means the same thing whether you render it to SQL, to a LINQ lambda for EF Core or EF6, or run it against a list in memory. NULL handling, type rules, rounding, and the empty-collection rules below hold everywhere. A small set of results belongs to the database and differs between engines; those are collected in [Where results depend on the database](#where-results-depend-on-the-database).

Look here when you ask:

- Why is a row with a NULL value missing from my results? See [NULL handling](#null-handling).
- Will this behave the same if I switch databases? See [Where results depend on the database](#where-results-depend-on-the-database).
- Which EF6 functions throw? See [EF6 limits](#ef6-limits).

The exact SQL or lambda for one function is on its [function page](functions/README.md).

## NULL handling

SQL has three outcomes for a condition: TRUE, FALSE, and UNKNOWN. UNKNOWN is what you get when a NULL is involved. **A filter returns a row only when the condition is TRUE.** An UNKNOWN row is dropped, just like a FALSE row.

That is why a comparison with a NULL field never matches, in either direction. For a row whose `name` is NULL:

| Filter | Row returned? | Why |
|---|---|---|
| `eq(name,"Ann")` | no | NULL compared with anything is UNKNOWN |
| `neq(name,"Ann")` | no | same |
| `not(eq(name,"Ann"))` | no | `not` leaves UNKNOWN as UNKNOWN |
| `isnull(name)` | yes | `isnull` is always TRUE or FALSE |
| `or(eq(name,"Ann"),isnull(name))` | yes | one TRUE side is enough |

To include rows with a NULL value, say so: `or(neq(name,"Ann"),isnull(name))`.

This holds for every comparison (`gt`, `in`, `startswith`, `contains`, and the others). It also holds under EF Core and EF6, whatever `UseRelationalNulls` or `UseDatabaseNullSemantics` is set to.

### Combining conditions

| `a` | `b` | `and(a,b)` | `or(a,b)` |
|---|---|---|---|
| TRUE | UNKNOWN | UNKNOWN | TRUE |
| FALSE | UNKNOWN | FALSE | UNKNOWN |
| UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN |

`and` is FALSE as soon as one argument is FALSE, and TRUE only when all are TRUE. `or` is TRUE as soon as one argument is TRUE, and FALSE only when all are FALSE. Every other combination is UNKNOWN.

### Functions that return values

A scalar function (`len`, `add`, `year`, and the rest) returns NULL when any argument is NULL. Some functions can also return NULL when arguments are non-NULL (for example `sqrt` of a negative value on SQLite). The in-memory profile counts string positions in Unicode code points (PostgreSQL `char_length` / `strpos` semantics), not UTF-16 code units.

- `concat` treats NULL as an empty string on engines whose `CONCAT` does that. See [`concat`](functions/string-transform/concat.md).
- Scalar `min(a, b)` and `max(a, b)` return `b` whenever the comparison is not TRUE. If `a` is NULL, the comparison is UNKNOWN, so `min(age,18)` is `18` for a row with no `age`, and so is `max(age,18)`. The result is NULL only when `b` is NULL.

### Collection conditions

`any`, `all`, and `none` are never UNKNOWN.

| Collection | `any(c,p)` | `all(c,p)` | `none(c,p)` |
|---|---|---|---|
| empty | FALSE | TRUE | TRUE |
| some item makes `p` TRUE | TRUE | depends on the other items | FALSE |
| every item makes `p` UNKNOWN | FALSE | TRUE | TRUE |

An item whose predicate is UNKNOWN is not a counterexample for `all`, and not a match for `any`. So `all(tags,gt(score,0))` still holds for a tag whose `score` is NULL.

## Values and types

Filters accept `string`, `bool`, `byte`, `int`, `double`, `DateTime`, `Guid`, and `TimeSpan` (a time of day). `DateOnly` and `TimeOnly` work too when you use the net6.0 build of the packages. Literal syntax is in [Query syntax](query-syntax.md).

- Numeric promotion: `byte` becomes `int`. If either side of an operation is `double`, both sides are `double`.
- Rounding: `round` rounds a halfway value away from zero on every engine: `round(2.5)` is `3`, and `round(-2.5)` is `-3`.
- Integer division: in a LINQ lambda (including EF Core, EF6, and in memory), dividing two integers truncates toward zero: `div(-7,2)` is `-3`. The SQL renderers emit `/`, and the engine decides the result; see [Where results depend on the database](#where-results-depend-on-the-database). To get a fractional result everywhere, use a `double` operand, such as `div(price,2.0)`.

## Parameters

A literal in the filter is never written into the SQL text. It is bound as a parameter, so a value cannot change the statement, and the database can reuse the query plan. The names you see in a log depend on the renderer: SQL uses `@prefix_0` (`:prefix_0` on Oracle, see [SQL rendering](rendering.md#parameters)), EF Core uses `@__Value_0`-style names, and EF6 uses `@p__linq__0`.

## Collections and sorting

- `count` of an empty collection is `0`, never NULL.
- `sum`, `min`, `max`, and `avg` skip NULL items. They return NULL when the collection is empty or every item is NULL.
- A boolean sort key sorts as `1` when TRUE and `0` otherwise, so `desc` puts the matches first.
- `sortfor` orders the rows of a child collection. It does not add a key to the parent `ORDER BY`. How you apply it depends on how you load children:
  - SQL: load the children in a second query and apply the nested sort to it.
  - EF Core: `IncludeSorted` loads each child collection in `sortfor` order.
  - EF6 has no filtered include. Order the child query with `OrderByNested`.

  See [`sortfor`](functions/collection/sortfor.md).

## Where results depend on the database

Expresso keeps the meaning of a filter the same across engines. It does not hide differences that belong to the database itself. These are the ones to know about when you move a filter or a sort between engines.

| Topic | SQL Server | PostgreSQL | MySQL / MariaDB | SQLite | Oracle | DB2 |
|---|---|---|---|---|---|---|
| NULL sort keys, ascending | first | last | first | first | last | last |
| Integer `/` integer | truncates | truncates | fraction | truncates | fraction | truncates |
| `avg` of an integer column | integer, truncated | fraction | fraction | fraction | fraction | integer, truncated |
| Time of day plus hours past 24:00 | wraps | wraps | does not wrap (`27:00`) | carries into the next day | carries into the next day | wraps |

The table describes the SQL renderers. The in-memory profile behaves like PostgreSQL in all four rows. With EF Core and EF6, the database still decides NULL ordering, and EF Core and EF6 reproduce the SQL renderer's `avg` truncation on SQL Server and DB2. Integer division is the exception: EF and in-memory lambdas always truncate. EF6 throws where it cannot match the engine; see [EF6 limits](#ef6-limits).

For time-of-day arithmetic, "wraps" means 17:00 plus 10 hours is 03:00. "Carries into the next day" means the result is a date-time, so `hour` returns 3, but the value does not equal a 03:00 time of day.

### Case and sort order of strings

Whether `"abc"` matches `"ABC"` depends on the column's collation. This applies to `eq`, `gt`, `startswith`, `endswith`, `contains`, and to string sorts. SQL Server and MySQL are case-insensitive with their default collations. In memory, strings compare by ordinal value, so they are case-sensitive, like PostgreSQL with the `C` collation.

SQLite is mixed. `startswith`, `endswith`, and `contains` use `LIKE` and ignore ASCII case, while `eq` is case-sensitive.

### Empty strings on Oracle

Oracle stores an empty string as NULL. An empty string literal used as a scalar argument, and `indexof` with an empty search string, are NULL, where other engines return `0` for `indexof`. `concat` is NULL only when every argument is NULL or empty, so `concat(name,"")` is NULL only when `name` is. `replace` is different: an empty search leaves the source unchanged, and an empty replacement deletes matches. The result is NULL when the source is NULL or the deletion leaves an empty string.

An empty substring result is NULL. `isnull(substring(name,1,0))` is TRUE for a non-NULL `name` in SQL and in EF Core and EF6, because the rendered `SUBSTR` is checked for NULL.

### Other engine notes

- DB2 `ORDER BY`: DB2 cannot sort by a correlated subquery. Put such a sort key in the `SELECT` list and order by that column. See [SQL rendering](rendering.md#collection-mapping).
- Oracle `DateOnly` and `TimeOnly` with EF Core: map `DateOnly` to `DATE` and `TimeOnly` to `INTERVAL DAY TO SECOND`. Text storage (`NVARCHAR2`) throws `NotSupportedException` for calendar and clock functions on those columns. `date` of a `DateTime` is `TRUNC` (a `DATE`); `time` of a `DateTime` is `value - TRUNC(value)`, an `INTERVAL DAY TO SECOND` that keeps fractional seconds. Comparison parameters convert `DateOnly` to `DateTime` and `TimeOnly` to `TimeSpan`. `year(date(...))` and `hour(time(...))` stay on those types.

## EF6 limits

EF6 providers translate fewer functions than the other renderers. When a provider cannot produce the same result as the SQL renderer, Expresso throws `NotSupportedException` while building the lambda, with the reason in the message. It never returns an approximation. The functions affected per provider are listed in [LINQ rendering](linq-rendering.md#ef6-provider-limits).

Two differences do not throw, so check them if you rely on those functions:

- `time()` precision: on SQL Server, EF6 keeps milliseconds, while the SQL renderer keeps 100-nanosecond precision. On MySQL and MariaDB, EF6 drops fractional seconds.
- MySQL 8 time-of-day comparisons: MySql.Data sends a `TimeSpan` parameter as `'0 hh:mm:ss.ffffff'`, so comparing a computed time with it matches no rows. This affects `time` and the time-of-day `addhours`, `addminutes`, and `addseconds`. MariaDB is not affected.
