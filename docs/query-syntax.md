# Query syntax

The filter and sort grammar defines how to write predicates, order related rows, and express literals of each type. Result limits are independent of this grammar; see [Pagination](pagination.md) for paged and offset/number semantics.

## Filter grammar

A filter is one function call, which can contain more function calls:

```text
functionName(arg1, arg2, ...)
```

- The root must be a boolean function, such as `eq`, `and` or `startswith`. If it is not, `IFilterParser.Parse` throws `ArgumentException("A boolean expression is expected.")`. See [Error handling](error-handling.md).
- An argument can be a field name, a quoted literal, an unquoted numeric literal, or another function call. You can nest as deeply as you like: `eq(abs(age), 1)` or `and(gt(age,25), startswith(name,"Jo"))`.
- Function names and field names ignore case. `startswith`, `StartsWith` and `STARTSWITH` are the same.
- A field name must match `^[a-zA-Z_][a-zA-Z0-9_]*$` and appear in the field catalog you supply (see [Field providers](field-providers.md)). Any other name throws `ArgumentException: Illegal field name: '...'`.

The [function reference](functions/README.md) lists every supported function by category.

## Collections

A collection function tests a related set of rows, such as the authors of a book. Pass a `QueryModel` with nested `CollectionModel`s to `IFilterParser.Parse`. Inside `any(authors, …)`, names resolve against the authors item catalog.

```text
any(authors, eq(displayname, "Leo Tolstoy"))
eq(count(authors), 2)
and(gt(year, 2020), any(authors, eq(displayname, "Leo Tolstoy")))
any(authors, any(awards, eq(title, "Nobel Prize")))
```

- In the `and(…)` example, `year` stays in the outer `WHERE`. It is not moved into the subquery.
- `min` and `max` work on both scalars and collections. `min(price, rating)` returns the smaller of two values; `min(authors, dateofbirth)` returns the smallest value across a collection. `sum`, `avg` and `count` work on collections only.
- `any`, `all` and `none` cannot be sort keys. Collection aggregates such as `count` and `min` can be used in `sort=`.

See [Field providers](field-providers.md) and [any](functions/collection/any.md).

## Sort grammar

A sort is a flat, comma-separated list of alternating field and direction tokens:

```text
field1,dir1,field2,dir2,...
```

For example:

```text
createdAt,desc,name,asc
```

- A direction is `asc` or `desc`, ignoring case. Any other value throws `NotSupportedException`.
- An odd number of tokens, or an empty string, throws `ArgumentException`.
- `ISortDirectiveParser.Parse` returns a `SortDirective`. Call `.RemoveDuplicates()` before rendering to drop repeated keys; the first one wins. See [Get started](getting-started.md).

### Sort related rows with sortfor

Use `sortfor(collectionPath, expression),dir` to order the rows of a related collection without adding a sort key to the parent. A path segment is a collection name, such as `authors` or `authors/awards`, with no leading `/`.

```text
year,desc,sortfor(authors, lastname),asc,sortfor(authors/awards, year),desc
```

- The parser stores these keys in `SortDirective.Nested`. The parent `Items` hold only keys of the outer entity.
- `sortfor` in `filter=` is rejected with its own error. See [sortfor](functions/collection/sortfor.md).
- For a boolean nested key, `asc` puts `false` first. Use `desc` to list matches first.

## Literal syntax

| Type | Syntax | Notes |
|---|---|---|
| `string` | `"text"` | Double quotes required. You cannot escape a quote inside the literal. |
| `DateTime` | `"2021-01-01"` | Double-quoted. Parsed as ISO `yyyy-MM-dd` (invariant culture) first, then with the current culture. You can change this with `LiteralParseOptions`. |
| `Guid` | `"550e8400-e29b-41d4-a716-446655440000"` | Double-quoted. |
| `TimeSpan` (time of day) | `"14:30:00"` or `"14:30"` | Available on all target frameworks. Clock time only. Parsed as invariant `hh:mm` or `hh:mm:ss`, with no culture fallback. You can change this with `LiteralParseOptions`. |
| `DateOnly` | `"2021-01-01"` | `net6.0` package only. Parsed as ISO `yyyy-MM-dd` first, then with the current culture. You can change this with `LiteralParseOptions`. |
| `TimeOnly` | `"14:30:00"` or `"14:30"` | `net6.0` package only. Parsed as `HH:mm:ss` or `HH:mm` first, then with the current culture. You can change this with `LiteralParseOptions`. |
| `int`, `byte` | `25` | Unquoted. The parser picks `byte` or `int` to match the target type, usually taken from the field it is compared with. |
| `double` | `19.99` or `1e3` | Unquoted. A number is a `double` if it contains `.`, `e` or `E`. |
| `bool` | not supported | There is no boolean literal. |

When you compare a literal with a field or another literal, the literal takes the type of the first operand. Some functions fix the type instead: the second and third arguments of `substring` are always `int`. Each [function page](functions/README.md) lists the type of every argument.

### Configure date and time parsing

Use `LiteralParseOptions` in `Expresso.Parsing` to set the culture (`CultureName`), format patterns per type (`DateTimeFormats`, `DateFormats`, `TimeFormats`, `TimeSpanFormats`), and whether to fall back to the culture (`AllowCultureFallback`). Without options, the defaults in the table apply.

```csharp
services.AddRequestParametersParsers(o =>
{
    o.CultureName = "nl-NL";
    o.DateTimeFormats = new[] { "dd-MM-yyyy", "yyyy-MM-dd" };
});
```

When you list several patterns, the first one that matches wins. To read the options from configuration, bind them in your host. The library does not depend on `IConfiguration`:

```csharp
services.AddRequestParametersParsers(
    configuration.GetSection("Expresso:Parsing").Get<LiteralParseOptions>() ?? new());
```

## Supported types

- On all target frameworks: `string`, `bool`, `byte`, `int`, `double`, `DateTime`, `Guid`, and `TimeSpan` (time of day only, not an interval).
- On the `net6.0` package only: `DateOnly` and `TimeOnly`. They are not available when you reference the `netstandard2.0` build.
- Not supported: `float` and `decimal`.

`DateTime`, `DateOnly`, `TimeOnly` and `TimeSpan` are not interchangeable in comparisons, and `TimeOnly` and `TimeSpan` are not interchangeable with each other. Convert with [`date()`](functions/datetime-getter/date.md) or [`time()`](functions/datetime-getter/time.md) when you need to compare across them.

## Operands that are not functions

Three kinds of argument are not functions:

- A field refers to an entry in the field catalog, such as `name` or `createdAt`. It resolves against the current `QueryModel`, or against the `(string, Type)[]` you pass to the parser.
- A collection reference names a related collection, such as `authors` in `any(authors, …)`. It is valid only as the first argument of a collection function.
- A literal is a constant parsed from the query string, as described above.

They have no pages of their own in the [function reference](functions/README.md).

## Next steps

- [Get started](getting-started.md): parse filter and sort expressions
- [Pagination](pagination.md): apply result limits independently of the grammar
- [Function reference](functions/README.md): syntax and behavior of each function
