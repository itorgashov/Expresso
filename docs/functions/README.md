# Function reference

This reference lists every function you can use in a `filter` or `sort` query string, grouped by category. Function and field names ignore case. For the overall grammar and literal rules, see [Query syntax](../query-syntax.md).

Each function page shows the syntax, the argument types, the exceptions you can expect, and how the function renders:

- SQL for every dialect, with engines that produce the same fragment grouped together. MariaDB uses the MySQL package and produces the same SQL as MySQL. Identifier quotes and parameter names are in [SQL rendering](../rendering.md).
- LINQ, as Queryable and in-memory lambdas. Setup is in [LINQ rendering](../linq-rendering.md).
- EF Core and EF6, including provider overrides and anything a provider cannot render exactly.

Rules that apply to every function, such as NULL logic, types and engine-defined behavior, are in [Filter behavior and database differences](../semantics.md). Exceptions are described in [Error handling](../error-handling.md).

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

## Membership and null

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
| [`mod`](arithmetic/mod.md) | Remainder of division |
| [`floor`](arithmetic/floor.md) | Rounds down to the nearest integer |
| [`ceiling`](arithmetic/ceiling.md) | Rounds up to the nearest integer (alias: `ceil`) |
| [`round`](arithmetic/round.md) | Rounds to a given number of decimal digits (one or two arguments) |
| [`sign`](arithmetic/sign.md) | `-1`, `0` or `1`, depending on the sign of the argument |
| [`power`](arithmetic/power.md) | Raises a number to a power (alias: `pow`) |
| [`sqrt`](arithmetic/sqrt.md) | Square root |
| [`min`](arithmetic/min.md) | Smaller of two arguments (for a collection, see [`min`](collection/min.md)) |
| [`max`](arithmetic/max.md) | Larger of two arguments (for a collection, see [`max`](collection/max.md)) |

## Collection quantifiers

Conditions inside a quantifier are checked against the fields of the collection's items, not the outer entity. See [Field providers](../field-providers.md).

| Function | Description |
|---|---|
| [`any`](collection/any.md) | True if at least one related item matches |
| [`all`](collection/all.md) | True if every related item matches (true when there are none) |
| [`none`](collection/none.md) | True if no related item matches |

## Collection aggregates

| Function | Description |
|---|---|
| [`count`](collection/count.md) | Number of related items (`int`) |
| [`min`](collection/min.md) | Smallest value of an item field |
| [`max`](collection/max.md) | Largest value of an item field |
| [`sum`](collection/sum.md) | Sum of a numeric item field |
| [`avg`](collection/avg.md) | Average of a numeric item field (`double`) |

## Collection sort

| Construct | Description |
|---|---|
| [`sortfor`](collection/sortfor.md) | Orders related rows by an item expression (in `sort=` only, not in a filter) |

## String predicates

These functions return `bool`.

| Function | Description |
|---|---|
| [`startswith`](string-predicate/startswith.md) | True if the string starts with the given prefix |
| [`endswith`](string-predicate/endswith.md) | True if the string ends with the given suffix |
| [`contains`](string-predicate/contains.md) | True if the string contains the given substring |

## String transforms

These functions return `string`.

| Function | Description |
|---|---|
| [`substring`](string-transform/substring.md) | Extracts a substring (alias: `substr`) |
| [`left`](string-transform/left.md) | Leftmost characters |
| [`right`](string-transform/right.md) | Rightmost characters |
| [`concat`](string-transform/concat.md) | Joins two or more strings |
| [`lower`](string-transform/lower.md) | Converts to lower case |
| [`upper`](string-transform/upper.md) | Converts to upper case |
| [`trim`](string-transform/trim.md) | Removes leading and trailing whitespace |
| [`ltrim`](string-transform/ltrim.md) | Removes leading whitespace |
| [`rtrim`](string-transform/rtrim.md) | Removes trailing whitespace |
| [`replace`](string-transform/replace.md) | Replaces all occurrences of a substring |

## String inspection

These functions return `int`.

| Function | Description |
|---|---|
| [`len`](string-inspect/len.md) | Length of a string |
| [`indexof`](string-inspect/indexof.md) | Zero-based index of a substring, or `-1` if not found |

## Date and time getters

| Function | Description | Return type |
|---|---|---|
| [`year`](datetime-getter/year.md) | Calendar year | `int` |
| [`month`](datetime-getter/month.md) | Month (1 to 12) | `int` |
| [`day`](datetime-getter/day.md) | Day of month (1 to 31) | `int` |
| [`dayofyear`](datetime-getter/dayofyear.md) | Day of year (1 to 366) | `int` |
| [`hour`](datetime-getter/hour.md) | Hour (0 to 23) | `int` |
| [`minute`](datetime-getter/minute.md) | Minute (0 to 59) | `int` |
| [`second`](datetime-getter/second.md) | Second (0 to 59) | `int` |
| [`dayofweek`](datetime-getter/dayofweek.md) | Day of week, from `Sunday=0` to `Saturday=6` | `int` |
| [`date`](datetime-getter/date.md) | Calendar date part | `DateOnly` (net6.0) or `DateTime` (netstandard2.0) |
| [`time`](datetime-getter/time.md) | Time-of-day part | `TimeOnly` (net6.0) or `TimeSpan` (netstandard2.0) |

## Date and time arithmetic

The return type matches the first argument: `DateTime`, `DateOnly` (calendar `add*` functions), `TimeOnly` (time `add*` functions on net6.0), or `TimeSpan` (time `add*` functions on netstandard2.0).

| Function | Description |
|---|---|
| [`addyears`](datetime-add/addyears.md) | Adds or subtracts whole years |
| [`addmonths`](datetime-add/addmonths.md) | Adds or subtracts whole months |
| [`adddays`](datetime-add/adddays.md) | Adds or subtracts whole days |
| [`addhours`](datetime-add/addhours.md) | Adds or subtracts whole hours |
| [`addminutes`](datetime-add/addminutes.md) | Adds or subtracts whole minutes |
| [`addseconds`](datetime-add/addseconds.md) | Adds or subtracts whole seconds |

Every `add*` function takes an `int` amount. Zero and negative values are allowed: `adddays(createdat,-7)` goes back one week.
