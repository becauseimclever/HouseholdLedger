# Feature 007: Correct or Remove a Selected-Day Transaction

## Status

Status: Draft - awaiting explicit user approval; implementation is not
authorized.

- Proposal date: 2026-08-06.
- Depends on Feature 006's approved and completed selected-day create workflow.
- Feature 003's Lucide governance finding is a separate implementation risk,
  not a reason to claim this feature approved, complete, or blocked from
  planning.

## Context and Outcome

Feature 006 records the first no-frills expense for a selected calendar date.
A useful household ledger must also let a user correct a recorded amount or
classification and remove an entry recorded in error. These are distinct from
initial creation and therefore follow it rather than expanding the create
slice.

**Proposed outcome:** From the selected day's inspector list, a user can edit
an existing transaction's amount or classification, or remove that transaction.
The inspector refreshes from backend data and truthfully reflects the result.

## Goals

1. Provide accessible edit and delete operations for transactions returned in
   the selected-day inspector list.
2. Reuse Feature 005's minimum transaction fields and Feature 006's validation
   rules; do not create a second data shape.
3. Keep correction/removal backend-owned, persisted, and visible through a
   reread day result.
4. Make destructive removal explicit before execution and report success or
   failure clearly.

## Non-Goals

- Creating a transaction; Feature 006 owns that outcome.
- Moving a transaction to another date. Date correction is deferred because it
  changes selected-day membership and needs a separately designed workflow.
- Notes, merchants, receipts, accounts, recurring/split transactions, bulk
  actions, undo, restore/archive, import, or custom category management.
- Totals, balances, budgeting, planning, reports, visual polish, or financial
  advice.
- Authentication, authorization design, multi-user conflict resolution, audit
  history, or preference persistence.

## Kakeibo and Calendar Rules

- Editing changes only the approved expense amount or classification; calendar
  date stays fixed. The list remains scoped to the selected day.
- Removal means the transaction no longer appears in that day's read model. It
  is not represented as a zero amount or fabricated balance change.
- Before removal, the UI requires clear confirmation naming the action and
  identifying the rendered transaction by amount and classification. Approved
  implementation planning selects a native dialog or accessible in-context
  pattern.
- Language is factual and nonjudgmental: it supports correcting a record, not
  evaluating spending.

## Responsibility Boundaries

| Layer | Responsibility | Must not do |
| --- | --- | --- |
| Domain | Enforce valid replacement amount/classification and transaction identity rules. | Implement UI confirmation, HTTP, EF Core, or balances. |
| Application | Execute update/delete against existing records and return invalid/not-found outcomes. | Delegate business validation to Client or calculate day totals. |
| Infrastructure | Persist correction/removal through established transaction mapping. | Add archival, audit-history, or unrelated schema. |
| API and contracts | Expose additive update/delete operations with aligned OpenAPI/convenience contracts. | Expose persistence entities or accept date moves. |
| Client | Start from a rendered selected-day record, gather allowed edits, confirm removal, and refresh day data. | Treat local list as authoritative or compute balances. |

## Acceptance Criteria

| Criterion | Observable outcome | Completion evidence |
| --- | --- | --- |
| AC-01: Edit existing record | A selected-day transaction opens in an accessible no-frills edit form with its current amount/classification. Valid save persists replacements and refreshed list shows them. | Domain/Application/API, Client interaction, and hosted desktop evidence. |
| AC-02: Edit validation and absence | Invalid replacement values are backend-rejected and presented accessibly. Editing a missing/deleted transaction returns a truthful not-found result and does not recreate or alter another record. | Unit/API problem-details and focused Client failure-state checks. |
| AC-03: Confirmed removal | Explicit confirmed removal persists, refreshes the list, and no longer shows the record; cancellation leaves it unchanged. | API/persistence, Client confirmation-state, and browser journey evidence. |
| AC-04: Day-context correctness | Operations start from selected-day returned records. Changing day during pending/completed work cannot show a result as belonging to another day. | Focused Client state/race test and browser proof. |
| AC-05: Bounded correction/removal | Creation, date moves, totals/balances, audit history, undo, category management, accounts/reporting, and visual enhancement remain absent. | Scoped implementation and audit review. |

