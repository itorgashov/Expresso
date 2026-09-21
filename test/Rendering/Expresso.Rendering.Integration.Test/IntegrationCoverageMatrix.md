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
