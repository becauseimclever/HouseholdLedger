# Feature 005: Record a Transaction for the Selected Day

## Status

Status: Complete.

- Domain, Application, PostgreSQL mapping and migration, API contracts and
   endpoints, checked OpenAPI, shared Client date state, inspector form, backend
   reread, and focused xUnit coverage are implemented.
- The owned PostgreSQL 18 harness passes migration, create, reread, correction,
  and removal persistence checks with no skips and cleans its container.
- The hosted Firefox journey passes against a fresh Release API publish and the
  isolated PostgreSQL database, proving date selection, creation, backend
  reread, correction, cancellation, confirmed removal, and the final empty
  reread.
- Checked OpenAPI matches runtime generation, and locked Release validation
  passes across the solution.

- Planned: 2026-08-31.
- Feature 004 supplies the existing calendar day-selection interaction.
- This plan replaces the former read-only Feature 005 milestone and absorbs the
  create behavior formerly proposed as Feature 006. A read-only empty inspector
  is not a useful milestone on its own.

## Outcome

When a user selects a calendar day, the right inspector opens for that date and
lets the user record one expense. After a successful save, the inspector rereads
that date from the backend and displays the persisted transaction.

This is the first functional HouseholdLedger vertical slice. It includes the
minimum read behavior needed to show an honest empty state and to verify a save;
neither local-only entries nor a write-only endpoint satisfy the outcome.

## User Flow

1. The user selects a day in the existing Today, Week, or Month calendar view.
2. The right inspector opens, identifies the selected date, and loads that day's
   transactions.
3. The inspector shows an empty state when none exist and an entry form with
   amount and classification fields.
4. The user enters a positive USD amount, chooses a classification, and saves.
5. The backend validates and persists the expense for the selected date.
6. The Client rereads the selected date and shows the saved transaction.

Changing the selected date at any point replaces the inspector context. A late
load or save response for an earlier date must not overwrite the current date's
state.

## Minimum Transaction Contract

| Field | Rule |
| --- | --- |
| Identifier | Backend-generated opaque identifier; stable and not user-editable. |
| Date | Required date-only ledger value fixed by calendar selection. The form has no date input. |
| Amount | Required positive USD amount, represented as .NET `decimal` and PostgreSQL `numeric(18,2)`. More than two fractional digits is invalid rather than rounded silently. |
| Classification | Required value: `Necessities`, `Optional`, `Culture`, or `Unexpected`. |

USD is fixed for this first slice and displayed explicitly. Currency selection,
household currency settings, conversion, and multi-currency records are deferred.
Transactions for a date are returned oldest first by backend-assigned creation
sequence. No user-entered time or timestamp is introduced.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Domain | Define the expense transaction and enforce date, positive amount, two-decimal precision, and classification invariants. |
| Application | Provide create-for-date and list-for-date use cases and ports. Return deterministic read models. |
| Infrastructure | Map and persist transactions with EF Core/Npgsql, including an explicit migration. Never migrate automatically at startup. |
| API and contracts | Expose additive selected-date list and create operations, transport validation, Problem Details, and aligned checked OpenAPI. Do not expose domain or EF entities. |
| Client | Own shared transient selected-date state, open the inspector on selection, submit through HTTP, and reread backend data. Do not make a local list authoritative. |

