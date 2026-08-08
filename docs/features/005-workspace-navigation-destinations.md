# Feature 005: Daily Transaction Read Model and Empty-Day Inspector

## Status

Status: Draft - awaiting explicit user approval; implementation is not authorized.

- Proposal date: 2026-08-06.
- Depends on Feature 004, treated as complete by the current roadmap: it supplies the transient selected calendar date. This feature neither persists nor route-addresses selection.
- Feature 003's audit did not pass its Lucide dependency-governance criterion. This is a distinct implementation risk, not a blocker for planning or user approval; its owner must resolve it before relying on shell implementation.

## Context and Outcome

HouseholdLedger is a calendar-centered household ledger informed by Kakeibo, a household-accounting practice of recording spending so it can later be considered with care. Calendar date selection is now the first interaction, but the right inspector has no transaction data or implementation.

**Proposed outcome:** Selecting a calendar date makes the right inspector request a backend-owned read model for that date and show the date's transaction list. A day without transactions is shown truthfully as empty.

This is an observable vertical slice, not infrastructure alone: selecting a different date replaces inspector content with that date's actual empty or populated backend result. Feature 006 creates the first populated result.

## Goals

1. Establish the smallest durable expense-transaction concept needed by first recording.
2. Provide one date-scoped Application/API read use case and persistence path.
3. Render loading, unavailable, empty, and returned-record states in the right inspector without fabricating data.
4. Keep list production in backend layers; Client renders received data only.

## Non-Goals

- Creating, editing, deleting, importing, recurring, splitting, or attaching transactions; Features 006 and 007 own the first write outcomes.
- Totals, balances, budgets, planning, reports, accounts, income, transfers, savings goals, reflections, or financial advice.
- Custom categories, authentication, household sharing, multi-currency, settings, date persistence, responsive redesign, navigation destinations, or pane preference persistence.
- Visual polish beyond clear, accessible, no-frills states.

## Kakeibo and Calendar Rules

- A transaction is an expense recorded on exactly one calendar date. Its date is date-only ledger data, not browser time or payment-account data.
- The proposed minimum record is opaque identifier, date, positive monetary amount, and one spending classification. Identifier is persistence/transport identity, not user-editable content.
- Proposed required classifications are the four broad Kakeibo-inspired labels `Necessities`, `Optional`, `Culture`, and `Unexpected`. This constrained set avoids a custom taxonomy, but both it and required classification need user approval.
- Description/note is deliberately absent from the MVP record. Empty means no stored transactions returned, never a zero balance or judgment.

## Responsibility Boundaries

| Layer | Responsibility | Must not do |
| --- | --- | --- |
| Domain | Define transaction invariants: one date, positive amount, allowed classification. | Depend on EF Core, HTTP, configuration, or Client state. |
| Application | Define date query and produce ordered day read model. | Let Client build lists or calculate totals/balances. |
| Infrastructure | Map/query approved model through PostgreSQL/EF Core boundary. | Apply migrations at startup or leak provider types upward. |
| API and contracts | Expose additive date-scoped read endpoint with validated boundary and aligned OpenAPI/convenience contract. | Expose domain/EF entities. |
| Client | Request on selected-date change and render all truthful read states in inspector. | Calculate lists/totals/balances or persist directly. |

## Acceptance Criteria

| Criterion | Observable outcome | Completion evidence |
| --- | --- | --- |
| AC-01: Selected-day read | Selecting a date requests and identifies that same date's detail. Selecting another date replaces its result. | Focused Client interaction check and hosted desktop browser proof. |
| AC-02: Truthful empty day | No returned transactions visibly identifies the date and says it has no recorded transactions, with no fabricated record, total, balance, or existing-record action. | Application/API empty-result, Client state, and browser evidence. |
| AC-03: Durable bounded read model | Persisted transaction has only approved minimum fields and is returned only for recorded date through one additive contract. Ordering is deterministic and documented during approved planning. | Domain/Application, API contract, real PostgreSQL, and checked OpenAPI evidence. |
| AC-04: Honest failure behavior | Loading, unavailable, and invalid-boundary states are communicated without showing a prior day's list as new-day data. | Focused Client/API failure-path checks and browser proof. |
| AC-05: Bounded no-frills slice | No write operation, calculated total/balance, accounts, custom categories, planning/reporting, authentication, or visual-enhancement program enters scope. | Scoped implementation and audit review. |

