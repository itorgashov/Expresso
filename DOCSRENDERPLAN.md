# Dialect function-SQL docs (approved)

Replace per-function **SQL Server rendering** with **SQL rendering** grouped by engines that emit the same fragment. MariaDB is always listed with MySQL. Quotes and bind names stay on [docs/rendering.md](docs/rendering.md) (lean examples: one template + pointer, not a quote table on every page).

Source of truth for function SQL: transformer hooks + `DialectSql` goldens in `test/Rendering/Expresso.Rendering.TestCases` / per-dialect `*RendererParityTests`.

## Files

- Every `docs/functions/**/*.md` except the index: rename section, fill grouped SQL.
- [docs/functions/README.md](docs/functions/README.md), [docs/rendering.md](docs/rendering.md), [CONTEXT.md](CONTEXT.md) template line.

## Grouping

See the inventory in the chat approval: portable operators as **All dialects**; `mod`/`ceiling`/`round`/`LIKE`/`len`/`indexof`/`concat`/`left`/`right`/`substring`/date parts/`add*` grouped by spelling. Collection aggregates add the DB2 `ORDER BY` correlation note.
