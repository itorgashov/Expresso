# Function reference

Every filter/sort function Expresso supports, grouped by semantic category. Function and field names are **case-insensitive** in query strings. See [docs/query-syntax.md](../query-syntax.md) for the overall grammar and literal rules, and [docs/error-handling.md](../error-handling.md) for the general exception model referenced from each page below.

## Logical

| Function | Description |
|---|---|
| [`and`](logical/and.md) | True if every argument is true |
| [`or`](logical/or.md) | True if any argument is true |
| [`not`](logical/not.md) | Negates a boolean argument |

## Comparison

| Function | Description |
|---|---|
| [`eq`](comparison/eq.md) | Equal to |
| [`neq`](comparison/neq.md) | Not equal to |
| [`gt`](comparison/gt.md) | Greater than |
| [`gte`](comparison/gte.md) | Greater than or equal to |
| [`lt`](comparison/lt.md) | Less than |
| [`lte`](comparison/lte.md) | Less than or equal to |

## Membership / null

| Function | Description |
|---|---|
| [`in`](membership-null/in.md) | True if the first argument equals any of the remaining arguments |
| [`isnull`](membership-null/isnull.md) | True if the argument is `NULL` |

## Arithmetic

| Function | Description |
|---|---|
| [`abs`](arithmetic/abs.md) | Absolute value |
| [`add`](arithmetic/add.md) | Addition |
| [`sub`](arithmetic/sub.md) | Subtraction |
| [`mult`](arithmetic/mult.md) | Multiplication |
| [`div`](arithmetic/div.md) | Division |

## String predicates (return `bool`)

| Function | Description |
|---|---|
| [`startswith`](string-predicate/startswith.md) | True if the string starts with the given prefix |
| [`endswith`](string-predicate/endswith.md) | True if the string ends with the given suffix |
| [`contains`](string-predicate/contains.md) | True if the string contains the given substring |

## String transforms (return `string`)

| Function | Description |
|---|---|
| [`substring`](string-transform/substring.md) | Extracts a substring (alias: `substr`) |
| [`left`](string-transform/left.md) | Leftmost N characters |
| [`right`](string-transform/right.md) | Rightmost N characters |
| [`concat`](string-transform/concat.md) | Concatenates two or more strings |
| [`lower`](string-transform/lower.md) | Lowercases a string |
| [`upper`](string-transform/upper.md) | Uppercases a string |
| [`trim`](string-transform/trim.md) | Trims leading and trailing whitespace |
| [`ltrim`](string-transform/ltrim.md) | Trims leading whitespace |
| [`rtrim`](string-transform/rtrim.md) | Trims trailing whitespace |
| [`replace`](string-transform/replace.md) | Replaces all occurrences of a substring |

## String inspection (return `int`)

| Function | Description |
|---|---|
| [`len`](string-inspect/len.md) | Length of a string |
| [`indexof`](string-inspect/indexof.md) | 0-based index of a substring, or `-1` if not found |