## Validation Boundaries

Domain/Application tests are primary evidence for amount, classification, date filtering, ordering, and empty result. API integration proves validation, controller/use-case mapping, checked OpenAPI, and problem details. The isolated real PostgreSQL test path is persistence evidence; if its documented Podman/WSL prerequisite is unavailable, it is reported as `Blocked`, not substituted with another provider.

Client component tests prove selected-date replacement and visible states. The hosted Client at the established desktop viewport is authoritative visual proof that calendar date and inspector stay connected. Test Architecture selects exact files and selectors after approval.

## Dependency and Ownership Plan

Implementation is not authorized. Exact files are assigned only in approved planning; required layer sequence is:

1. Resolve applicable Feature 003 Lucide governance risk.
2. Domain defines approved transaction value rules.
3. Application/Infrastructure add query port/use case, mapping, and migration.
4. API/contracts publish additive read contract and checked OpenAPI artifact.
5. Client wires Feature 004 selected date to inspector state rendering.
6. Test Architecture supplies proportionate tests; Documentation audits AC-01 through AC-05.

## Definition of Done

Feature 005 is done only after explicit user approval and a read-only audit marks AC-01 through AC-05 `Met` with retained current evidence. The audit must prove selected-date-to-inspector behavior against the real API contract, a truthful empty state, backend-enforced minimum fields, and absence of write, total, balance, or deferred scope.

## Open Questions Requiring User Approval

1. Approve or change expense-only minimum: date, positive amount, required classification, and no description/note.
2. Approve or change the proposed four Kakeibo-inspired classifications.
3. Decide same-day ordering when no time-of-day exists. Stable identifier order is technically small but has little user meaning; explicit ordering adds model scope.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Original Feature 005 proposed a Settings destination. | It addressed a then-prioritized navigation idea. |
| 2026-08-06 | Replace Settings proposal with selected-day transaction read detail. | The user reprioritized MVP around calendar selection and functional day recording. Settings remains deferred; no new feature number is created. |
| 2026-08-06 | Make first transaction slice read-only but vertical. | Selected-date inspector result, including honest empty result, proves cross-layer contract before writes. |

## Dependencies

- [Feature 004](004-calendar-item-selection-and-inspector-detail-contract.md) supplies completed transient selected-date interaction.
- Feature 006 depends on this transaction model and selected-day read contract.
- Feature 007 depends on Feature 006's first-entry workflow.

<!-- Superseded 2026-08-06 Settings-destination draft retained as historical context.
# Feature 005: Daily Transaction Read Model and Empty-Day Inspector

## Status

Status: Draft - awaiting explicit user approval; implementation is not
authorized.

- This replacement proposal is not approval for product, test, configuration,
   or runtime work.
- Proposal date: 2026-08-06.
- Depends on Feature 004, which the current roadmap treats as complete: it
   supplies the transient selected calendar date. Feature 005 does not persist,
   route-address, or otherwise change that selection.
- Feature 003 remains a distinct implementation risk: its audit did not pass
   its Lucide dependency-governance criterion. That risk does not prevent this
   roadmap from being planned or approved, but its owning work must be resolved
   before a feature implementation relies on the workspace shell.

## Context and Outcome

Feature 003 deliberately renders a `Navigation` heading without destinations
so the workspace does not promise unavailable product areas. The user has now
identified one minimum destination: Settings must be available from, and stay
pinned to the bottom of, the desktop left navigation pane. The user has not
defined a finite information architecture or any further destinations.

