# IT seed and case extensions (approved)

Extend the existing widget catalog only. No extra tables, no parser/error ITs, no empty-string notes, no midpoint `round`, no div0/sqrt-negative.

## Seed (`WidgetDdl`)

- Eve `amount` = `-12.7` (real `abs` / `sign`). Carol `amount` = `61.4` for `round(..., 1)` (avoid `.25` float/midpoint).
- Bob `notes` = `100Xoff` so unescaped `%` in `contains("100%")` would also match Bob.
- Frank `notes` = `a\b` for literal backslash LIKE.

`indexof-missing` uses `name` (not `notes`): SQL Server’s `CHARINDEX` remap turns NULL into `-1`, which would include Alice and break parity. `round-digits` uses a 61.3–61.5 window around Carol `61.4` because MySQL/DB2 reject exact `= 60.3` on FLOAT.

## Cases

New filter ids: `eq-code`, `eq-time-midnight`, `round-digits`, `addhours-neg`, `count-red`, `none-pred`, `indexof-missing`, `contains-underscore`, `contains-backslash`, `not-isnull`, `eq-datetime`.

Update `abs` / `sign` to use Eve’s negative amount.

New parent sort `sort-min-score`: `min(tags,score)` DESC. Optional `any(tags)` filter so Carol’s NULL `MIN` does not hit dialect NULLS FIRST/LAST.

## Harness

`ParentSortCase` optional `Filter`. `ParentSort_ReturnsExpectedIds` passes it into `QueryWidgetIds`.
