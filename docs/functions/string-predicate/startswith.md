# `startswith`

True if the string starts with the given prefix.

## Syntax

```text
startswith(text, prefix)
```

Exactly 2 arguments.

- **Category:** String predicate
- **Return type:** `bool`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `testExpression` | `string` |
| 2 | `matchToExpression` | `string` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Startswith() function should have 2 arguments."`
- **Parser coercion:** both arguments are coerced to `string` literals if they are quoted string tokens (`CoerceToString`).
- **IR construction** (`StrStartswithFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - Either argument's `ReturnType` is not `string` → `ArgumentException`

## SQL Server rendering

Renders as a `LIKE ... ESCAPE '\'` predicate, always with a trailing `ESCAPE '\'` clause:

```sql
(text LIKE 'prefix%' ESCAPE '\')
```

**If the prefix is a string literal** (the common case — a quoted value in the query string), Expresso escapes SQL `LIKE` wildcard characters in the literal **before** binding it as a parameter, so they're matched literally rather than as wildcards:

- `\` → `\\`, then `%` → `\%`, then `_` → `\_` (in that order)
- The escaped value has `%` appended: `startswith(name,"Jo")` → parameter value `"Jo%"`
- `startswith(name,"a_b")` → parameter value `"a\_b%"` (the literal `_` is escaped so it doesn't act as a single-character wildcard)

**If the prefix is not a string literal** (e.g. a field or another function's result), the escaping is done in SQL itself:

```sql
(text LIKE (REPLACE(REPLACE(REPLACE(prefix, '\', '\\'), '%', '\%'), '_', '\_') + '%') ESCAPE '\')
```

## Notes

- See [`endswith`](endswith.md) and [`contains`](contains.md) for the other `LIKE`-based predicates — all three share the same escaping rules, differing only in where `%` is placed.
