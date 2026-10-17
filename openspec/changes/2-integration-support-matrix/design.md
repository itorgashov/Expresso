# Design

## Context

See [proposal.md](proposal.md) for the motivation and outputs. This is the research approach for [issue #2](https://github.com/itorgashov/Expresso/issues/2), including the maintainer's 2026-10-17 instruction to investigate market share and prefer larger-share grid vendors. No product selection or market ranking has been established by drafting these artifacts.

Repository inspection provides concrete boundaries:

- [QueryPolicyDefinition](../../../src/Expresso.Core/Policies/QueryPolicy.cs) exposes policy source and limits; the compiled policy is opaque. [Policy documentation](../../../docs/query-policy.md) demonstrates positional rules and separate filtering/sorting catalogs.
- [Pagination](../../../docs/pagination.md) supports page/page-size and skip/take while leaving HTTP names and response shape to the host. [BooksController](../../../samples/Expresso.Sample.WebApi/Controllers/BooksController.cs) is one sample contract, not a universal endpoint schema.
- [Projection](../../../docs/projection.md) uses an independent response catalog. [ProjectionEndpointTests](../../../test/Expresso.Sample.Test/ProjectionEndpointTests.cs) already checks that filtering, sorting, paging and count headers survive projection.
- Existing [CI](../../../.github/workflows/ci.yml) and [publishing](../../../.github/workflows/publish.yml) cover the current repository. This spike assesses future UI testing/distribution prerequisites without changing pipelines.
- OpenSpec currently has no adopted capability specifications to modify. The issue calls for documentation and accepted decisions, so the change uses `skip_specs: true`.

## Goals / Non-Goals

**Goals:** make product comparisons reproducible, preserve the backend constraints, make uncertainty visible, and turn market evidence into an explainable delivery recommendation. Keep decisions traceable from evidence to product/framework rows and later issues.

**Non-Goals:** collect proprietary customer information, purchase reports, conduct a new population survey, prove compatibility by implementing grids, choose detailed runtime APIs, or turn package-download percentages into an asserted global market share. Scope exclusions in the proposal also apply.

## Decisions

### 1. Use a bounded investigation with three durable outputs

After approval, create these files, each with a scope sentence and research/acceptance date:

| File under `docs/research/grid-integration/` | Responsibility |
|---|---|
| `report.md` | Executive findings, market comparison summary, product decisions, first delivery/release contents, sequence, alternatives, unresolved limitations and acceptance record |
| `market-evidence.md` | Market definition, source/search ledger, measurements, comparability/confidence assessment and calculation notes |
| `support-matrix.md` | Exact technical rows, feasibility evidence, feature boundaries and future verification scenarios |

Keep measurements in the evidence file and technical facts in the matrix; the report links their stable IDs. Link accepted outputs from the epic and implementation stories once published. The OpenSpec design remains the investigation method, while the report holds the resulting decisions.

A single large report was considered, but separate evidence and technical tables keep the decision readable and easier to maintain. An archived change alone was rejected as the sole home for a support matrix that later deliveries must update.

### 2. Define the market and product identities before comparing numbers

Working definition: adoption of data-grid/table products in the web application ecosystems targeted by the epic, with no geographic or industry restriction assumed. Prefer installation/application/organization usage evidence over revenue share for this integration decision. Record the actual population of each source; do not relabel a regional, survey-only or public-website sample as global.

Assess browser JavaScript/framework products and native Blazor products as separate segments. Within a segment, distinguish vendor, exact product/edition and framework variant. Broad vendor revenue or customer counts spanning unrelated components are context only unless a grid-specific subset is identified.

Start with the candidates in issue #2: AG Grid Community, Syncfusion EJ2, DevExtreme DataGrid, distinct Kendo products, Syncfusion Blazor and Telerik Blazor. Other grids may enter after a documented demand/adoption/fit rationale. Assess AG Grid Community without presuming Enterprise; assessment order does not fix delivery order.

Create a product-to-evidence map: official product names, maintained package IDs, wrapper/core relationships, renamed packages, bundled suites and known private feeds. Avoid double-counting the same integration through core and wrapper downloads or through overlapping editions. Vendor-level aggregation requires compatible units, periods and a defensible overlap treatment; otherwise present product figures side by side.

### 3. Attempt direct market-share discovery, then triangulate adoption indicators

For each named candidate, search these source classes and record both useful results and gaps:

1. Original market/usage studies, developer surveys and technology-detection datasets with a disclosed population, period and measurement method. Assess grid coverage and representativeness before using an estimate.
2. Official grid-specific vendor disclosures, marked as self-reported and checked for date, scope and comparability.
3. Official package registries and, where useful, repository/dependency statistics as supplementary adoption indicators.

Use original publications or the data producer's methodology, not unsourced comparison articles. Independent evidence is preferred for market comparisons; official vendor docs remain primary for technical API, edition and license facts. A source being original does not make its sample representative.

The source/search ledger records source ID, URL, publisher, query/search route, publication date, measurement period, access date, product/framework coverage, population/denominator, sample size where provided, metric/unit, methodology, sponsor, result and limitations. Log unavailable, inaccessible, outdated and inconclusive results. Seek two independent evidence families for comparisons when obtainable; record the gap rather than fabricate a second source.

Use the most recent relevant data available, preferring studies from the preceding 24 months. Older findings require an explicit recency limitation. Stop after a documented pass through the source classes for each named candidate and targeted follow-up on material contradictions; record remaining gaps instead of conducting an exhaustive market census. Extending that scope is a reviewed decision.

### 4. Keep measured share, estimated share and proxies distinct

Classify each observation as a source-reported share, a reproducible share estimate within a defined population, an adoption proxy, or contextual information.

Only derive a percentage when numerator and denominator use the same population, unit and period. Publish the formula, exclusions and coverage alongside it. A percentage within the assessed candidate set is only a share of that sample. Multi-select survey responses may overlap; do not force them to sum to 100 percent.

For registry indicators:

- Map exact package IDs before retrieval. For npm, use a common latest completed UTC cutoff and the most recent 90 complete days; compare the preceding 90 days when available. Record the API's actual dates, missing data and any anomaly. The [npm download API](https://github.com/npm/registry/blob/main/docs/download-counts.md) provides package counts over explicit periods.
- Treat downloads as downloads, not unique users or deployed applications. Explain repeated/automated installation, dependency, cache/mirror, bundled-package and commercial/private-distribution biases. Do not add wrappers and their shared core as if they were separate customers.
- The [NuGet search API](https://learn.microsoft.com/en-us/nuget/api/search-query-service-resource) exposes download totals. Distinguish cumulative totals from interval counts, and do not rank native Blazor products against npm products on raw totals. If comparable interval or grid-specific data is unavailable, record that limitation without requiring a long-running measurement project.
- GitHub stars measure interest, and public dependency counts cover an observable subset. Use them only as labeled secondary evidence; a vendor suite repository is not a grid's installed base.

For each comparison, rate confidence with an explanation: High for current, comparable grid-specific evidence with disclosed methods and corroboration; Medium for useful but limited or partly corroborated evidence; Low for indirect, stale, self-reported or poorly comparable evidence. Confidence describes the inference and source coverage, not a statistical confidence interval. Preserve disagreement rather than averaging incompatible metrics.

The report's market table must show vendor/product, segment, reported or estimated share (or unavailable), measured population, period, proxy indicators, confidence, source IDs and prioritization implication. Never convert missing data to zero.

A downloads-only league table was rejected because package structure and distribution differ. Direct market-share estimates alone were also rejected as a completion dependency because relevant public data may not exist. The chosen method reports the best defensible evidence and an explicit inconclusive outcome when necessary.

### 5. Apply technical feasibility gates before prioritizing delivery

For each product/framework/data-mode row, verify from exact-version public documentation:

- A supported loading/query-change extension point, remote rows/counts, refresh and lifecycle/disposal handling. The mode must not silently process a partial page as the full dataset.
- Faithful mapping to configured Expresso fields, types, policy shapes/limits, sorting and paging. State disabled/restricted/excluded native operations instead of expanding the backend.
- Pager requirements for exact totals, optional counts, page counts and end-of-data signals. Unknown totals must remain unknown.
- Mounting and value/event integration with the shared operation-first editor, including permitted nesting, themes, keyboard/focus and independent instances.
- Host-provided structured filter load/display/apply, advanced/native predicate composition, and state preservation across filter, sort and page changes. Persistence remains host-owned; raw-string import is a separate release choice.
- Feasible repeatable local/CI tests, vendor licenses/dependencies, packed consumer examples, npm or native NuGet distribution and independent UI release constraints.

Record pass, conditional or unresolved/fail with evidence. A selected row must have critical conditions resolved; otherwise keep it candidate/deferred/excluded. Documentation-based feasibility is not runtime verification.

Technical matrix rows include exact product/edition, framework and grid baselines/ranges, data API/mode, browser/runtime and Blazor hosting targets, operation/feature boundaries, endpoint prerequisites, editor integration, distribution, verification scope and delivery status. Later selected rows receive dated baselines and a required recheck before implementation.

Assess test methodology at the scenario/prerequisite level: backend conformance, real-grid request/state behavior, count/error/cancellation/stale-response handling, accessibility/theme checks, and packed-package consumption. Recommend suitable test-framework options where evidence supports them; final tool versions and quality budgets belong to implementation designs.

### 6. Prefer greater market reach using comparable evidence

Among feasible candidates within comparable segments, prefer the vendor/product with greater credible market share. When direct share is unavailable, use the best triangulated adoption evidence for a provisional recommendation and label that basis explicitly.

Do not combine unrelated percentages or proxies into an arbitrary score. Use ranked groups or ties where evidence cannot distinguish candidates. Keep known consuming-application demand, framework fit, test/license constraints, maintenance effort and evidence confidence visible beside market reach.

Any recommendation to deliver a lower-share candidate first must name the concrete reason and receive maintainer acceptance. Higher share does not override a critical feasibility failure. Missing public commercial data alone does not exclude a candidate or establish low adoption.

Show whether the order changes when weak evidence is removed, when the measurement window changes, or when browser and native Blazor segments are considered separately. Where cross-segment evidence is incomparable, make the framework/ecosystem trade-off explicit for the maintainer. The previously suggested AG Grid / Syncfusion sequence may change based on this investigation.

### 7. Accept scope and evidence before closing the spike

Keep Candidate / Selected / Deferred / Excluded separate from Not started / In progress / Verified delivery states. Every assessed candidate gets a rationale, and deferred ones get reconsideration conditions.

For each selected row, classify features as required now, later candidate or out of scope. Include projection/row identity, native filters, multi-sort, advanced tree scope, host-filter loading, raw-string import, counts, errors/lifecycle, accessibility/themes/localization and explicit grouping/pivot/summary/CRUD/export boundaries.

Recommend the first complete grid scope, a bounded framework subset versus all selected variants, initial public-release editor contents and sequential order. Complete its adapter/editor/framework/test/example scope before starting another grid. Internal adapter checkpoints and public-release contents are separate decisions.

The report must trace all ten issue acceptance criteria to evidence/output sections and include an additional market-research check: a documented discovery attempt, market comparison with confidence/gaps, and an explained effect on delivery order. No numerical estimate is mandatory when evidence cannot support one. Critical technical unknowns require resolution or deferral of affected rows.

Present findings to the maintainer and record accepted choices, reasons and remaining non-blocking questions. Reconcile actual epic/story scope and local planning references only after acceptance; new issue creation, remote messages and publishing follow the user's authorization. Prepare exact edits where that authorization is still needed. Do not close the spike solely because its proposal exists.

## Risks / Trade-offs

- **No reliable grid-specific market shares:** record the search and gaps, use labeled proxies, and request acceptance of a provisional order.
- **Public data favors open distributions over commercial products:** identify suites/private feeds, triangulate methods and avoid interpreting absence as low adoption.
- **Conflicting samples or vendor claims:** retain source-level observations, downgrade inference confidence and show sensitivity rather than forcing a precise ranking.
- **Product/framework variants evolve:** date every technical row and recheck later integrations before design/implementation.
- **Research grows into implementation:** resolve uncertainties through evidence or a separately approved probe; otherwise defer the affected row.
- **Selection appears to promise shipped support:** keep roadmap status explicit and reserve verified claims for subsequent implementation evidence.

## Investigation Questions

These are the outputs of the approved research method, not blockers to drafting it: which candidates have comparable market evidence; which technical rows pass the gates; which first framework subset and release contents are best supported; and which exceptions or deferrals the maintainer accepts. Their answers do not authorize later production implementation.
