# MVP Roadmap Feature Audit

**Audit date:** 2026-08-07

**Scope:** Current HouseholdLedger Features 001-008, current audits and
 governance records, and the minimum current code/test surfaces needed to assess
 MVP readiness. This is a read-only audit; retained command results are not
 represented as rerun results.

## Decision Summary

HouseholdLedger is on a credible but incomplete MVP path. The calendar workspace
and transient selected-date behavior are implemented. The API hosts the Client,
but current source has only a health endpoint, an empty EF Core context, and no
transaction domain model, application use case, API contract, migration, or
inspector data bridge.

The first viable end-to-end milestone is a selected date that causes the
inspector to request and truthfully render that day's empty transaction list.
Create, edit, and delete work should follow that slice, not precede it.

The MVP is limited to selected-date list/empty state, date-bound expense
creation, correction, and permanent deletion. Balances, totals, settings or
culture persistence, accounts, reporting, imports, recurring entries, auth,
and responsive polish remain non-MVP.

## Evidence and Limits

- Current source has hosted Blazor WebAssembly, a three-region Client shell,
  date selection/presentation, a neutral inspector, and an open-source notices
  route. The only API controller is `GET /api/v1/health`.
- `HouseholdLedgerDbContext` has no `DbSet`; source search found no transaction
  CRUD, transaction endpoint, or migration.
- [Feature 002 audit](2026-08-04-feature-002-definition-of-done.md) records a
  complete hosted-shell proof. [Feature 004 final re-audit](2026-08-06-feature-004-calendar-item-selection-and-inspector-detail-contract-reaudit.md)
  records retained evidence for date selection.
- [Feature 003 audit](2026-08-06-feature-003-workspace-navigation-and-ui-foundation-audit.md)
  leaves its dependency-governance criterion unmet. [Feature 008 final
  re-audit](2026-08-07-feature-008-open-source-notices-page-reaudit.md) marks
  Feature 008 complete and contributes notices/publish-inventory evidence.
- No build, test, publish, browser, vulnerability scan, or PostgreSQL command
  was run for this audit.

## Feature Inventory

| Feature | Current title/status | Evidence and MVP classification | Audit finding |
| --- | --- | --- | --- |
| 001 | Application Scaffolding; approved, paused, incomplete. | Project, API/OpenAPI, Client, EF/Npgsql boundary, and harnesses exist. Foundation/compliance support, not a ledger workflow. | Feature 002 supersedes its active hosted-shell validation. Historical closure is much broader than lean MVP and remains incomplete. |
| 002 | Minimal Hosted Sample Shell; complete. | Current API-to-Client hosted static-asset path and retained build/component/HTTP/browser evidence. Completed foundation support. | Temporary shell content is superseded in practice by calendar UI, but the hosting proof remains valid. |
| 003 | Workspace Navigation and UI Foundation; approved, implementation in progress. | Current layout implements desktop panes, toggles, central calendar, and neutral inspector. MVP-supporting UI foundation. | AC-01 through AC-05 are met by audit; AC-06 governance closure remains open. Its follow-on list uses old 005-007 names/scopes. |
| 004 | Calendar Day Selection and Deferred Inspector Contract; source says draft/unapproved, re-audit says complete. | Current `CalendarPage` implements transient date selection, modes, and navigation. MVP-essential UI context. | Status, approval, and completion claims conflict. It supplies no inspector data contract and cannot be the completed predecessor asserted by 005-007. |
| 005 | Daily Transaction Read Model and Empty-Day Inspector; filename says workspace navigation destinations; draft. | No transaction model, query port, schema/migration, API contract, HTTP request, or inspector data state exists. MVP-essential first vertical slice. | Active title is appropriate; filename and embedded Settings draft are stale. It incorrectly assumes 004 is complete and lacks the selection-to-inspector bridge. |
| 006 | Record a Transaction for the Selected Day; filename says workspace pane preference persistence; draft. | No create form, command, persistence, API operation, or refreshed list exists. MVP-essential after 005. | Active scope is narrow; filename and embedded pane-persistence draft are stale. It inherits 005's missing contract decisions. |
| 007 | Correct or Remove a Selected-Day Transaction; filename says calendar entry recording; draft. | No edit/delete use case, endpoint, confirmation, or transaction list exists. MVP-essential after 006. | Active scope is narrow; filename and embedded general-entry draft are stale. It needs persisted returned-record identity first. |
| 008 | Open-Source Notices Page; complete by current re-audit. | Notices route, footer link, manifest, tests, and inventory exist. Compliance support, no ledger behavior. | Resolves its own scope and supplies part of Feature 003's Lucide evidence, not Feature 003 completion. |

## Required MVP Workflow

