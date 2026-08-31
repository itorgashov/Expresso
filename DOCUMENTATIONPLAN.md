# Expresso Documentation Plan

> Status: implemented. See [IMPLEMENTATION.md](IMPLEMENTATION.md) for the current state and [CONTEXT.md](CONTEXT.md) for the structural blueprint.

## Goal
Turn the current single-file `README.md` into a slim entry point, and move/expand the real documentation into `/docs` as plain, GitHub-renderable Markdown. Cover: what Expresso is and its use cases, the role of each of the 3 NuGet packages, a step-by-step consumer guide (which package goes in which layer, `IRequestFieldsInfoProvider` explained), the sample app, and a full function reference (one page per function, grouped by semantic category, with syntax/return type/argument types/exceptions/SQL output).

Also fix a discovered inconsistency: `samples/Expresso.Sample.WebApi/README.md` says packages **0.1.1**; the real current version (`Directory.Build.props`) is **0.2.0** — will be corrected while touched.

## Documentation tree

```text
README.md                                  (rewritten: slim entry point)
docs/
  overview.md                              (what/why, use cases, pipeline diagram)
  packages.md                              (role of each of the 3 NuGet packages)
  getting-started.md                       (step-by-step consumer guide)
  query-syntax.md                          (filter/sort syntax, literals, quoting, case rules)
  field-providers.md                       (IRequestFieldsInfoProvider deep dive)
  error-handling.md                        (exceptions catalog: parsing + rendering)
  sample-app.md                            (walkthrough of Expresso.Sample.WebApi)
  functions/
    README.md                              (index table, grouped by category, links to all 31 pages)
    logical/
      and.md  or.md  not.md
    comparison/
      eq.md  neq.md  gt.md  gte.md  lt.md  lte.md
    membership-null/
      in.md  isnull.md
    arithmetic/
      abs.md  add.md  sub.md  mult.md  div.md
    string-predicate/
      startswith.md  endswith.md  contains.md
    string-transform/
      substring.md  left.md  right.md  concat.md  lower.md  upper.md
      trim.md  ltrim.md  rtrim.md  replace.md
    string-inspect/
      len.md  indexof.md
```

31 function pages total (Field/Literal are operands, not functions — documented in `query-syntax.md` instead).

## Content per section

### `README.md` (rewritten, entry point only)
- One-paragraph pitch + pipeline diagram (kept, trimmed).
- Packages table (name + one-line role) linking to [docs/packages.md](docs/packages.md).
- Minimal filter/sort example, linking to [docs/query-syntax.md](docs/query-syntax.md).
- "Documentation" section linking to: overview, getting started, function reference, sample app.
- Build/test commands (kept as-is).
- Remove the long inline function table and type list — moved to `docs/`.

### `docs/overview.md`
- What Expresso is: a small library that turns a function-call query string into a validated expression tree, then renders it to parameterized SQL — an alternative to hand-rolling dynamic `WHERE`/`ORDER BY` string concatenation.
- Use cases: paginated list APIs with client-driven filter/sort, admin/back-office grids, reporting endpoints, any ADO.NET/Dapper-based data layer needing safe dynamic queries.
- Explicit non-goals / when *not* to reach for it (e.g., full OData-style feature parity, ORM replacement) — brief, based on prior discussion in this chat.
- Mermaid pipeline diagram: query string → `Expresso.Parsing` → expression tree (`Expresso.Core`) → `Expresso.Rendering.SqlServer` → SQL + parameters.

### `docs/packages.md`
- Table + prose per package, sourced from the exploration:
  - `Expresso.Core` — expression tree (`AbstractExpression`/`AbstractFunction`), `FilterCriteria`, `SortDirective`/`SortDirectiveItem`, `IRequestFieldsInfoProvider`. No dependencies. Published standalone so parsers/renderers share one type identity.
  - `Expresso.Parsing` — `IFilterParser`/`FilterParser`, `ISortDirectiveParser`/`SortDirectiveParser`, DI via `AddRequestParametersParsers()`. Depends on `Expresso.Core`.
  - `Expresso.Rendering.SqlServer` — `IExpressionToQueryClauseTransformer`/`ExpressionToSqlServerQueryClauseTransformer`, DI via `AddExpressionTransformations()`. Depends on `Expresso.Core`.
- Which layer typically references which package (parsing in the layer that reads query params — e.g. API/controller layer; rendering in the data-access layer).
- Target framework (net6.0, usable from net6/7/8/10) and supported types (`string`,`bool`,`byte`,`int`,`double`,`DateTime`) with the "not in v1" list.

### `docs/getting-started.md` (step-by-step)
1. Install packages per layer:
   - Presentation/API layer → `Expresso.Core` + `Expresso.Parsing`.
   - Data-access layer → `Expresso.Core` + `Expresso.Rendering.SqlServer`.
2. Register DI: `services.AddRequestParametersParsers(); services.AddExpressionTransformations();`
3. Implement `IRequestFieldsInfoProvider`: explain exactly what it specifies — a per-`context` allow-list of `(fieldName, Type)` pairs used by the parser to (a) resolve field tokens, (b) type-check literals/function arguments against real field types, and (c) prevent arbitrary/unsafe field exposure (only listed fields are filterable/sortable). Show a minimal example.
4. Parse filter/sort strings via `IFilterParser.Parse` / `ISortDirectiveParser.Parse` in a controller/handler, using fields from step 3.
5. Render to SQL via `IExpressionToQueryClauseTransformer.RenderWhereClause` / `RenderOrderByClause` in the repository, supplying a `fieldToColumnMap` (recommend `OrdinalIgnoreCase`) and a parameter prefix.
6. Execute the generated SQL + parameters (ADO.NET/Dapper example).
- Cross-link to [docs/field-providers.md](docs/field-providers.md), [docs/query-syntax.md](docs/query-syntax.md), [docs/error-handling.md](docs/error-handling.md), and [docs/sample-app.md](docs/sample-app.md) for a full worked example.

