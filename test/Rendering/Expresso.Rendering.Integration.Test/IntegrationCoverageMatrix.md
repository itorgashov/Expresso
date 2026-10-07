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