The current implementation keeps `ActiveDate` private to `CalendarPage` while
the inspector is rendered by `MainLayout`. Implementation therefore needs one
Client-scoped selected-date state service, registered through dependency
injection, that notifies both components. This state remains transient and is
not persisted or placed in the URL.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Selection opens entry | Selecting any visible calendar day opens the right inspector, identifies that date, and presents amount and classification controls. The date is not independently editable. |
| AC-02: Honest daily read | The inspector loads only the selected date's backend records and visibly distinguishes loading, empty, populated, and unavailable states without showing a prior date's data. |
| AC-03: Valid creation | A positive USD amount with at most two decimal places and an allowed classification creates exactly one persisted transaction for the selected date. A backend reread then displays it oldest first. |
| AC-04: Validation and recovery | Missing, malformed, zero, negative, over-precision, or unsupported values create nothing. Accessible field or summary errors are shown and recoverable entered values are retained. |
| AC-05: Selection safety | Changing dates resets the inspector context immediately. Obsolete load or save responses cannot appear under the newly selected date. |
| AC-06: Accessible operation | Labels, validation association, status announcements, keyboard operation, focus behavior, and the inspector's expanded state remain understandable without pointer input. |
| AC-07: Bounded slice | No edit/delete, notes, merchant, account, income, transfer, totals, balances, budgets, custom categories, authentication, or currency settings are added. |

## Implementation Plan

1. **Domain and Application:** add the minimum transaction model, classification
   values, create/list use cases, ports, and focused invariant/use-case tests.
2. **Persistence:** add the EF mapping, repository adapter, deterministic
   creation sequence, and explicit PostgreSQL migration.
3. **API:** add request/response contracts, selected-date list and create
   endpoints, Problem Details mapping, OpenAPI updates, and focused integration
   tests.
4. **Client state and inspector:** lift selected date into a scoped state service,
   connect calendar selection to `MainLayout`, add the form and truthful read/save
   states, and protect state from obsolete asynchronous responses.
5. **Journey validation:** prove select date -> empty inspector -> save -> backend
   reread -> visible transaction through component tests and one hosted browser
   journey.

No new runtime dependency is expected. Existing .NET, Blazor, EF Core, Npgsql,
and xUnit facilities are sufficient.

## Validation

- Domain and Application unit tests cover every invariant, date filtering,
  creation, and deterministic ordering.
- API integration tests cover valid creation, invalid Problem Details responses,
  empty/populated reads, date isolation, and create-then-read behavior.
- Infrastructure integration tests prove mapping, precision, ordering, and date
  filtering against the repository's real PostgreSQL test path. An unavailable
  configured database is reported as blocked, not replaced with another provider.
- Client component tests cover selection notification, inspector expansion,
  form semantics, validation/recovery, refresh, and obsolete-response handling.
- One hosted desktop browser test proves the complete user flow. Existing
  calendar keyboard behavior and solution Debug build remain regression checks.

## Definition of Done

The feature is complete when a user can select a date, record one valid expense,
and see the persisted result after a backend reread; invalid input and stale
responses are handled as specified; focused tests pass; checked OpenAPI is
current; the explicit migration is present; and the full solution builds.

A separate documentation audit is not required for completion unless requested.

## Deferred Work

- Feature 007 owns correction and deletion of an existing transaction.
- Description, merchant, accounts, income, transfers, recurring entries, splits,
  imports, attachments, totals, balances, budgets, planning, reports, custom
  categories, authentication, and multi-currency remain outside this feature.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-31 | Combine selected-day read, empty state, and first creation in Feature 005. | The combined flow is the smallest slice that lets a user accomplish ledger work and verify persisted truth. |
| 2026-08-31 | Fix the MVP currency to USD with two decimal places. | The user selected USD; an explicit fixed currency avoids ambiguous durable money without adding settings or multi-currency. |
| 2026-08-31 | Keep the four Kakeibo-inspired classifications. | A small fixed vocabulary supports meaningful first recording without category management. |
| 2026-08-31 | Order same-day records by backend creation sequence, oldest first. | This is deterministic and matches entry history without adding user-entered time. |
| 2026-08-31 | Use shared transient Client state for the selected date. | Calendar and layout currently own separate component scopes; both need one framework-aligned notification boundary. |

## Dependencies

- [Feature 004](004-calendar-item-selection-and-inspector-detail-contract.md)
  supplies calendar date selection.
- Existing hosted Client/API, PostgreSQL boundary, OpenAPI workflow, and xUnit
  test projects supply the implementation and validation homes.
