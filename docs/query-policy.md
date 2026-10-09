# Query policy

Query policies let you limit the filters and sorts that clients can send to an endpoint. You write rules for allowed query shapes, compile them at startup, and pass the resulting models to the parsers.

A field catalog says which fields exist and what types they have. A policy narrows what clients can do with those fields. For example, exposing `title` in the catalog does not mean you must allow `len(title)` or `contains(title,"War")`. Policy text is written by the endpoint owner; the filter and sort strings shown below are requests from clients.

## Allow exact title matches

Suppose a book endpoint has an indexed `title` column. The administrator wants clients to compare the complete title with a supplied value, but not apply functions such as `len()` or `trim()` to it or search for fragments of it. Clients may also compare `year` with a value, combine two to five permitted conditions with `and`, and sort by `title` or `year`. The policy is:

```policy
filter := $p | and($p...[2,5])
$p := eq(title,?) | {eq,gt,lt}(year,?)
sort := title | year
```

The following examples show client filter and sort requests accepted or rejected by this policy. The accept and reject labels are not part of the requests:

```cases
accept filter: eq(title,"War")
accept filter: and(eq(title,"War"),gt(year,2000))
accept sort: year,desc,title,asc
reject filter: eq(len(title),3)
reject filter: eq(trim(title),"War")
reject filter: contains(title,"War")
reject filter: or(eq(title,"War"),eq(title,"Peace"))
reject filter: lt(2000,year)
reject filter: eq(title,publisher)
reject sort: lower(title),asc
```

Here is what each line does:

1. `filter := $p | and($p...[2,5])` describes a complete filter. It can be one permitted condition (`$p`) or an `and` containing two to five permitted conditions. It does not allow `or` or `not`.
2. `$p := eq(title,?) | {eq,gt,lt}(year,?)` defines those conditions. For `title`, only `eq` with the field itself on the left and a literal value on the right is allowed. For `year`, `eq`, `gt`, and `lt` are allowed in the same positions. The `?` stands for a literal supplied by the client.
3. `sort := title | year` allows either field as a sort key. The client can choose `asc` or `desc` and can supply more than one permitted key.

The `cases` block shows the effect on client queries. `eq(title,"War")` and the two-condition `and` fit the rule. `eq(len(title),3)` and `eq(trim(title),"War")` apply functions to `title`; `contains(title,"War")` searches for a fragment. `or(...)` uses a disallowed combination. `lt(2000,year)` reverses the required argument positions, and `eq(title,publisher)` puts a field where a literal is required. Sorting by `lower(title)` applies a function rather than sorting by the field itself.

Matching is positional. `gt(year,?)` does not also allow the equivalent `lt(?,year)`. A field-to-field comparison needs an explicit field pattern in each position where a field may appear.

By default, a policy denies any filter or sort it does not allow. Filters and sorts have separate rules: if you omit `sort := ...`, all sorts are rejected unless you set `default sort: allow`. Compilation warns when one side has no way to accept a query.

## Compile and attach a policy

Policies are included in `Expresso.Core` and `Expresso.Parsing`; you do not need another package or renderer setup. This C# example attaches the simpler version of the book policy, with one condition per filter:

```csharp
using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Parsing;
using Expresso.Parsing.Policies;

var fields = new QueryModel(new[] {
    ("title", typeof(string)), ("year", typeof(int))
});
var definition = new QueryPolicyDefinition {
    Rules = "filter := eq(title,?) | gt(year,?)\nsort := title | year",
    Limits = new QueryPolicyLimits { MaxSortKeys = 3 }
};
var models = QueryPolicyCompiler.Compile(definition, fields, fields);
foreach (var warning in models.Warnings)
    Console.WriteLine(warning);

// Keep models in your endpoint's singleton configuration.
var filter = new FilterParser().Parse("eq(title,\"War\")", models.Filter);
var sort = new SortDirectiveParser().Parse("year,desc", models.Sort);
```

The imports bring in field models, policy definitions, parsers, and the compiler. Read the setup in order:

1. `fields` lists the names and types available to this endpoint. The policy can restrict these fields but cannot add others.
2. The `filter := ...` line in `Rules` accepts one condition per filter: either an exact `title` match or a `year` greater-than comparison.
3. The `sort := ...` line accepts sorting by `title` or `year`.
4. `MaxSortKeys = 3` caps the entire sort request at three keys, even when each key is allowed.
5. `Compile` checks the policy against the field catalog once at startup and returns separate models for filter and sort parsing. Inspect `Warnings` for dead rules or closed targets.
6. On each request, pass the appropriate model to `FilterParser` or `SortDirectiveParser`. The shown filter and sort are accepted; a disallowed query throws `QueryPolicyException`.

