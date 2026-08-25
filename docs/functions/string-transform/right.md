# `right`

Returns the rightmost N characters of a string.

## Syntax

```text
right(text, length)
```

Exactly 2 arguments.

- **Category:** String transform
- **Return type:** `string`

## Arguments

| Position | Name | Required type |
|---|---|---|
| 1 | `sourceString` | `string` |
| 2 | `length` | `int` |

## Validation & exceptions

- **Parser arity check:** argument count `!= 2` → `System.Exception`: `"Right() function should have 2 arguments."`
- **Parser coercion:** argument 1 coerced to `string`; argument 2 coerced to `int` if a literal token.
- **IR construction** (`RightFunc`):
  - Either argument is `null` → `ArgumentNullException`
  - `sourceString.ReturnType` is not `string` → `ArgumentException`
  - `length.ReturnType` is not `int` → `ArgumentException`

## SQL Server rendering

```sql
RIGHT(text, length)
```

Example: `eq(right(isbn,1),"3")` renders as `(RIGHT([isbn], @wparam_0) = @wparam_1)`.

## Notes

- See [`left`](left.md) for the mirror function, and [`substring`](substring.md) for arbitrary start positions.
