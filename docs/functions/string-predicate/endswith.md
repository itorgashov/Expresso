# `endswith`

True if the string ends with the given suffix.

## Syntax

```text
endswith(text, suffix)
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

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Endswith() function should have 2 arguments."`
- **Parser coercion:** both arguments are coerced to `string` literals if they are quoted string tokens.
- **IR construction** (`StrEndswithFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - Either argument's `ReturnType` is not `string` → `ArgumentException`

## SQL Server rendering

```sql
(text LIKE '%suffix' ESCAPE '\')
```

**If the suffix is a string literal:** wildcard characters (`\`, `%`, `_`) are escaped in C# before parameterization (same order and rules as [`startswith`](startswith.md)), and `%` is **prepended**: `endswith(name,"hn")` → parameter value `"%hn"`.

**If the suffix is not a string literal:**

```sql
(text LIKE ('%' + REPLACE(REPLACE(REPLACE(suffix, '\', '\\'), '%', '\%'), '_', '\_')) ESCAPE '\')
```

## Notes

- See [`startswith`](startswith.md) and [`contains`](contains.md) for the other `LIKE`-based predicates.