Pass the same `LiteralParseOptions` to the compiler and parsers when customizing date or time formats. Compilation copies the limits. Changing the definition afterwards does not change the compiled policy.

Names in filter rules resolve against the filter catalog; names in sort rules resolve against the sort catalog. A policy cannot expose a field absent from its catalog. `WithPolicy` returns a new model, and `WithPolicy(null)` removes the policy. Using a compiled policy with a different catalog throws `InvalidOperationException`.

The `QueryModel` parser overloads enforce the policy after parsing and type checking. Array-of-fields overloads have no policy. Only the top-level model's policy is consulted; policies attached to collection item models are ignored. SQL, LINQ, and EF rendering consume the accepted tree as usual. Host-created trees and keys appended after parsing are the host's responsibility.

## Deny rules

Suppose an internal reporting endpoint needs a wide range of queries. Its administrator is comfortable allowing any valid filter and sort, except for `or` and `not`, and wants to stop clients from inspecting the length or a substring of `title`. Unlike the previous example, this policy intentionally still permits `lower(title)`:

```policy
default allow
deny or(*...) | not(*)
deny {len,substring}(~title,...)
```

The following examples show client filter and sort requests accepted or rejected by this policy. The accept and reject labels are not part of the requests:

```cases
accept filter: eq(title,"War")
accept filter: eq(lower(title),"war")
accept filter: eq(1,1)
accept sort: lower(title),desc
reject filter: not(eq(title,"War"))
reject filter: eq(len(lower(title)),3)
reject filter: eq(substr(title,1,3),"War")
reject sort: len(title),asc
```

Read the policy one line at a time:

1. `default allow` accepts valid filters and sorts unless a deny rule or a resource limit rejects them. It applies to both targets.
2. `deny or(*...) | not(*)` rejects either function wherever it appears in a query. `*...` means any arguments to `or`; `*` means any argument to `not`.
3. `deny {len,substring}(~title,...)` rejects either named function when its first argument contains `title`, even inside another function. The `~` searches through wrappers, and `...` allows any remaining arguments.

The accepted examples show what this broad policy still permits: a direct title match, `lower(title)`, a comparison of two literals, and sorting by `lower(title)`. The rejected examples contain a denied function: `not(...)`, `len(lower(title))`, `substr(title,1,3)` (an alias of `substring`), or `len(title)` as a sort key. The deny applies to the filter or sort even when the rest of the query would be allowed.

`deny len(title)` would miss `len(lower(title))`; `deny len(~title)` catches it. Under `default allow`, field-free predicates also pass unless you deny them, for example with `deny eq(?,?)`. Future library functions also become available under `default allow` unless a deny rule covers them. Use explicit allow rules when you need a closed set of query shapes.

This sample checks the first argument of `len` and `substring`. For functions that can receive the sensitive field in a later argument, use the exists shape: `deny {indexof,replace}(..., ~title, ...)` finds `title` in any argument position, including inside a wrapper.

Deny statements inspect every subtree and take precedence over allow rules. A target prefix, such as `deny filter:`, confines a deny to that side. An untargeted deny applies to both sides; if its fields exist on only one side, it applies there. Failure to resolve on both sides is a compilation error.

## Collections and sort keys

Suppose each book has `authors`, and each author has `awards`. An administrator wants clients to find books by an author's display name or an award title, and to sort by the book's year, an author's name, or an award field. The endpoint's field catalog must already expose these collections and fields. This policy specifies where each name is used:

```policy
filter := any(authors,$author)
$author := eq(displayname,?) | any(awards,eq(title,?))
sort := year | sortfor(authors,lastname | firstname)
             | sortfor(authors/awards,year | title)
```

The following examples show client filter and sort requests accepted or rejected by this policy. The accept and reject labels are not part of the requests:

```cases
accept filter: any(authors,eq(displayname,"Leo"))
accept filter: any(authors,any(awards,eq(title,"Nobel Prize")))
accept sort: year,desc,sortfor(authors,lastname),asc,sortfor(authors/awards,year),desc
reject filter: eq(title,"Nobel Prize")
reject filter: any(authors,eq(title,"Nobel Prize"))
reject sort: sortfor(authors,title),asc
reject sort: sortfor(editors,lastname),asc
```

Each policy line has a separate job:

1. `filter := any(authors,$author)` allows a book when at least one author matches the reusable `$author` rule. No other whole-filter shape is allowed.
2. `$author := eq(displayname,?) | any(awards,eq(title,?))` checks names in an author, not in the book. It permits an exact author `displayname` or an award whose `title` equals a literal. Inside `any(awards,...)`, `title` belongs to an award.
3. `sort := year | sortfor(authors,lastname | firstname)` allows sorting by the book's `year` or by an author's `lastname` or `firstname`.
4. `| sortfor(authors/awards,year | title)` continues the same sort rule. It also permits sorting by an award's `year` or `title` at the exact `authors/awards` path.