HouseholdLedger is a calendar-centered household ledger informed by Kakeibo, a
household-accounting practice of recording, planning, and reflection. Settings
is a distinct workspace destination, but this slice must not present settings
as already configured or displace the calendar as the default work surface.

**Proposed outcome:** A desktop user can use an accessible `Settings`
navigation destination, anchored at the bottom of the left navigation pane, to
reach a deliberately minimal Settings page. The page truthfully presents a
neutral no-settings-yet state. The root calendar route remains the default,
central workspace.

## Goals

1. Add exactly one usable destination: a labeled `Settings` navigation link
    that routes to a minimal Settings page.
2. Keep the Settings link at the bottom of the expanded desktop navigation
    pane with normal-flow flex or grid anchoring, rather than fixed, absolute,
    or overlay positioning.
3. Make the destination and page accessible through a navigation landmark,
    native link semantics, keyboard operation, visible focus, and an accurate
    current-route indication.
4. Preserve the root calendar route as the default route and the calendar as
    the central work surface when that route is active.
5. State honestly that Settings is a destination with no available settings
    yet; do not imply that configuration is implemented.

## Non-Goals and Explicitly Out of Scope

- Any setting, control, preference, form, save action, validation, application
   configuration, culture, timezone, currency, week-start, or time-format
   behavior.
- A settings model; Client, browser, or database persistence; retrieval; API,
   Application, Domain, Infrastructure, contract, OpenAPI, migration, or
   authorization work.
- Calendar behavior, date selection, inspector behavior, entry recording,
   financial data, calculations, or changes to Feature 004's calendar contract.
- Additional destinations, placeholder links, accounts, reports, categories,
   hierarchy, sidebar groups, a sitemap, a generic navigation abstraction, or
   invalid/unavailable-route policy beyond framework-default behavior.
- A responsive or narrow viewport requirement, an overlay, a fixed-position
   floating control, or changes to Feature 003 pane dimensions, default states,
   ordering, or toggle controls.
- A new dependency, UI library, visual component framework, icon source, or
   project/configuration/lock-file change. The approved Feature 003 Lucide
   Blazor/.NET package may be reused only when it is already available; this
   slice does not authorize adding or changing it.

## Kakeibo and Calendar UX Rules

- The calendar remains the default route and central workspace for calm review
   and orientation. Settings is supporting navigation, not a financial
   dashboard or a competing home surface.
- `Settings` is clear, neutral language for the future place where a household
   may manage application preferences. Its empty state makes no claim about a
   household's money, choices, records, or required next action.
- The Settings page identifies itself with a plain `Settings` heading and a
   neutral no-settings-yet message. It does not render example preferences,
   disabled controls, or explanations that imply configuration exists.
- Broader workspace navigation remains intentionally deferred until the user
   defines the next real destination and its user outcome.

## Design and Accessibility Contract

- The left navigation remains a named navigation landmark. Its Settings item
   is a native Client route link with a visible `Settings` label or an
   equivalent visible text label. It must be usable without icon recognition.
- When the already-approved Lucide integration is available, a recognizable
   Lucide Settings icon may supplement the label. A decorative icon is hidden
   from assistive technology; a meaningful icon has equivalent accessible text.
   The icon is optional and is not a reason to change dependencies.
- The expanded desktop left pane uses its existing normal-flow layout with a
   flexible middle track or equivalent so the Settings link sits at the pane's
   bottom edge. It scrolls or reflows only according to the existing shell's
   normal-flow behavior; it is not fixed to the viewport, absolutely positioned,
   or layered over the calendar or pane content.
- The current destination is visibly distinguishable without color or position
   alone and exposes a programmatic current-page indication. On the Settings
   route, Settings is current; on the calendar root route, it is not current.
- `Tab` and `Shift+Tab` follow normal document order. The Settings link is
   reachable by keyboard, has visible focus, and native link activation through
   `Enter` changes the route. After route navigation, focus follows the
   framework's established route-navigation behavior; this feature does not
   prescribe custom focus scripting. The collapsed-pane behavior remains owned
   by Feature 003.