| Required outcome | Verdict | Actual current flow or limitation |
| --- | --- | --- |
| Select a calendar date. | Implemented behavior; documentation status contradicted. | `CalendarPage` owns transient active-date state. The 004 re-audit records retained component/browser evidence, while the source feature still says implementation is unauthorized. |
| Inspector identifies selected date and lists its transactions. | Missing. | `MainLayout` contains static `No calendar item selected`; it cannot receive the private calendar state, and no read model/HTTP client exists. |
| Selected empty day is truthful. | Missing. | Current text is a pre-data shell state, not a date-specific empty result. No query distinguishes empty from unavailable. |
| Create a date-bound expense. | Planned but blocked. | Feature 006 depends on unapproved/unimplemented 005. No model, endpoint, form, or persistence exists. |
| Correct an expense. | Planned but blocked. | Feature 007 depends on a persisted returned record that does not exist. |
| Permanently delete an expense. | Planned but blocked. | Feature 007 proposes confirmation and no undo/audit history; deletion and identity do not exist. |

**First viable milestone:** after Feature 005 is reconciled, selected date flows
through Client-owned presentation context to the inspector, which calls one
date-scoped backend query and renders loading, unavailable, empty, or returned
records without totals or balances. This proves the intended UI/backend split.

## Minimal Gaps

| Gap | Classification | Required resolution |
| --- | --- | --- |
| No durable expense rule, persistence mapping, schema, explicit migration, or query port. | Existing Feature 005 implementation prerequisite. | Implement the approved read-model slice with PostgreSQL provisioning outside startup and real-provider evidence when available. |
| Calendar selection is private page state; inspector belongs to layout. No context bridge, request lifecycle, or stale-response rule is specified. | Feature-document gap requiring rewritten 005. | Define smallest Client-owned shared date context, inspector trigger, cancellation/replacement behavior, and boundary. Client must not construct transaction lists. |
| No additive date-scoped API/contract/OpenAPI surface exists beyond health. | Existing Feature 005 implementation prerequisite. | Add one query contract/controller mapping after approving the read model. |
| Currency and precision semantics are unspecified; positive numeric amount is not an unambiguous durable money value. | Feature-document gap requiring rewritten 005. | Decide a fixed named MVP currency and precision/storage rule without settings or multi-currency. |
| Required fields, four classifications, and same-day ordering are unresolved. | Feature-document gap requiring rewritten 005 and user decision. | Resolve before approval and reuse through 006/007. |
| Create form/command/endpoint/recovery/refresh is absent. | Existing Feature 006 implementation prerequisite. | Implement only after 005 is complete. |
| Update/delete/not-found/confirmation/reread is absent. | Existing Feature 007 implementation prerequisite. | Implement only after 006 is complete. |
| Feature 003 Lucide closure lacks target-framework vulnerability evidence and required narrow re-audit. | Dependency/compliance blocker. | Complete evidence and re-audit before MVP release. |
| Totals, balances, settings persistence, accounts, reporting, imports, recurring entries, auth, and responsive polish. | Explicitly deferred non-MVP. | Keep absent. |

## Features 005-007 Assessment

Feature 005 has the correct first product outcome: durable selected-day read
data and an honest empty state. It is not sufficient as drafted. Its completed
Feature 004 premise conflicts with Feature 004's current status, and it omits
the bridge from calendar-owned state to layout-owned inspector. Money and list
ordering are product contracts, not implementation details. Rewrite 005 before
approval rather than deciding those matters during implementation.

Features 006 and 007 are correctly sequenced. Create needs the selected-day
list to refresh truthfully; edit/delete need a persisted returned record with
stable identity. Start neither until 005 has delivered the application
contract, API contract/OpenAPI, database provisioning/migration path, and
Client context bridge. Then 006 adds only create; 007 adds only
amount/classification correction and confirmed deletion.

The current architecture supports the split: Domain/Application are empty homes
for rules/use cases, Infrastructure owns EF Core/Npgsql, API owns HTTP/OpenAPI,
and Client references only API Contracts. No new layer is justified. Existing
test projects provide homes for unit, API integration, PostgreSQL integration,
component, and one browser journey evidence. The real PostgreSQL harness must
be reported blocked when unavailable, not replaced with another provider.

## Lucide Governance Decision

Feature 008 resolves two gaps from the [Feature 003 Lucide review](2026-08-06-feature-003-lucide-dependency-governance-review.md):
fresh Client/hosted publish inventory and in-app notice delivery. The
[third-party inventory](../development/third-party-notice-inventory.md)
records conservative Lucide/Feather notice treatment, satisfying the review's
attribution alternative to exact icon-to-Feather mapping.