The accepted filters follow the author and award paths above; the accepted sort combines three permitted keys. The rejected filters try to use award `title` at the book or author level, where this policy does not permit it. The rejected sorts request `authors/title` or an `editors` path, neither of which appears in the sort rule. The client may choose `asc` or `desc` for any permitted key.

An unqualified name belongs to the current scope. The second argument of a collection function with a named collection uses that collection's item catalog. A qualified path is relative to the current scope. Named rules can be reused in different scopes; each use is checked separately. Multiple `sortfor` alternatives for one path form a union.

`sortfor` belongs only in sort start alternatives, including through rule references. Collections and collection quantifiers cannot be sort keys. Host-added tie-breakers, such as `ThenBy(id)`, are outside policy enforcement.

## Pattern reference

| Pattern | Meaning |
|---|---|
| `?` | Any literal at this position |
| `"active"`, `42` | A literal equal to this constant, typed from its position |
| `title` | A field or collection in the current scope |
| `authors/displayname` | A field reached through collections from the current scope |
| `*` | Any expression, subject to default counts in an allow pattern |
| `$p` | Any alternative of rule `$p` |
| `p \| q`, `(p \| q)` | Either alternative |
| `~p` | `p` at this node or anywhere below it |
| `eq(title,?)` | The named function with positional arguments |
| `{eq,neq}(title,?)` | Either function with those arguments |
| `@comparison(year,?)` | Any viable function in that category |
| `*(title,?)` | Any viable function with those arguments |

Names, categories, keywords, and rule references ignore case. String constants compare case-sensitively. Constants use the query parser's literal conversions; for example, `eq(price,10)` converts the constant to `double` when `price` is a `double` field. Strings have no escapes and cannot contain line breaks. `#` starts a comment outside a string.

Fields with other CLR types, such as `long`, can still be named in sort rules and matched by `*`, `~field`, or a deny rule. A same-type `in` over such fields is also possible. This does not make functions such as `eq(longField,?)` valid when the underlying expression parser does not support that type.

Aliases refer to the same function: `substr`/`substring`, `ceil`/`ceiling`, and `pow`/`power`. `min` and `max` resolve to arithmetic or collection functions according to their arguments. Every member of an explicit set must be viable. A category discards non-viable members and fails only if no member remains.

### Argument shapes and bounds

| Shape | Example | Meaning |
|---|---|---|
| Fixed | `eq(title,?)` | Exactly these argument positions |
| Fixed plus repeated tail | `in(title,?...[1,20])` | Fixed first argument, then 1 to 20 literals |
| Bare tail | `eq(title,...)` | Remaining arguments are unrestricted expressions |
| Exists | `and(...,eq(year,?),...)` | At least one argument matches the middle pattern |

A repeated tail must be last. A typed tail without bounds has minimum 1; a bare tail has minimum 0. The function's own arity still applies: `and($p...[1,3])` requires at least two arguments. Empty calls and multiple tails are invalid.

Bounds are `[n]` (exactly n), `[n,m]`, `[n,]` (default maximum), and `[,m]` (minimum zero). They count only the repeated arguments after the fixed positions. `[0,0]` and reversed bounds are invalid. Bounds above a fixed function's maximum arity are clipped. Explicit bounds replace the local default count, in either direction; they never relax tree limits or `MaxSortKeys`.

Without explicit bounds, `and`, `or`, and `concat` use `MaxArgs` for their total argument count; `in` uses `MaxInItems` for candidates after its first argument. Other functions use their own arity. Allow-position `*`, bare tails, and unmatched arguments in an exists pattern also enforce defaults on their descendants. In deny patterns those wildcards are unconditional, so an explicitly relaxed list cannot bypass a deny rule.

### Categories

| Category | Functions |
|---|---|
| `@logical` | and, or, not |
| `@comparison` | eq, neq, gt, gte, lt, lte |
| `@membership-null` | in, isnull |
| `@arithmetic` | abs, add, sub, mult, div, mod, floor, ceiling, round, sign, power, sqrt, min, max |
| `@string-predicate` | startswith, endswith, contains |
| `@string-transform` | substring, left, right, concat, lower, upper, trim, ltrim, rtrim, replace |
| `@string-inspect` | len, indexof |
| `@datetime-getter` | year, month, day, dayofyear, hour, minute, second, dayofweek, date, time |
| `@datetime-add` | addyears, addmonths, adddays, addhours, addminutes, addseconds |
| `@collection-quantifier` | any, all, none |
| `@collection-aggregate` | count, min, max, sum, avg |
| `@predicate` | All boolean functions except and, or, not |

## Limits and failure order