- The Settings page has a descriptive main-content heading and does not create
   duplicate page landmarks. No narrow viewport behavior is specified.

## Acceptance Criteria

| Criterion | Observable outcome | Completion evidence |
| --- | --- | --- |
| AC-01: Pinned Settings destination | At Feature 003's `1440 x 900` desktop viewport, the expanded left navigation pane contains exactly one destination item: `Settings`. It remains at the pane's bottom in normal flow and does not overlay the calendar or other pane content. | Hosted desktop browser geometry and interaction proof; scoped Client markup/CSS review. |
| AC-02: Reachable, honest Settings page | Pointer or keyboard activation of Settings routes to a page headed `Settings` that presents only a neutral no-settings-yet state. It contains no controls, preference values, configuration behavior, financial content, or implied implementation of settings. | Focused Client component/route checks and hosted desktop browser proof. |
| AC-03: Accessible route navigation | Navigation is exposed through a named navigation landmark and Settings through native link semantics with a visible or equivalent accessible label. It is keyboard reachable, visibly focused, and activates with `Enter`. The active route has matching visible and programmatic current-page state; Settings is not current on the calendar root route. | Focused Client semantic and interaction checks plus browser keyboard proof. |
| AC-04: Calendar-centered default remains intact | The root calendar route remains the default route and retains its central desktop calendar work surface. Adding Settings does not change calendar, inspector, pane-toggle, selection, or data behavior. | Focused regression checks for the root route; hosted browser proof; scoped diff review. |
| AC-05: Bounded Client-only slice | Only the Settings navigation destination and its minimal neutral route page are introduced. No settings implementation, persistence, non-Client change, dependency/configuration change, responsive behavior, or additional information architecture is introduced. | Scoped diff and package/configuration inspection; audit of implementation and retained validation evidence. |

## Authoritative Validation Boundaries

The published hosted Client at the existing one-API HTTPS root URL, observed at
Feature 003's `1440 x 900` desktop viewport, is authoritative visible proof.
It verifies the bottom-anchored expanded-pane placement, visible focus and
keyboard route activation, Settings current-route state, the neutral Settings
page, and the unchanged calendar root route.

Focused Client component and route tests are primary proof for landmark/link
semantics, accessible naming, current-page state, normal keyboard activation,
and the absence of Settings controls or configuration behavior. Browser
evidence supplements, rather than replaces, these deterministic checks. Test
Architecture selects the exact tests and selectors.

No API, database, OpenAPI, persistence, settings-model, full-solution, or
narrow-viewport validation belongs to this slice. No new dependency review is
needed unless an implementation incorrectly proposes a package change, which is
outside this specification.

## Dependency-Ordered Implementation Waves

Implementation is not authorized. After explicit user approval, the
orchestrator assigns exclusive ownership sequentially. Each specialist uses its
own terminal and does not inspect, reuse, stop, or send input to another
specialist's process. No wave may edit a file or directory assigned to another
wave.

