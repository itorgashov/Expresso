# `contains`

True if the string contains the given substring anywhere within it.

## Syntax

```text
contains(text, substring)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Contains() function should have 2 arguments."`
- **Parser coercion:** both arguments are coerced to `string` literals if they are quoted string tokens.
- **IR construction** (`StrContainsFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - Either argument's `ReturnType` is not `string` → `ArgumentException`

## SQL Server rendering

```sql
(text LIKE '%substring%' ESCAPE '\')
```

**If the substring is a string literal:** wildcard characters (`\`, `%`, `_`) are escaped in C# before parameterization (same order and rules as [`startswith`](startswith.md)), and `%` is added on **both** sides: `contains(title,"ar")` → parameter value `"%ar%"`. Example: `contains(title,"100%")` → parameter value `"%100\%%"` (the literal `%` is escaped so it isn't treated as a wildcard).

**If the substring is not a string literal:**

```sql
(text LIKE ('%' + REPLACE(REPLACE(REPLACE(substring, '\', '\\'), '%', '\%'), '_', '\_') + '%') ESCAPE '\')
```

## Notes

- See [`startswith`](startswith.md) and [`endswith`](endswith.md) for the other `LIKE`-based predicates.
