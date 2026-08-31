# String functions extension — Plan

## Status

Implemented: portable IR + parser + SQL Server renderer for the string function set below. Package version **0.2.0**.

## Functions

| Function | Args | Return | Notes |
|---|---|---|---|
| `substring` / `substr` | `(s, start, length)` | string | `substr` is a parser alias |
| `left` / `right` | `(s, n)` | string | |
| `concat` | `(a, b, …)` min 2 | string | SQL Server 2012+ `CONCAT` |
| `startswith` / `endswith` / `contains` | `(s, pattern)` | bool | LIKE with `%`/`_`/`\` escaped; `ESCAPE '\'` |
| `lower` / `upper` | `(s)` | string | |
| `trim` / `ltrim` / `rtrim` | `(s)` | string | SQL Server `LEN` does not count trailing spaces |
| `len` | `(s)` | int | |
| `replace` | `(s, old, new)` | string | |
| `indexof` | `(s, find)` | int | 0-based; **-1** if not found (`CHARINDEX` translated) |

## LIKE escaping

String literals used as `startswith` / `endswith` / `contains` patterns escape `\`, `%`, and `_`. The full LIKE pattern (including `%` wildcards) is a single SQL parameter.

## Future SQL dialects

Same IR. Map `indexof` to `POSITION`/`LOCATE` but keep 0-based / -1 at the Expresso API. Map `concat` to `||` or dialect `CONCAT`. Map `len` to `LENGTH`/`CHAR_LENGTH` as appropriate.

## Out of scope

PostgreSQL/MySQL/SQLite/Oracle renderer packages, collection filters, regex.
