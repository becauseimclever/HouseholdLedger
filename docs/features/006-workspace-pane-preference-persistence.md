# Feature 006: Record a Transaction for the Selected Day

## Status

Status: Draft - awaiting explicit user approval; implementation is not
authorized.

- Proposal date: 2026-08-06.
- Depends on Feature 005's approved and completed transaction model,
  persistence boundary, selected-day read contract, and inspector read state.
- Feature 003's unresolved Lucide dependency-governance audit finding is a
  separate implementation risk. It does not block planning or approval of this
  draft, but its owner must resolve it before relying on the workspace shell.

## Context and Outcome

Kakeibo is a household-accounting practice of recording spending so it can be
considered with care. Feature 005 lets a user select a date and truthfully see
that day's stored transactions or empty state. The next smallest useful result
is recording one expense in that same calendar context.

**Proposed outcome:** A user selects a calendar date, uses the right inspector
to enter the approved minimum transaction values, and saves one transaction for
that selected date. The refreshed inspector shows the saved record in that
day's list.

## Goals

1. Add one accessible, no-frills create form to the selected-day inspector.
2. Keep the record date fixed by calendar selection; the form does not collect
   a competing date.
3. Validate, create, persist, and reread the transaction through backend-owned
   domain and application behavior.
4. Show field-level validation and save failure without unnecessarily losing
   entered valid values.

## Non-Goals

- Editing or deleting existing transactions; Feature 007 owns correction and
  removal.
- Description/note, merchant, receipt, timestamp, account, income, transfer,
  recurring entry, split, import, attachment, or custom category management.
- Totals, balances, budgets, planning, reports, advice, confirmation dialogs,
  undo, autosave, bulk entry, or visual refinement beyond usable controls.
- Authentication, multi-currency, settings, navigation destinations, or pane
  preference persistence.

## Kakeibo and Calendar Rules

- The form records one expense on the selected date. Amount and classification
  are the only proposed user-entered fields.
- Amount is required and positive. Currency selection and formatting are
  deferred until separately approved product decisions exist.
- Classification is required and exposes only Feature 005's proposed four
  Kakeibo-inspired labels: `Necessities`, `Optional`, `Culture`, and
  `Unexpected`. The field set and labels still require user approval.
- Saving, validation, and errors are factual and nonjudgmental. The feature
  must not state or infer a balance, budget result, or financial judgment.

## Responsibility Boundaries

| Layer | Responsibility | Must not do |
| --- | --- | --- |
| Domain | Enforce existing transaction invariants for a new expense. | Depend on forms, HTTP, EF Core, or balances. |
| Application | Provide a create-for-date command and return a result suitable for a backend refresh. | Let the Client create identifiers or own business validation. |
| Infrastructure | Persist the approved transaction through existing mapping/migration boundaries. | Add finance tables or startup migration. |
| API and contracts | Expose one additive create operation with boundary validation and aligned OpenAPI/convenience contracts. | Expose domain entities or accept Client-calculated values. |
| Client | Render, submit, report state, and refresh from backend data. | Independently assign date, calculate totals, or make a local list authoritative. |

## Acceptance Criteria

| Criterion | Observable outcome | Completion evidence |
| --- | --- | --- |
| AC-01: Date-contextual entry | With readable day detail, the inspector identifies the selected date and exposes an accessible form collecting only amount and classification. | Client semantic/interaction checks and hosted desktop browser proof. |
| AC-02: Valid creation | Valid values create one persisted transaction for the selected date. The inspector rereads backend data and displays it only on that date. | Domain/Application/API integration, real PostgreSQL, Client, and browser evidence. |
| AC-03: Validation and recovery | Missing, malformed, non-positive, or unsupported input is rejected by the authoritative backend path and presented accessibly. Failed save creates nothing and retains recoverable valid form values. | Unit, API problem-details, and Client validation-state checks. |
| AC-04: Selection safety | A form and result always match the current selected date. An obsolete response cannot be shown as data for a new date. | Focused Client state/race test and browser proof. |
| AC-05: Bounded create slice | No edit/removal, note/merchant, totals/balances, category management, or visual-enhancement scope is introduced. | Scoped implementation and audit review. |

## Validation Boundaries

Domain/Application tests prove invariants. API integration and the checked
OpenAPI artifact prove transport validation and the additive create contract.
The repository's supported real PostgreSQL path proves persistence when its
documented environment prerequisite is available; otherwise that evidence is
reported as `Blocked`, not substituted with another provider.

Client component tests prove form semantics, date-change state replacement,
validation, and refresh. Hosted desktop browser evidence is authoritative for
the select-date, record-expense, see-refreshed-list journey. Test Architecture
selects the exact files and selectors after approval.

## Dependency and Ownership Plan

Implementation is not authorized. Exact files are assigned only in approved
planning; the required layer sequence is:

1. Confirm Feature 005 is complete and resolve applicable Feature 003
   governance risk.
2. Domain/Application adds only the create command/use case.
3. Infrastructure persists only the approved existing transaction model.
4. API/contracts publishes the additive create operation and checked OpenAPI
   artifact through the existing workflow.
5. Client wires the selected-day inspector form and refresh handling.
6. Test Architecture validates each layer; Documentation audits AC-01 through
   AC-05.

## Definition of Done

Feature 006 is done only after explicit user approval and a read-only audit
marks AC-01 through AC-05 `Met` from retained current code and evidence. The
audit must prove a user can create exactly one valid expense for a selected
calendar date, backend validation controls invalid input, persisted data is
reread rather than invented locally, and Feature 007 edit/delete behavior is
absent.

