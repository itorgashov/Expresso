# Documentation refresh plan

Bring the end-user documentation up to date with the LINQ/EF Core/EF6 renderers, add a getting-started path for them, stop treating SQL Server as the default dialect, and rewrite the text in a plain, task-oriented style in the spirit of Microsoft .NET docs.

## Findings (before)

- `docs/getting-started.md` covered only the SQL path (`AddSqlServerExpressionTransformations`, `dbo.book`, `RenderWhereClause`). No guide for LINQ, EF Core, EF6 or in-memory use.
- SQL Server was the implied default in the README, overview, sample page and about 40 function pages (`Example: ... on SQL Server`).
- Developer-facing text leaked into user docs (integration tests in `rendering.md` / `linq-rendering.md`; class names, reflection and "IR construction" on function pages).
- `README.md` listed only the SQL packages.
- The tone was dense and list-heavy: bold lead-ins, "Note on ..." paragraphs, long run-on bullets, em dashes.

## Decisions

- **SQL examples:** keep real renderer output, label every example with its dialect, and on the overview, getting-started and rendering pages add a short side-by-side table (SQL Server, PostgreSQL, MySQL, Oracle) for the same filter.
- **Function pages:** edit in place. Keep headings and order (a test checks the four rendering headings), fix dialect labelling, drop implementation internals, tighten wording.
- **New pages:** `getting-started.md` stays as the shared start; `getting-started-sql.md` and `getting-started-linq.md` hold the two paths.

## Style guide

Reference: Microsoft .NET docs.

- Address the reader as "you". Active voice, present tense. Short sentences. Plain words.
- Guides and concept pages (overview, getting started, packages, semantics, rendering pages) open with one or two sentences saying what the reader can do or learn and when to read the page. Do not write "This page describes ..."; start with the subject ("Expresso turns ...", "To filter an EF Core query, ...").
- Function reference pages open directly with what the function does, in one sentence, as .NET API reference pages do ("Returns true when ...", "Rounds a number to ..."). No sentence about the page itself, no "This function ...". The Syntax, Arguments and Return type sections follow unchanged.
- Task headings start with a verb ("Register the services"); concept headings are nouns. Sentence case.
- Show the code or SQL first, then explain it. Prefer a short example to a paragraph of rules.
- Use a table only for true comparisons. Do not use bold as a lead-in for every bullet.
- No em-dash asides, no filler ("deliberately", "aggressively", "it is worth noting"), no "Note on ...".
- No internal vocabulary in user docs: IR, walker, marker, golden, integration tests, class names of expression nodes. Say what the user sees instead. Keep exception types, because users catch them.
- One term per concept: filter, sort, renderer, dialect, provider. "Queryable" and "in-memory" are defined once in `docs/linq-rendering.md`.
- Every SQL snippet carries its dialect in the line above or the caption. Generic text uses the four-dialect table. Diagrams define both fill and text colours.
- End each guide with "Next steps" links.

## Work

Top-level pages (done in the main session)

- `README.md`, `docs/overview.md`, `docs/getting-started.md` (shared start), new `docs/getting-started-sql.md`, new `docs/getting-started-linq.md`, `docs/packages.md`, `docs/rendering.md`, `docs/linq-rendering.md`, `docs/semantics.md` (consistency pass), `docs/query-syntax.md`, `docs/field-providers.md`, `docs/error-handling.md`, `docs/sample-app.md`, `docs/functions/README.md`.

Function pages (65, five parallel subagents by category)

- logical + comparison + membership-null (11), string predicate/transform/inspect (15), collection (9), datetime getter/add (16), arithmetic (14).
- Per page: first line is a direct one-sentence description; dialect label on every SQL/EF example; "Validation & exceptions" in user terms with no constructor or reflection details; tone per the style guide; no change to headings, order, or any SQL/LINQ fact; report doubts instead of guessing.

## Validation

- `FunctionPageTests` (Linq.Test) passes.
- Scripted checks: no "integration test", "IR construction", `Activator` or node class-name leaks in `docs/` and `README.md`; no broken relative links.
- Every C# snippet in the two getting-started pages compiles in a scratch project against the real packages (deleted afterwards).

## Status

- [x] Plan and style guide
- [x] Getting-started split (shared, SQL, LINQ)
- [x] Top-level pages
- [x] Function pages (5 subagents)
- [x] Validation
- [x] CONTEXT.md and IMPLEMENTATION.md

## Risks

- Sixty-five edited pages can drift in tone or facts; the style guide and a review sample per category limit that.
- Keep `getting-started.md` as the shared page so inbound links still resolve.
- The supported EF Core version statement must match what is tested: EF Core 8.

## Open questions found while editing

- eq: two ArgumentException messages for incompatible types; the page does not say when each appears.
- 
eq: compatibility list omits Guid, TimeSpan, DateOnly, TimeOnly, which eq allows. Verify against NeqFunc.
- concat: parser System.Exception and constructor ArgumentException both listed for fewer than two arguments, without qualifier.
- date, 	ime (EF6): SQLite/Oracle sentences sit under the MySQL heading; heading structure is test-enforced.
- ound: Notes describe SQL Server midpoint rounding only; PostgreSQL differs.
- indexof, ltrim, trim: a few internal helper names remain in EF sections.
