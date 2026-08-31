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
| [`mod`](arithmetic/mod.md) | Remainder of division |
| [`floor`](arithmetic/floor.md) | Rounds down to the nearest integer |
| [`ceiling`](arithmetic/ceiling.md) | Rounds up to the nearest integer (alias: `ceil`) |
| [`round`](arithmetic/round.md) | Rounds to a given number of decimal digits (alias-free; 1 or 2 args) |
| [`sign`](arithmetic/sign.md) | `-1`, `0`, or `1` depending on the argument's sign |
| [`power`](arithmetic/power.md) | Raises to a power (alias: `pow`) |
| [`sqrt`](arithmetic/sqrt.md) | Square root |
| [`min`](arithmetic/min.md) | Smaller of two arguments (scalar; collection overload: [`min`](collection/min.md)) |
| [`max`](arithmetic/max.md) | Larger of two arguments (scalar; collection overload: [`max`](collection/max.md)) |

## Collection quantifiers

Item predicates are parsed against the nested `QueryModel` of the collection, not the outer entity. See [docs/field-providers.md](../field-providers.md).

| Function | Description |
|---|---|
| [`any`](collection/any.md) | True if at least one related item matches |
| [`all`](collection/all.md) | True if every related item matches (vacuous if empty) |
| [`none`](collection/none.md) | True if no related item matches |

## Collection aggregates

| Function | Description |
|---|---|
| [`count`](collection/count.md) | Number of related items (`int`) |
| [`min`](collection/min.md) | Minimum of an item-scope selector |
| [`max`](collection/max.md) | Maximum of an item-scope selector |
| [`sum`](collection/sum.md) | Sum of a numeric item-scope selector |
| [`avg`](collection/avg.md) | Average of a numeric item-scope selector (`double`) |

## Collection sort (sort-only)

| Construct | Description |
|---|---|
| [`sortfor`](collection/sortfor.md) | Order related rows by an item-scope expression (`sort=` only; not a filter function) |

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

## DateTime getters

| Function | Description | Return type |
|---|---|---|
| [`year`](datetime-getter/year.md) | Calendar year | `int` |
| [`month`](datetime-getter/month.md) | Month (1–12) | `int` |
| [`day`](datetime-getter/day.md) | Day of month (1–31) | `int` |
| [`dayofyear`](datetime-getter/dayofyear.md) | Day of year (1–366) | `int` |
| [`hour`](datetime-getter/hour.md) | Hour (0–23) | `int` |
| [`minute`](datetime-getter/minute.md) | Minute (0–59) | `int` |
| [`second`](datetime-getter/second.md) | Second (0–59) | `int` |
| [`dayofweek`](datetime-getter/dayofweek.md) | Day of week, `Sunday=0` … `Saturday=6` | `int` |
| [`date`](datetime-getter/date.md) | SQL `CAST` to calendar date | `DateOnly` (net6.0) / `DateTime` (netstandard2.0) |
| [`time`](datetime-getter/time.md) | SQL `CAST` to time-of-day | `TimeOnly` (net6.0) / `TimeSpan` (netstandard2.0) |

## DateTime arithmetic

Return type matches the first argument: `DateTime`, `DateOnly` (calendar `add*`), `TimeOnly` (time `add*` on net6.0), or `TimeSpan` (time `add*` on netstandard2.0).

| Function | Description |
|---|---|
| [`addyears`](datetime-add/addyears.md) | Adds/subtracts whole years |
| [`addmonths`](datetime-add/addmonths.md) | Adds/subtracts whole months |
| [`adddays`](datetime-add/adddays.md) | Adds/subtracts whole days |
| [`addhours`](datetime-add/addhours.md) | Adds/subtracts whole hours |
| [`addminutes`](datetime-add/addminutes.md) | Adds/subtracts whole minutes |
| [`addseconds`](datetime-add/addseconds.md) | Adds/subtracts whole seconds |

All `add*` functions take an `int` amount; zero and negative values are allowed (e.g. `adddays(createdat,-7)`).