It does not provide the required successful, inspectable vulnerability result
enumerating the target framework and closure, and no narrow Feature 003
re-audit has occurred. [Dependency Governance](../development/dependency-governance.md)
requires complete closure and published-output review; the [lean policy](../../.github/instructions/lean-feature-slices.instructions.md)
does not relax dependency-governance approval.

**Decision:** Lucide is not a technical blocker to implementing no-new-
dependency transaction slices. It is a dependency/compliance blocker to
claiming Feature 003 complete and to MVP release with the unresolved closure.
Feature 008 completion does not close it.

## Documentation and Process Health

The active documents contain more ceremony than the lean workflow requires.
Features 001, 003, and 004 retain extensive historical wave/resource/validation
prescriptions. Useful history should remain evidence, but not be copied into
each MVP slice. Feature 003's old follow-ons and 005-007's embedded superseded
drafts obscure the active roadmap.

Current contradictions requiring controlled cleanup are:

- [Architecture Overview](../architecture/overview.md) says API has no Client
  reference, while current source and Feature 002 establish API-to-Client
  hosted-WASM reference.
- README/testing environment/status claims do not align cleanly with later
  retained PostgreSQL and Feature 008 evidence.
- Feature 004 source says draft/unapproved while its re-audit says complete;
  005-007 depend on it as complete.
- 005-007 filenames describe superseded scopes, and 003 retains former names.

Safe recommendation: retain completed/superseded documents as history, but give
every active feature one current title, status, outcome, acceptance criteria,
and decision history. Move or clearly fence superseded full drafts as archived
history rather than leaving them inside active specifications. Maintain a short
current status index or correct headers; do not rewrite past audit evidence.
Under lean policy, require proportional feature documents for meaningful product
outcomes and one user approval for all waves in each approved feature.

## Recommended Execution Order

1. Reconcile Feature 004 approval/status; rewrite 005 for the date-context
   bridge, money representation, ordering, and minimum transaction contract;
   archive or clearly fence stale 005-007 material and names.
2. Obtain the necessary product decisions below and user approval for revised
   meaningful feature scopes. Each approval authorizes every documented wave;
   do not request per-wave approval.
3. Implement/validate Feature 005: domain/application query, PostgreSQL
   mapping/migration, API contract/OpenAPI/controller, Client bridge and
   inspector states, proportionate tests, and one browser journey.
4. Implement/validate Feature 006 as selected-day create and backend reread.
5. Implement/validate Feature 007 as correction and confirmed permanent delete.
6. In parallel, obtain Lucide vulnerability evidence and perform the narrow
   Feature 003 governance re-audit before release.
7. Audit the implemented transaction features and full MVP journey before
   declaring MVP complete. Do not add deferred features to pad the release.

### Necessary User Product Decisions

- Confirm or replace the four proposed classifications: `Necessities`,
  `Optional`, `Culture`, and `Unexpected`.
- Choose fixed MVP currency and precision/storage representation.
- Choose deterministic same-day transaction ordering.
- Confirm selection fixes creation date and correction cannot move a date.
- Confirm explicit permanent-delete confirmation with no undo/audit history.

## Gap Ownership and Next Actions

| Gap | Owner/layer | Risk | Recommended next action |
| --- | --- | --- | --- |
| Reconcile 004 status and stale 003/005-007 material. | Research and Documentation | High: false prerequisites/completion claims. | Assign roadmap-document update before 005 approval. |
| Define context bridge, money, classifications, and ordering. | Product plus Research and Documentation | High: ambiguous contract and Client/business leakage. | Record user decisions and revise 005 acceptance criteria. |
| Transaction query/persistence/migration/API read model. | Domain, Application, Infrastructure, API | High: no truthful inspector. | Implement approved Feature 005 vertical slice. |
| Inspector loading/unavailable/empty/list states. | Client | High: current inspector cannot show backend truth. | Implement approved Feature 005 bridge and state handling. |
| Selected-day expense creation/refresh. | Domain, Application, Infrastructure, API, Client | Medium: cannot record expense. | Implement 006 after 005. |
| Transaction correction/permanent deletion. | Domain, Application, Infrastructure, API, Client | Medium: cannot correct mistakes. | Implement 007 after 006. |
| Lucide vulnerability evidence and F003 re-audit. | Dependency Governance and Research/Documentation | High for release/compliance. | Run approved evidence process and narrow re-audit. |
| Deferred scope creep. | All implementation owners | Medium: MVP delay/false claims. | Reject excluded functionality without separate approval. |

## Audit Validation

- This report is the sole file created by this audit.
- No product, test, package, lock, configuration, existing feature, existing
  audit, or BudgetExperiment file was modified.
- Markdown diagnostics and a CRLF-aware scoped diff check for this report are
  recorded after creation.
