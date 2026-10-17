# Tasks

These tasks execute the approved research plan for [issue #2](https://github.com/itorgashov/Expresso/issues/2). The proposal authoring step completes none of them. Research decisions and documentation are the deliverables; see [design.md](design.md) for the method and evidence rules.

## 1. Establish the research records

- [ ] 1.1 Create `docs/research/grid-integration/report.md`, `market-evidence.md` and `support-matrix.md` with their defined responsibilities; record the issue/epic links, dated candidate inventory, market segments and inspected backend constraints, and verify each constraint points to the existing repository contract cited in the design.
- [ ] 1.2 Map candidate products/editions/frameworks to maintained package IDs, wrapper/core relationships, suites and private distribution; verify the identity map against official product/package sources and flag overlap that would prevent aggregation.

## 2. Investigate market share and adoption evidence

- [ ] 2.1 Search original studies, developer surveys and usage-detection sources for the named browser grid products; record queries, source methods, populations, periods and results in the source ledger, and verify every candidate has evidence or a documented search gap.
- [ ] 2.2 Repeat the market-source search for native Blazor candidates as a separate segment; verify the ledger distinguishes native products from JavaScript interop and records inaccessible or absent coverage.
- [ ] 2.3 Check official grid-specific vendor disclosures for all assessed candidates; record date, metric and self-reported status, and verify suite-wide/customer/revenue claims are not silently attributed to one grid.
- [ ] 2.4 Collect npm indicators for the mapped browser packages using the common 90-day window and preceding window where available; retain exact query URLs/dates and counts, verify period alignment and calculations, and document wrapper/core overlap and download biases.
- [ ] 2.5 Collect available NuGet and useful repository/dependency indicators for relevant candidates; verify interval versus lifetime counts, grid versus suite scope and distribution gaps are explicit, with no raw npm-versus-NuGet ranking.
- [ ] 2.6 Classify evidence, assess comparability/confidence and investigate material contradictions; verify derived shares have compatible numerators/denominators and reproducible formulas, and every remaining gap has a reason rather than a zero or fabricated estimate.
- [ ] 2.7 Produce the market-share/adoption comparison in `market-evidence.md`; verify every assessed candidate has a row with segment, share or unavailable status, population/period, proxies, confidence, source IDs and prioritization implication, satisfying the bounded search method.

## 3. Assess concrete technical combinations

- [ ] 3.1 Assess AG Grid Community framework/data-mode combinations against the design's feasibility gates; record versioned public API, editor, count, license and test/distribution evidence in `support-matrix.md`, and verify any Enterprise prerequisite is explicit rather than assumed away.
- [ ] 3.2 Assess Syncfusion EJ2 combinations; verify each proposed wrapper/data API reuse claim, remote-operation/count behavior and local/CI/license prerequisite has cited evidence or a named unresolved condition.
- [ ] 3.3 Assess DevExtreme DataGrid combinations; verify remote loading, query mapping, editor lifecycle and distribution/test constraints are documented for the actual product and relevant framework variants.
- [ ] 3.4 Assess the distinct Kendo products/frameworks under consideration; verify API/version/edition differences have separate matrix rows where they affect implementation or acceptance.
- [ ] 3.5 Assess the native Syncfusion and Telerik Blazor candidates and any justified additional candidate; verify hosting modes, native versus interop boundaries, editor integration, NuGet/JS asset delivery and test feasibility are explicit.
- [ ] 3.6 Review all assessed rows for unresolved critical gates; verify each is resolved from evidence, retained as candidate/deferred/excluded, or accompanied by a separately reviewable probe proposal with pass/fail criteria. Do not execute a code probe in this task.

## 4. Recommend scope and delivery order

- [ ] 4.1 Define each selected row's feature boundaries and future validation scenarios, including combined advanced/native filters, sort/page state, structured-filter load/display/apply, projection, optional counts, errors/cancellation/stale responses, disposal, accessibility/themes and packed consumers; verify prerequisites and exclusions are recorded without backend expansion or persistence responsibilities.
- [ ] 4.2 Compare feasible candidates using the market evidence and secondary demand/effort constraints; write ranked groups or an explicitly provisional order in `report.md`, verify higher-share preference is visible, and show sensitivity to weak evidence, time windows and ecosystem segmentation.
- [ ] 4.3 Recommend the first complete grid/framework/version scope, initial public-release editor contents, raw-string-import timing and subsequent sequential deliveries; verify the first can complete its adapter/editor/bindings/tests/example without implementing a second vendor.
- [ ] 4.4 Populate the product selection register and report's market summary; verify every assessed candidate has a rationale/status, deferred products have reconsideration conditions, market entries link to their evidence, and planned delivery is distinct from verified compatibility.

## 5. Review, reconcile and validate the research result

- [ ] 5.1 Audit the three documents against all ten issue acceptance criteria plus the market-share research/reporting requirement; record the traceability table and verify unique row/source IDs, evidence dates, selected-row completeness, reproducible arithmetic and explicit treatment of missing or incomparable data.
- [ ] 5.2 Present the findings and proposed order for maintainer review, resolve selection-critical questions and record acceptance in `report.md`; verify any departure from the higher-share preference has an accepted reason and that no unaccepted recommendation is described as an agreed selection.
- [ ] 5.3 Reconcile the accepted epic/backlog scope and local plan references, and add a brief result reference to `IMPLEMENTATION.md`; verify actual issue titles/links and matrix rows agree. Prepare exact remote edits where authorization is still needed and keep this task pending until the authorized reconciliation is complete.
- [ ] 5.4 Check relative links, table consistency and research completion evidence, then run `openspec validate 2-integration-support-matrix --strict`; verify validation passes, no behavioral spec files exist, and all completed task claims point to delivered research artifacts. Runtime builds/tests are unnecessary for this documentation-only work.

## Workflow follow-up

- Archive the change after the accepted findings, reconciliation and required review are complete; verify the archived metadata and planning record.
- Add published report/change links to the GitHub records after any separately authorized commit/push; use no links to nonexistent remote files.
- Close the spike and update its board status only when its completion criteria and publication/linking requirements are satisfied and the action is authorized.
- Propose each selected implementation story separately with its own behavioral requirements, design, tasks and tests.