## Validation Boundaries

Domain/Application tests prove identity, replacement-value, and missing-record
rules. API integration and checked OpenAPI prove update/delete contracts,
validation, and not-found behavior. The repository's supported real PostgreSQL
path proves persistence when its documented prerequisite is available; otherwise
that evidence is `Blocked`.

Client component tests prove edit semantics, confirmation/cancel behavior,
errors, and selected-date safety. Hosted desktop browser evidence is
authoritative for correction and confirmed-removal journeys. Test Architecture
selects exact coverage after approval.

## Dependency and Ownership Plan

Implementation is not authorized. Exact files remain for approved planning; the
required layer sequence is:

1. Confirm Feature 006's create/list workflow and prerequisite shell
   governance are complete.
2. Domain/Application adds only update and deletion use cases over the existing
   transaction model.
3. Infrastructure persists those operations through existing mapping.
4. API/contracts adds aligned update/delete contracts and checked OpenAPI.
5. Client adds no-frills inspector edit and confirmed-delete flows with reread
   state handling.
6. Test Architecture validates each layer; Documentation audits AC-01 through
   AC-05.

## Definition of Done

Feature 007 is done only after explicit user approval and a read-only audit
marks AC-01 through AC-05 `Met` with retained current evidence. The audit must
prove that edit and confirmed removal persist through backend-owned operations,
the inspector reflects a reread selected-day result, invalid/missing cases are
truthful, and no date move, total/balance, audit-history, undo, or visual scope
was introduced.

## Open Questions Requiring User Approval

1. Confirm correction is limited to amount and classification and date moves
   remain deferred.
2. Confirm permanent removal requires confirmation, accepting there is no undo
   or audit history in this MVP.
3. Confirm Feature 005's minimum fields and classification labels remain
   sufficient for correction and removal.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Original Feature 007 was an open-ended calendar-entry research proposal. | The selected calendar context and transaction contract were not then defined. |
| 2026-08-06 | Narrow Feature 007 to edit and removal after Feature 006 creation. | This preserves one observable responsibility per slice and avoids duplicating initial entry. |
| 2026-08-06 | Defer date moves, undo, and audit history. | They add workflow, persistence, and policy choices beyond correcting the minimum day record. |

## Dependencies

- Feature 005 supplies the transaction model and selected-day read result.
- Feature 006 supplies the first-entry workflow and persisted list behavior.

<!-- Superseded 2026-08-06 open-ended entry-recording draft retained as historical context.
# Feature 007: Calendar Entry Recording

## Status

Status: Draft - awaiting explicit user approval; implementation is not
authorized.

- This is a proposal only. It cannot start implementation until the user
  explicitly approves this document.
- Proposal date: 2026-08-04.
- Depends on user-approved calendar selection/detail and genuine data-contract
  decisions in [Feature 004](004-calendar-item-selection-and-inspector-detail-contract.md).
  Feature 004 is currently only a proposal, so this document does not authorize
  recording design or implementation.

## Context and Outcome

Kakeibo is a household-accounting practice that combines recording, planning,
and reflection. A first recording workflow must earn its place by helping a
household record an approved kind of calendar information in a clear, calm way.
It cannot be inferred from a temporary calendar or an empty inspector.

**Proposed outcome:** After selection/detail and data-contract decisions are
approved, a later slice can define one first Kakeibo-informed calendar entry
workflow. This proposal does not select the user workflow, field set,
categorization, financial calculations, persistence schema, or API shape.

## Goals

1. Identify the first household recording outcome that is valuable enough to
   support in the calendar-centered workspace.
2. Define the user journey only after the selected calendar object, detail
   presentation, and genuine data boundary are approved.
3. Preserve Kakeibo's reflective, nonjudgmental character without assuming a
   specific category system, amount model, calculation, or financial workflow.
