# Integration coverage matrix

Case ids from `RendererIntegrationCases`. Every dialect collection (SQL Server, PostgreSQL, SQLite, MySQL, MariaDB, Oracle, DB2 on net6.0) runs the **same** ids.

| Function / feature | Case id |
|---|---|
| and | `and` |
| or | `or` |
| not | `not` |
| eq | `eq`, `eq-bool`, `eq-guid`, `eq-time`, `eq-time-midnight`, `eq-code`, `eq-datetime` |
| neq | `neq` |
| gt / gte / lt / lte | `gt`, `gte`, `lt`, `lte` |
| in | `in` |
| isnull | `isnull`, `not-isnull` |
| abs / add / sub / mult / div / mod | `abs`, `add`, `sub`, `mult`, `div`, `mod` |
| floor / ceiling / round / sign / power / sqrt | `floor`, `ceiling`, `round`, `round-digits`, `sign`, `power`, `sqrt` |
| scalar min / max | `min`, `max` |
| startswith / endswith / contains | `startswith`, `endswith`, `contains`, `contains-underscore`, `contains-backslash` |
| substring / left / right / concat | `substring`, `left`, `right`, `concat` |
| lower / upper / trim / ltrim / rtrim / replace | `lower`, `upper`, `trim`, `ltrim`, `rtrim`, `replace` |
| len / indexof | `len`, `indexof`, `indexof-missing` |
| year … second, dayofweek, date, time | `year`, `month`, `day`, `dayofyear`, `hour`, `minute`, `second`, `dayofweek`, `date`, `time` |
| addyears … addseconds | `addyears`, `addmonths`, `adddays`, `addhours`, `addhours-neg`, `addminutes`, `addseconds` |
| any / none / all | `any-tag`, `any-empty-pred`, `none-tag`, `none-pred`, `all-pred` |
| count / min / max / sum / avg | `count-tags`, `count-red`, `min-score`, `max-score`, `sum-score`, `avg-score` |
| nested any | `nested-any` |
| parent sort | `sort-name-asc`, `sort-age-desc-name`, `sort-count-tags`, `sort-min-score`, `sort-bool` |
| nested collection sort | `nested-label-asc`, `nested-score-desc` |
| paging | `page-1-size-2`, `page-2-size-2`, `page-2-size-4`, `page-4-size-2`, `pagesize-only-3`, `skip-2-take-3`, `skip-only-4`, `take-only-2`, `skip-beyond`, `page-wins`, `page-without-size`, `page-only`, `page-age-desc`, `page-filter`, `page-count-tags` |

`sortfor` grammar is covered by parsing unit tests; IT uses `SortDirective` trees.

## LINQ / EF Core (net8.0) / EF6 (net48)

