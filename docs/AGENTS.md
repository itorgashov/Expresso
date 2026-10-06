# Docs — agent guide

User-facing documentation. Style guide: [DOCSREFRESHPLAN.md](../DOCSREFRESHPLAN.md). Page map: [CONTEXT.md](../CONTEXT.md#documentation-structure-docs).

## Function pages (`functions/**`)

- One page per IR function. Follow the template in [functions/string-transform/right.md](functions/string-transform/right.md): Description, Syntax, Category / Return type, Arguments, Validation & exceptions, SQL rendering, LINQ rendering, EF Core rendering, EF6 rendering, Notes.
- `FunctionPageTests` (in `Expresso.Rendering.Linq.Test`) fails if any page is missing `## SQL rendering`, `## LINQ rendering`, `## EF Core rendering` or `## EF6 rendering`, or has them out of order.
- Add a new page to [functions/README.md](functions/README.md).
- Take SQL and exception messages from the renderer and parser output, not from memory. Group dialects only when the SQL is identical.

## Writing

- Guides open with one sentence that says what the page covers; function pages open with one sentence that says what the function returns.
- Every SQL example names its dialect.
- Describe behavior for users of the packages. Leave test harness details and internal class names out unless the reader calls them.
- Check relative links and anchors after moving or renaming a heading.