### `docs/query-syntax.md`
- Filter grammar: `functionName(arg1, arg2, ...)`, nesting, case-insensitive function/field names, root must be boolean.
- Sort grammar: `field,dir,field,dir,...` (`asc`/`desc`, case-insensitive), `RemoveDuplicates()`.
- Literal rules: strings/dates double-quoted, numbers unquoted, no boolean literal (bool fields only compare to other bool expressions), `DateTime.TryParse` current-culture caveat (prefer ISO `yyyy-MM-dd`).
- Supported types table (from README, moved here).

### `docs/field-providers.md`
- Full explanation of `IRequestFieldsInfoProvider` contract (`GetValidFilterFields(context)`, `GetValidSortFields(context)`), the `context` parameter's purpose (per-entity/endpoint allow-lists), and why it exists (security boundary — only these fields become queryable; types drive validation).
- Example implementation referencing the sample's `Filtering/RequestFieldsInfoProvider.cs`.

### `docs/error-handling.md`
- Table of exceptions consumers should catch, with cause, drawn from the exploration:
  - Parser syntax/arity errors → mostly `System.Exception` / `ArgumentException` (note the inconsistency plainly, don't hide it).
  - IR-level validation → `ArgumentNullException` / `ArgumentException` (from `AssertNotNull`/`AssertExpressionOfTypes`/etc.).
  - Note: comparison/arithmetic functions are constructed via reflection in the parser, so validation failures can surface wrapped in `System.Reflection.TargetInvocationException` (inspect `.InnerException`).
  - `FilterParser.Parse` requires a boolean root → `ArgumentException("A boolean expression is expected.")`.
  - `SortDirectiveParser` → `NotSupportedException` for bad direction, `ArgumentException` for malformed directive.
  - Renderer → `ArgumentException` for missing field-to-column mapping or bad parameter prefix, `NotSupportedException` for unknown expression type.
- Guidance: catch broadly (`Exception`) at the API boundary and return 400, as the sample controllers do.

### `docs/sample-app.md`
- Purpose: end-to-end reference implementation.
- Architecture: presentation layer (controllers parsing `filter`/`sort`, mapping models → view models) and data-access layer (ADO.NET repositories using the SQL renderer), per `samples/Expresso.Sample.WebApi/README.md`.
- Entities: books/authors/publishers schema summary (table list from `database/schema.sql`), field catalogs from `RequestFieldsInfoProvider`.
- Endpoints table + example queries (reuse/expand the sample's own README examples).
- Link to [samples/Expresso.Sample.WebApi/README.md](samples/Expresso.Sample.WebApi/README.md) for setup/run instructions instead of duplicating them.
- Fix version reference: update `samples/Expresso.Sample.WebApi/README.md` prerequisite line from **0.1.1** to **0.2.0**.

### `docs/functions/README.md` (function reference index)
- One table per semantic category (Logical, Comparison, Membership/Null, Arithmetic, String predicates, String transform, String inspect), each row linking to its page.

### Per-function page template (applied to all 31 pages)
Each page (e.g. [docs/functions/string-predicate/startswith.md](docs/functions/string-predicate/startswith.md)) will contain:
- **Description** — one or two sentences.
- **Syntax** — `startswith(text, prefix)` plus aliases if any (e.g. `substring`/`substr`).
- **Category / Return type**.
- **Arguments** table — position, name, required CLR type(s), notes on literal coercion.
- **Validation & exceptions** — exact exception types and trigger conditions (arity from parser, type/null checks from the IR constructor), including the `TargetInvocationException`-wrapping caveat where applicable (comparison/arithmetic functions), linking back to [docs/error-handling.md](docs/error-handling.md) instead of repeating the general note.
- **SQL Server rendering** — exact generated SQL pattern (e.g. `(src LIKE @p ESCAPE '\')`, `SUBSTRING(s, start, len)`, `(ISNULL(NULLIF(CHARINDEX(find, s), 0), 0) - 1)`), with example input query + resulting SQL/parameters.
- **Notes** — function-specific caveats (`indexof` is 0-based / -1 when absent; `substring`/`SUBSTRING` is SQL's 1-based; `concat` needs SQL Server 2012+, `trim` needs 2017+; LIKE wildcard escaping for `startswith`/`endswith`/`contains`; `len` ignores trailing spaces; `neq` renders `!=` not `<>`).

Content for every page is already gathered from the exploration (constructors, `Assert*` calls, parser arity rules, exact SQL patterns) — no further code reading needed before writing.

## Workspace convention
- Create `CONTEXT.md` (structural blueprint: doc tree above) and `IMPLEMENTATION.md` (brief status/progress) per workspace rule, since none exist yet; keep both brief and reference this plan rather than duplicating it.
- Keep `DOCUMENTATIONPLAN.md` as the durable plan record in the repo root (per workspace rule for feature plans).

## Out of scope
- No changes to library code or behavior (only documenting current behavior, including known quirks like the `System.Exception` arity errors and the `NotSupportedException` typo in sort-direction messages — noted plainly, not silently "fixed").
- No static-site tooling (MkDocs/DocFX) — plain Markdown only, per your choice.