- **Catalog:** every EF Core collection (`{Engine}EfItTests`, all seven engines) runs the ids above and must return the expected ids. `NestedSort_IncludeSorted` also runs the nested cases through `IncludeSorted`.
- **EF6:** `{Engine}Ef6ItTests` (SQL Server, PostgreSQL, MySQL, MariaDB, SQLite, Oracle) run the catalog and the differential cases the same way. Cases listed in `Ef6ProviderGaps` for that provider must throw instead (skipped with the reason when the divergence is silent); see [docs/linq-rendering.md](../../../docs/linq-rendering.md#ef6-provider-limits).
- **POWER remediation:** shared differential cases cover representable subnormals, signed/field/nested/nullable operands, sort keys, and computed Oracle NUMBER overflow. SQLite EF6 additionally compares native ADO at binary and non-binary underflow boundaries and neighboring double inputs (`PowerUnderflowBoundary_*`).
- **Differential:** `RendererDifferentialCases` has no fixed expectation; EF Core must return the same outcome as ADO on the same engine (both failing counts as agreement). In-memory LINQ must match PostgreSQL ADO (all TFMs) on the catalog and on the differential cases that don't depend on collation.

| Split probed | Case id |
|---|---|
| integer vs decimal `/` | `div-odd-eq`, `div-odd-gt`, `sort-div` |
| integer `AVG` (SQL Server, DB2) | `avg-fraction` |
| rounding mode | `round-half`, `round-half-digits` |
| NULL in `concat` (incl. every argument NULL) | `concat-null-isnull`, `concat-null-eq`, `concat-null-all` |
| time-of-day arithmetic (wrap, carry, negative, compare) | `time-hour`, `time-wrap`, `time-carry`, `time-negative`, `time-seconds`, `time-add-eq` |
| `left` / `right` / `substring` past the end | `left-long`, `right-long`, `substring-long` |
| case-sensitive `indexof` | `indexof-case` |
| empty, trailing-space and NULL `indexof` | `indexof-empty`, `indexof-space`, `indexof-null` |
| case sensitivity of `contains` / `startswith` / `endswith` (`LIKE` vs `instr` / `CHARINDEX`) | `contains-case`, `startswith-case`, `endswith-case` (collation-dependent, EF only) |
| empty `contains` / `startswith` / `endswith` pattern | `contains-empty`, `startswith-empty`, `endswith-empty` |
| empty-collection `sum`, string `min` | `sum-empty-isnull`, `min-label` |
| NULL sort position | `sort-len-notes-asc`, `sort-len-notes-desc`, `sort-notes-asc`, `sort-notes-desc` (collation-dependent, EF only) |

## Projection (0.13.0)

`Projection_CompositeKeysAndNestedSparseShape` runs for every ADO, EF Core, and EF6 engine collection. Integration-owned `widget_projection_parent/child/leaf` fixtures use composite primary/foreign keys with repeated individual components. The case checks nested attachment, hidden key omission, empty collections, scalar-only responses, and selected nulls. SQL select-list and omitted-child-query assertions are also covered by the provider translation unit tests.

`Projection_Composition_PreservesCurrentGraphs` adds 16 private SQLite executions for `EF.Property` root predicates and nested mappings,
standard left joins, and collections carried by intermediate projections. Both EF execution entry points and inferred/explicit key scopes
verify composite attachment, duplicate/empty root rows, sparse output, and no tracked entities.

`Projection_Stages_PreserveActualRows` adds 64 private SQLite executions covering source-less generators, all nine reviewed EF wrappers,
saved predicates/selectors, both conditional branches and Unicode-equivalent field/collection names. `Projection_ReferencePaths_KeepMappedGraph`
adds 32 private SQLite executions for CLR/EF.Property reference/collection combinations in predicates and selected mappings. Both entry points
and key modes retain actual row/attachment semantics without tracking. Temporal/opaque saved expressions are verified by no-I/O unit preflight,
including selected deeper mappings and empty roots, rather than live historical database fixtures.

The middle entity also has an unselected non-comparable column: SQL Server XML, Oracle/DB2 CLOB, PostgreSQL JSON, or TEXT on other engines. Nested and default full projections must both execute without selecting that column or using entity DISTINCT. No sample schema is changed or executed.

EF6 TPH/TPT derived-source resolution, EF Core TPH, and generic-transformer/custom-executor first-level execution are pinned by genuine provider-translation unit tests without database I/O; this database matrix does not create inheritance tables.

`Projection_QueryFilters_RespectCallerScopeAtEveryLevel` adds 16 EF Core SQLite executions in private in-memory `widget_projection_*` tables.
Shadow boolean soft-delete filters cover parents, children, leaves, and all levels together, with retained/ignored filters and both the
adapter and direct-plan/custom-executor paths. Composite identities, an authorized paged root, explicit `keySource`, empty collections,
and actual descendant values are checked. SQL-only unit cases additionally verify filter presence/absence for five EF Core providers
and explicit/inferred key sources; their graph executor does not simulate global filters.

`Projection_FilteredOrderedPage_LoadsOnlyKnownKeysAndRequestedColumns` adds a filtered, ordered composite-key page, nested sorting, minimal output SELECTs, query counts, no child OFFSET, and invalid-selection rejection before I/O. Oracle EF6 retains an unused entity column inside its paging wrapper; the test pins this documented provider limitation and verifies projected outer columns and minimal child queries. The scalar-only case verifies exactly one query. Provider window-shift and incomplete-key regressions run without I/O in unit projects. SQL execution uses Common directly without a sample dependency.

Second-review regressions use only private in-memory SQLite `widget*` tables:

- `Projection_TextKeys_DatabaseMatchingAndAttachmentAgree`: six executions on each framework, covering actual `NOCASE`, `RTRIM`, and `BINARY` primary/foreign-key equality with single/composite projection identities. Configured mapping comparers retain mixed-case/padded relationships at two child levels; binary controls do not merge distinct parents. Duplicate rows, original key values, tenant isolation, and returned/attached counts are asserted.
- `Projection_ReusedOwnedTypes_LoadEachActualOwnership`: four EF Core executions, selecting one/both sibling owned navigations with reused child/leaf CLR types, full composite ownership keys, overlapping local ids, and ordinary/repeated owners. Actual materialization preserves sparse graphs without tracking or entity grouping.
- `Projection_GrandchildSort_KeepsParentOrdering`: two EF Core executions, verifying that a deepest-only sort preserves the intermediate default, and an explicit intermediate sort overrides it.

Temporal collection rejection is covered by no-I/O SQL Server unit tests for AsOf and all range modes, explicit/inferred key sources, and both execution paths. Scalar-only historical SQL and current-table temporal models are controls. This matrix does not claim historical graph execution support or create SQL Server temporal tables. Catalog-bounded default/bare selections are composed with broader SQL/LINQ mappings in unit tests; no database is needed for their select-list and sparse-shape assertions.