| Limit | Default | Scope |
|---|---:|---|
| `MaxDepth` | 8 | Each tree, counting its root as depth 1; configuration maximum 100 |
| `MaxNodes` | 100 | Each tree, including operands |
| `MaxArgs` | 20 | Default argument count for and, or, concat |
| `MaxInItems` | 100 | Default candidate count for in |
| `MaxStringLength` | 500 | Each string literal, in UTF-16 code units |
| `MaxSortKeys` | 5 | The entire raw sort directive |

All limits must be positive. The filter is one tree; every root or nested sort key is a separate tree. Tree limits are not summed across keys. `MaxSortKeys` counts root keys plus every nested key, including duplicates, before `RemoveDuplicates()` and before host-added `ThenBy` keys. It applies in both default modes and is checked before any key's deny or allow rules. The three-key sort above needs `MaxSortKeys` of at least 3.

For each parsed tree, structural limits run first, then deny rules in post-order, then the start rule or default allow. `MaxDepth` is checked after the expression parser builds the tree; it does not limit the parser's recursion or replace a host request-size limit. Explicit bounds cannot override structural limits. A repetition whose minimum size exceeds `MaxNodes` generates warning W2; increase `MaxNodes` if you intend to admit it. Empty or comment-only policies deny both targets.

## Language reference

```ebnf
Policy      ::= Statement* EOF
Statement   ::= (DefaultStmt | DenyStmt | RuleStmt) ';'?
DefaultStmt ::= 'default' Target? ('allow' | 'deny')
DenyStmt    ::= 'deny' Target? Pattern
Target      ::= ('filter' | 'sort') ':'
RuleStmt    ::= ('filter' | 'sort' | RuleName) (':=' | '|=') Pattern
Pattern     ::= Term ('|' Term)*
Term        ::= SortFor | Call | '~' Term | '(' Pattern ')'
              | RuleName | FieldRef | '?' | '*' | String | Number
SortFor     ::= 'sortfor' '(' FieldRef ',' Pattern ')'
Call        ::= Callee '(' ArgList ')'
Callee      ::= Name | Category | '{' SetItem (',' SetItem)* '}' | '*'
SetItem     ::= Name | Category
ArgList     ::= '...' ',' Pattern ',' '...'
              | (Pattern ',')* TailItem | Pattern (',' Pattern)*
TailItem    ::= '...' Bounds? | Pattern '...' Bounds?
Bounds      ::= '[' Count ']' | '[' Count ',' Count? ']' | '[' ',' Count ']'
FieldRef    ::= Name ('/' Name)*
```

Names use ASCII letters, digits, and underscores and cannot start with a digit. Rule names start with `$`; categories with `@`. Numbers support a leading minus, fractions, and exponents. Counts are unsigned decimal integers that fit in `Int32`. Whitespace and comments separate tokens; newlines do not end a statement, and `;` is optional. A continuation line may start with `|`. Keywords can be field names, for example `eq(sort,?)`.

`$p |= pattern` extends an earlier definition. Forward references are allowed; duplicate definitions and extensions before definitions are errors. Recursive rules must have a finite alternative. Targeted defaults override untargeted defaults; duplicate defaults for one target are invalid. A default-allow target cannot also have a start rule. Excessive syntactic nesting is rejected at compilation before exhausting the stack.

## Diagnostics and hosting

`QueryPolicyCompileException` is a startup configuration error. Its ordered `Diagnostics` contain `Code`, `Severity`, `Line`, `Column`, and `Message`. Errors are collected within one phase; lexical errors stop before syntax checking, and syntax errors stop before semantic checks. Syntax codes begin with P; semantic codes S1 through S12 cover fields, functions, arity, types, rules, productivity, targets, defaults, categories, bounds, limits, and required inputs. Warnings are returned in `QueryPolicyModels.Warnings`: W1 unused rule, W2 repetition larger than `MaxNodes`, W3 closed target, W4 duplicate set member.

`QueryPolicyException` derives from `ArgumentException`. Its `Kind` is `DenyRuleMatched`, `NotAllowed`, or `LimitExceeded`; `Target`, `Path`, `RuleText`, and `LimitName` provide logging details when applicable. Not-allowed paths are deterministic best-effort locations. The default message is `The query is not permitted by the endpoint policy.` Set `ErrorDetail = QueryPolicyErrorDetail.Detailed` only when clients should see those details.

The [sample hosts](sample-app.md#query-policies) join arrays under `Expresso:Policies:book`, `author`, and `publisher` into the library's single rules string. They compile eagerly at startup and inject `ControllerQueryModels<TController>`. A missing section leaves that context unrestricted. Invalid policies stop startup with the context name; violations are logged and return the existing empty 400 response.

See [Field providers](field-providers.md), [Error handling](error-handling.md), and the [function reference](functions/README.md) for the underlying catalog and function constraints.