## Open Questions Requiring User Approval

1. Confirm that calendar selection fixes the date for creation.
2. Confirm Feature 005's proposed minimum fields and four classifications.
3. Confirm a deterministic same-day ordering choice from Feature 005, or defer
   any meaningful order until a later approved feature.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Original Feature 006 considered pane-preference persistence. | Persistence is not required for the first calendar-to-transaction workflow. |
| 2026-08-06 | Replace pane-preference persistence with first transaction creation. | The user reprioritized functional selected-day recording over non-MVP workspace preferences. Pane persistence remains deferred and receives no new feature number here. |
| 2026-08-06 | Keep creation date-contextual and non-editable. | Calendar selection is the workflow's first interaction and avoids duplicate date entry. |

## Dependencies

- Feature 005 supplies the approved transaction model and selected-day read
  path.
- Feature 007 depends on this first-entry workflow.

<!-- Superseded 2026-08-06 pane-preference-persistence draft retained as historical context.
# Feature 006: Workspace Pane Preference Persistence

## Status

Status: Draft - awaiting explicit user approval; implementation is not
authorized.

- This is a proposal only. It cannot start implementation until the user
  explicitly approves this document.
- Proposal date: 2026-08-04.
- Depends on the approved, pending implementation workspace shell in
  [Feature 003](003-workspace-navigation-and-ui-foundation.md), which defines
  transient desktop and narrow-screen pane defaults only.

## Context and Outcome

Feature 003 makes pane state responsive for the current session. Whether a
household benefits from remembering navigation and inspector visibility is a
separate product, privacy, accessibility, and migration decision.

**Proposed outcome:** A later approved slice can decide whether pane preference
persistence is useful and appropriate. If approved, it can define a respectful,
understandable persistence lifecycle with a fallback and reset path. No storage
mechanism is selected by this proposal.

## Goals

1. Decide whether pane state should persist at all, and for which contexts.
2. Define the intended scope: current session, browser/device, user identity,
   or another explicitly approved boundary.
3. Evaluate privacy, shared-device, retention, clearing, reset, unavailable
   storage, and version/state-migration behavior.
4. Define accessible restoration and reset behavior so a stored state does not
   surprise or trap keyboard and assistive-technology users.

## Non-Goals and Explicitly Out of Scope

- Assuming persistence is desired or choosing browser storage, cookies,
  server-side storage, synchronization, or any other mechanism before a
  product decision.
- Changing Feature 003's initial desktop-expanded and narrow-collapsed defaults
  for users without a valid approved preference.
- Cross-device synchronization, accounts, authentication, analytics, or
  profile management.
- Calendar selection, inspector details, navigation destinations, entry
  recording, or financial data persistence.

## Kakeibo and Calendar UX Rules

- Remembered pane state may reduce repetition, but it must not hide the
  calendar or make a household feel required to configure the workspace.
- The default and reset experience remains calm, predictable, and
  nonjudgmental.
- Any stored state is limited to what is necessary for the approved outcome and
  is explained clearly enough for a shared-device user to understand.

## Product Decisions and Open Questions

1. Is persistence desired, and which pane states, breakpoints, and contexts
   should it cover?
2. Is the preference scoped to a session, device/browser, authenticated user,
   or not stored at all?
3. What privacy risk exists on shared devices, and what disclosure, consent,
   retention, clearing, or opt-out behavior is appropriate?
4. How does the experience behave when storage is unavailable, blocked,
   corrupted, stale, or from an earlier state version?
5. Where can users reset the preference, and how do restored states preserve
   focus, landmarks, keyboard reachability, and narrow-screen usability?
6. If a mechanism is later approved, what state versioning and migration rules
   are required?

## Readiness Criteria

| Criterion | Observable decision or outcome | Completion evidence |
| --- | --- | --- |
| RC-01: Persistence decision | The user explicitly chooses persistence or no persistence, with its intended user benefit. | Recorded product decision. |
| RC-02: Scope and privacy | The proposed scope, shared-device implications, disclosure, clearing, and retention behavior are defined. | Privacy review and approved behavior. |
| RC-03: Fallback and reset | Users have defined behavior for unavailable or invalid state and a usable reset path. | UX/accessibility review. |
| RC-04: State evolution | Any approved stored state has versioning and migration or discard rules. | Approved state-lifecycle decision. |
| RC-05: Mechanism justification | A storage mechanism is selected only after RC-01 through RC-04 prove it meets the approved need. | Architecture decision linked to requirements. |

## Proposed Dependency-Ordered Waves

These are design/readiness waves, not implementation authorization.

1. Decide whether remembering pane state serves a real household outcome.
2. Define scope, privacy, reset, accessibility, fallback, and state-evolution
   requirements.
3. Evaluate storage mechanisms against those requirements using platform-
   supported options where appropriate.
4. Create a separately approved implementation specification only if
   persistence remains justified.

## Definition of Done

This proposal is ready for implementation planning only after user approval and
the recorded decisions satisfy every readiness criterion. It is complete only
when a later approved implementation feature has audited acceptance evidence.
This proposal does not authorize or claim implementation, validation, or
completion.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Created separately from Feature 003. | Transient responsive defaults do not establish that persistent user preferences are wanted or safe. |

## Dependencies

- [Feature 003](003-workspace-navigation-and-ui-foundation.md) defines only
  the baseline transient pane behavior to which any future reset falls back.
-->