4. Establish the design prerequisites needed for a later lean implementation
   specification.

## Non-Goals and Explicitly Out of Scope

- Preselecting entry entities, fields, categories, accounts, currencies,
  amounts, calculations, budgets, balances, rules, imports, attachments, or
  persistence schema.
- Choosing API endpoints, Application use cases, Domain models, validation
  rules, authorization, or migrations before the user workflow requires them.
- Implementing calendar selection, inspector detail, navigation destinations,
  or pane preference persistence.
- Claiming that Kakeibo requires one recording habit, household structure, or
  financial approach.

## Kakeibo and Calendar UX Rules

- The first workflow begins with a user-defined household need, not a generic
  accounting form.
- Language invites clear recording and later reflection without judgment,
  urgency, or assumptions about income, debt, spending, or family structure.
- Calendar context remains visible and understandable throughout the approved
  workflow; the feature must not turn the workspace into a disconnected data
  entry screen without explicit approval.
- Required information, optional reflection, validation, saving, cancellation,
  error recovery, and accessibility are defined only after the actual workflow
  and data are approved.

## Product Decisions and Open Questions

1. What first household need should recording solve, and for whom?
2. What calendar object or context starts the workflow, and how does it relate
   to Feature 004 selection and inspector detail?
3. Which information must the household provide, which information is optional,
   and which terminology is clear and nonjudgmental?
4. Is categorization useful in the first workflow? If so, what user-defined
   language and behavior are needed? If not, it remains out of scope.
5. Are any financial calculations needed for the initial outcome? If so, which
   are observable and how are they explained? If not, none are assumed.
6. What validation, saving, cancellation, recovery, privacy, and accessibility
   behavior is necessary for the approved journey?
7. Which genuine Domain, Application, API, contract, and persistence changes
   follow from those decisions, and what is the smallest valuable vertical
   slice?

## Readiness Criteria

| Criterion | Observable decision or outcome | Completion evidence |
| --- | --- | --- |
| RC-01: User outcome | The user defines one first household recording outcome and its expected result. | Recorded user decision. |
| RC-02: Selection/detail prerequisite | The selected calendar context and its truthful detail contract are approved through Feature 004 or a superseding approved feature. | Approved dependency evidence. |
| RC-03: Honest workflow | Required and optional information, terminology, validation, save/cancel, and recovery behavior are defined without invented financial assumptions. | Reviewed user journey. |
| RC-04: Kakeibo alignment | The workflow supports calm recording and later reflection without judgment or mandatory categorization/calculation. | Product and UX review. |
| RC-05: Minimal contracts | Necessary domain, application, API, persistence, security, and test work is derived from the approved journey. | Dependency map and implementation proposal. |

## Proposed Dependency-Ordered Waves

These are design/readiness waves, not implementation authorization.

1. Obtain approval for Feature 004's selection/detail and data-contract
   decisions.
2. Identify and approve one first household recording outcome.
3. Define the end-to-end user journey, information needs, privacy,
   accessibility, validation, cancellation, and recovery behavior.
4. Derive the smallest necessary domain, application, API, persistence, and
   test responsibilities from that journey.
5. Produce a separately approved lean implementation feature with observable
   acceptance criteria and a proportionate test strategy.

## Definition of Done

This proposal is ready for implementation planning only after user approval,
Feature 004's approved prerequisite, and evidence for every readiness
criterion. It is complete only after a later approved implementation feature
has audited acceptance evidence. This proposal does not authorize or claim
implementation, validation, or completion.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Created as a follow-up proposal after selection/detail. | A truthful recording workflow depends on a chosen calendar context and data contract; preselecting financial models would create false scope. |

## Dependencies

- [Feature 004](004-calendar-item-selection-and-inspector-detail-contract.md)
  is the required selection/detail and data-contract predecessor.
- [Feature 003](003-workspace-navigation-and-ui-foundation.md) remains the
  physical workspace shell and does not provide recording behavior.
-->
