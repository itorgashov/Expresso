# Proposal

## Why

[Spike #2](https://github.com/itorgashov/Expresso/issues/2) must turn the candidate vendors in [epic #1](https://github.com/itorgashov/Expresso/issues/1) into a finite, feasible support matrix and delivery sequence. The maintainer's additional direction on 2026-10-17 is to prefer vendors with greater grid market share, so the investigation must attempt to establish that share and report the evidence, uncertainty and effect on prioritization.

## What Changes

- Produce a research report with a product selection register, dated technical support matrix, feature boundaries, first complete grid delivery, initial-release contents and sequential order for later integrations.
- Investigate grid market share before recommending the order. Define the relevant market and measurement basis; seek original research with a stated population and methodology, then use clearly identified adoption indicators where comparable market-share data is unavailable.
- Include a market-share/adoption comparison in the resulting report for every assessed candidate, with source links, measurement periods, scope, confidence and explicit gaps. Document the search attempt even when no reliable share estimate can be obtained.
- Prefer higher-share vendors among technically feasible candidates using comparable evidence. Explain uncertainty, framework differences and any proposed exception; the existing AG Grid / Syncfusion sequence remains a hypothesis to evaluate.
- Assess exact products/editions and framework variants against Expresso's existing policy, query, paging/count and projection contracts, plus editor integration, testing, distribution and maintenance constraints.
- Review and accept a finite scope, then reconcile the epic and relevant backlog entries using real issue links. Selecting a product records planned delivery; verification of shipped integrations belongs to later changes.

## Capabilities

### New Capabilities

None. This change delivers research and decision documentation; it adds no runtime behavior.

### Modified Capabilities

None. The change metadata sets `skip_specs: true`. Research acceptance criteria are tracked in the issue, design and tasks; no behavioral specification is invented for the spike.

## Impact

After plan approval, create the authoritative research documents under `docs/research/grid-integration/`: `report.md` for conclusions, selection and release decisions; `market-evidence.md` for the market research and source ledger; and `support-matrix.md` for technical rows and future verification scope. Update `UIINTEGRATIONPLAN.md` and `IMPLEMENTATION.md` with brief references as the findings are accepted; reconcile GitHub records within the authorized scope and use published links only when available.

The proposal includes the ten acceptance criteria in issue #2 plus the new market-share investigation/reporting requirement. Completion requires accepted findings and consistent delivery scope, including an honest unavailable/inconclusive result where market data cannot support a reliable ranking.

Production adapters, editor/parser development, backend expansion, persistence, package publication and CI configuration are outside this research change. Detailed implementation languages, package exports, public signatures and test-tool budgets remain decisions for the relevant implementation stories. A code experiment, if evidence later requires one, needs a separately reviewed scope.