| Order | Specialist | Exclusive writable ownership | Responsibility and no-overlap constraint | Dedicated resources and required checks |
| --- | --- | --- | --- | --- |
| 1 | Blazor Workspace UI | `src/HouseholdLedger.Client/Layout/MainLayout.razor`, `MainLayout.razor.cs`, `MainLayout.razor.css`, `src/HouseholdLedger.Client/Pages/SettingsPage.razor`, `SettingsPage.razor.cs`, and `SettingsPage.razor.css` | Add only the Settings link, normal-flow bottom anchoring, minimal route page, and documented semantics. Reuse existing routing/layout and the already-available approved Lucide package only if applicable. Do not edit calendar, inspector, tests, projects/configuration, or non-Client layers. Publish route, selector, and semantic handoff to wave 2. | One dedicated terminal; no server and no reserved port. Run the narrow Client build after wave 2 releases compatible tests. |
| 2 | Test Architecture | Only feature-specific files under `tests/HouseholdLedger.Client.ComponentTests/` and `tests/HouseholdLedger.EndToEndTests/` | Add focused checks for AC-01 through AC-05, including normal-flow desktop placement, link/landmark/current-page semantics, keyboard route activation and visible focus, neutral page content, root-calendar continuity, and boundary exclusions. Do not edit production, configuration, or browser dependency policy. | One dedicated terminal; unique test-owned publish root, browser profile root, output root, API port, and Client port. Confirm ports are free. Run focused component tests and the documented fresh-publish browser workflow in `docs/development/testing.md`. |
| 3 | Research and Documentation | `docs/audit/` only, in a separately assigned audit report | Audit AC-01 through AC-05 and this Definition of Done against current code and retained validation evidence. Do not edit production, tests, configuration, or this draft specification without a new documentation assignment. | One dedicated terminal; read-only evidence access; no ports. Produce a criterion-to-evidence verdict. |

Wave 2 begins only after wave 1 releases its files and handoff. Wave 3 begins
only after the validation evidence from wave 2 is retained. No wave may alter
`BudgetExperiment/`, artifacts, other feature documents, solution/project
files, lock files, runtime configuration, or the future persisted-settings
proposal.

## Definition of Done

Feature 005 is done only when all of the following are true:

1. The user explicitly approves this refined draft before implementation.
2. A read-only audit marks AC-01 through AC-05 `Met` with current code and
    retained focused Client and authoritative desktop browser evidence.
3. The audit confirms Settings is the sole destination item, remains
    bottom-anchored in normal flow at `1440 x 900`, and does not overlay the
    calendar or pane content.
4. The audit confirms accessible landmark, link, label, focus, keyboard, and
    current-page behavior, plus the deliberately neutral no-settings-yet page.
5. The audit confirms the calendar root remains the default central workspace
    and no calendar, inspector, data, configuration, persistence, dependency,
    responsive, or broader information-architecture scope was introduced.
6. The audit confirms no work duplicated or anticipated Feature 004's future
    persisted application-settings proposal.

## Future Information Architecture Proposal

This unnumbered proposal is intentionally deferred. It allocates no feature
number and grants no implementation authority. Once the user defines another
real household outcome, a separately approved specification may decide whether
that outcome needs a distinct destination, route, ordering, active state,
invalid-route behavior, responsive behavior, and documentation. It must not
infer generic destinations from the existence of the Settings link.

## Open Questions Requiring Product Decisions

1. Which next user-defined outcome, if any, warrants another destination after
    Settings?
2. When a separately approved persisted-application-settings feature is ready,
    which settings are valuable, what defaults and validation apply, and how
    should changes affect calendar presentation?

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Created separately from Feature 003. | A navigation shell label does not establish a need for, or choice among, product destinations. |
| 2026-08-06 | Refine the first destination to a Settings link pinned to the bottom of the desktop navigation pane and a neutral empty Settings page. | The user identified Settings as the minimum needed destination but has not defined a finite navigation or information-architecture plan. |
| 2026-08-06 | Use normal-flow flex or grid bottom anchoring, not a fixed, absolute, or overlay control. | The user's word `float` is interpreted as visual placement within the pane while preserving Feature 003's no-overlay workspace rule. |
| 2026-08-06 | Keep settings implementation and persisted application preferences deferred. | Feature 004 already records a separate future proposal for configuration UI, model, persistence, and calendar-affecting preferences. |

## Dependencies

- [Feature 003](003-workspace-navigation-and-ui-foundation.md) owns the
   desktop workspace shell, including the navigation region, pane behavior, and
   approved Lucide dependency policy.
-->
- [Feature 004](004-calendar-item-selection-and-inspector-detail-contract.md)
   retains the separate future proposal for persisted application settings and
   its effect on calendar presentation.
