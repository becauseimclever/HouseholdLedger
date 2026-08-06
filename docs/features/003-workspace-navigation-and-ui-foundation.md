# Feature 003: Workspace Navigation and UI Foundation

## Status

Status: Approved / implementation in progress

- Approval authority: the user. The orchestrator cannot approve this document
  on the user's behalf.
- Approval record: the user authorized implementation through completion on
  2026-08-04. The user approved the desktop-only scope refinement recorded on
  2026-08-06. Deferring expressly unimplemented responsive behavior does not
  require a further approval renewal.
- Implementation state: completed implementation-wave work remains authorized.
  This specification does not claim that later waves, validation, audit, or
  final feature completion have occurred.
- Research date: 2026-08-04.
- Relationship to Feature 002: Feature 002 is the completed hosted runtime
  proof. This feature changes the visible Client workspace only after approval;
  it does not reopen hosting, API, persistence, or financial-product scope.

## Context and Outcome

HouseholdLedger is a calendar-centered household ledger informed by Kakeibo.
Kakeibo is a household-accounting practice that combines recording, planning,
and reflection. A future ledger needs a quiet, dependable place to view time
and make those activities understandable without judging a household's choices.

The user wants a utility-application workspace influenced by Visual Studio and
VS Code: collapsible left navigation, a permanent calendar work area in the
center, and an expandable right-hand inspector. The request spans more than one
user-observable capability. The recommended first slice is only the workspace
shell: it proves that the calendar remains the central work surface while a
user can independently reveal or hide the surrounding panes. The inspector is
honest about its empty state because calendar selection and ledger data do not
yet exist.

**Outcome:** At the root calendar route, desktop users see a bespoke
three-region workspace. They can independently collapse and expand navigation
and inspector panes with accessible controls. Neither pane overlays or obscures
the calendar. The calendar remains the sole central temporary work surface, and
the inspector states that no calendar item is selected. Narrow viewport
behavior is deliberately deferred.

## Goals

1. Replace the temporary centered-page frame with a calendar-centered workspace
   shell at the existing root route.
2. Provide independent, accessible collapse and expand controls for left
   navigation and right inspector regions.
3. Keep the existing calendar temporary content as the only central work
  surface. Rename its placeholder heading to the neutral `Calendar` heading;
  it may otherwise be restyled to fit the shell but gains no record,
  selection, editing, date-navigation, or financial behavior.
4. Provide a neutral inspector empty state that truthfully communicates that no
   calendar item is selected.
5. Establish only the CSS and Blazor primitives necessary for this shell, using
   a bespoke visual language rather than a third-party UI component library.
6. Preserve the successful one-host, hosted Blazor WebAssembly runtime from
   Feature 002 and the Client's one-way dependency boundary.
7. Establish and verify the desktop workspace at 1440 x 900 without committing
  to narrow viewport behavior.

## Non-Goals and Explicitly Out of Scope

- Calendar item selection, keyboard selection, focused-day semantics, or a
  selection-to-inspector interaction contract.
- Ledger entries, accounts, categories, budgets, savings plans, reflections,
  totals, financial calculations, seed records, or fake financial content.
- Inspector fields, editing, saving, validation, commands, data fetching,
  persistence, API endpoints, contracts, migrations, or use cases.
- Concrete navigation destinations, routing, active-route behavior, or menu
  items that imply unavailable product areas. This feature shows the
  `Navigation` region heading only and contains no destination items.
- A reusable application-wide component library, theme switcher, design-token
  package, or a third-party UI library or visual component framework. The
  narrow Lucide exception in this document does not authorize another icon
  source, a general icon abstraction, or a UI component package.
- Changes to API, Domain, Application, Infrastructure, hosting, browser runtime
  dependencies, or Feature 001/002 validation scope. The only permitted
  project-configuration changes are the central version, Client package
  reference, and Client lock-file update strictly required to add the approved
  official Lucide Blazor/.NET icon package after the existing dependency review
  is completed and approved.
- Overlay drawers that cover the calendar, persistent user preferences, and
  cross-device pane-state synchronization.
- Responsive or narrow viewport workspace behavior, including narrow default
  pane states, narrow normal-flow ordering, 500 x 844 proof, and page-level
  overflow behavior at narrow widths. These require a separately approved
  future proposal.
- JavaScript viewport detection or viewport interop. The former minimal JS
  viewport interop is no longer needed or authorized by Feature 003.

## Kakeibo and Calendar UX Rules

- The calendar is the permanent primary work area. Navigation and inspection
  support orientation and future detail work; they must not displace the
  calendar's role or compete with it visually.
- The shell must not imply that recording, planning, saving, or reflection is
  already possible. Its copy remains neutral and factual.
- The inspector empty state says `No calendar item selected`, or communicates
  the same fact in similarly plain language. It does not fabricate a selected
  day, transaction, balance, or suggested action.
- A calendar cell, date, or temporary calendar control does not become
  selectable merely because the shell exists. Selection behavior is reserved
  for a later approved feature.
- The workspace supports calm review: stable regions, clear labels, restrained
  contrast, and no judgmental financial language, urgency, or gamification.
- At the required desktop viewport, both panes are expanded by default. This is
  an initial, transient display state only; no preference persistence is
  implied.
- The navigation region displays the `Navigation` heading and no destination
  items. It identifies the future workspace region without suggesting that any
  destination is available.

## Bespoke Design-System Constraints

This slice deliberately uses small, local Blazor markup and CSS primitives
instead of a UI library. Native HTML buttons, landmarks, CSS Grid, CSS custom
properties, media queries, and Blazor component state are framework- and
platform-supported mechanisms that meet the stated need without prematurely
defining a general component framework. This is a product-design decision, not
a claim that UI libraries are generally unsuitable.

The user authorizes the official Lucide Blazor/.NET integration strictly as an
icon source for familiar controls in this feature, such as pane toggles. It is
not a third-party UI component library or visual component framework, and its
adoption does not change the bespoke design-system decision. The assigned owner
may add it only after completing and obtaining approval for the repository's
existing dependency-governance review, including the package's complete locked
closure, license and notice obligations, provenance, vulnerabilities,
commercial-model review, and why a built-in alternative is insufficient.

- Define only the local semantic variables needed for workspace surfaces,
  borders, text, focus, spacing, and pane widths; retain the repository's
  existing visual direction where it remains suitable.
- Use CSS Grid or an equivalent normal-flow layout for regions and transitions.
  Panes must consume layout space or reflow outside the calendar, never float
  above it as modal or overlay drawers.
- Use native `<button>` controls for pane visibility. Labels, icons, and
  tooltips must remain understandable without relying on icon recognition. A
  Lucide icon may supplement a familiar control but must not be its only
  accessible label: each native button retains an accessible name and a tooltip
  or equivalent visible explanation. A decorative icon is hidden from assistive
  technology; a non-decorative icon has equivalent accessible text.
- Keep panel corners restrained (at most 0.5rem unless a documented visual
  reason requires otherwise); do not introduce decorative gradient-orb, hero,
  card-stack, or marketing-page treatment.
- Do not extract a generic component abstraction unless the assigned owner can
  demonstrate duplicate, current-slice behavior that a local primitive cannot
  express clearly.

## Accessibility and Desktop Behavior

- Use programmatically determinable landmarks for navigation, main calendar
  work area, and complementary inspector. The existing page must retain one
  descriptive `h1` in main content; pane headings follow a logical hierarchy.
- Each toggle is keyboard operable and exposes an accessible name, `aria-
  expanded`, and `aria-controls` pointing to its pane. Its announced state and
  visible state agree after every toggle.
- Collapsed panes are removed from sequential keyboard navigation and are not
  exposed as active visible content. Focus remains on the pressed toggle unless
  a user deliberately moves it.
- Existing visible-focus treatment remains visible with sufficient separation
  from adjacent controls and boundaries. Hover alone must not communicate a
  state or action.
- At 1440 x 900, both panes are expanded by default and the calendar is shown
  between them. No pane may cover the calendar. The calendar remains reachable
  and usable without page-level horizontal viewport scrolling caused by the
  workspace shell.
- The calendar's existing intentional internal horizontal scrolling, if needed
  for its grid, remains distinct from page-level horizontal overflow at the
  required desktop viewport.
- Controls have stable touch-target dimensions and labels wrap or reflow rather
  than clipping or overlapping.

## Acceptance Criteria

| Criterion | Observable outcome | Completion evidence |
| --- | --- | --- |
| AC-01: Calendar-centered workspace | The root route visibly presents left navigation, central calendar, and right inspector regions. The calendar is the only central temporary work surface, headed `Calendar`; no financial records or selection behavior appear. | Client component assertion for landmarks/content plus the authoritative browser proof. |
| AC-02: Independent pane controls | A keyboard or pointer user can collapse and expand either pane without changing the other pane's state. The relevant accessible toggle state accurately changes. | Focused bUnit interaction and accessibility-attribute assertions; authoritative browser proof for visible behavior. |
| AC-03: Honest neutral inspector | With no selection model, the inspector visibly communicates that no calendar item is selected and contains no invented detail, totals, or actions. | Client component assertion and browser-visible text inspection. |
| AC-04: Calendar remains unobscured | In expanded and collapsed pane states at the required desktop viewport, panes consume layout space rather than overlay the calendar. The calendar's main region remains visible and reachable. | Browser geometry/visibility assertions at 1440 x 900, with retained screenshots or diagnostics. |
| AC-05: Accessible desktop shell | Landmarks, heading hierarchy, keyboard controls, visible focus, accessible names, `aria-expanded`, and `aria-controls` are present. At 1440 x 900, both panes are expanded by default; shell controls and text do not overlap or create page-level horizontal overflow. | Focused component/structural checks and the authoritative browser proof at 1440 x 900. |
| AC-06: Bounded implementation | The Client uses local Blazor/CSS primitives and adds no UI library, visual component framework, or product data behavior. The only dependency exception is the approved official Lucide Blazor/.NET icon package used strictly as an icon source for Feature 003 controls; native buttons retain accessible names and tooltips or equivalent visible explanations. API, Domain, Application, Infrastructure, contracts, migrations, and hosting remain unchanged. Configuration changes are limited to the centrally managed Lucide version, the Client reference, and the regenerated Client lock file, after the repository's dependency-governance review is approved. | Dependency-review record; central-version, Client-project, and lock-file inspection; locked restore and package-closure inspection; scoped diff review; and focused Client validation. |

## Authoritative Validation Boundaries

The authoritative behavioral proof is the published hosted Client at the
existing one API HTTPS root URL, observed at `1440 x 900`. It verifies the
expanded and collapsed states of both panes, the calendar's reachable visible
region, and the neutral inspector empty state. The existing browser harness and
its approved package-free runtime remain the only browser path; this feature
does not add a browser library or a second host.

bUnit and structural assertions are supporting evidence for semantic markup and
toggle state. They do not replace the geometry proof. Direct API, database,
OpenAPI, persistence, full-solution, or browser-matrix tests are outside this
feature unless a changed shared test contract makes a focused regression check
necessary. Test Architecture selects exact tests and can reject brittle pixel
assertions that do not prove the criteria.

The Lucide package is not a browser-runtime or UI-framework exception. Before
the package manifests change, the assigned owner must follow the existing
dependency-governance procedure: record and approve the review, make the
central-version and Client-reference changes, regenerate and inspect the Client
lock file, then retain the required locked-restore, inventory, vulnerability,
license, and published-output evidence. This feature does not prescribe a new
record location or workflow beyond that policy.

## Dependency-Ordered Implementation Waves

Implementation is authorized by the recorded user approval. The orchestrator
assigns the exclusive ownership below. Every specialist uses its own terminal
and does not inspect, reuse, stop, or send input to another specialist's
process. No wave may edit a file or directory assigned to another wave.

| Order | Specialist | Exclusive writable ownership | Responsibility and no-overlap constraint | Dedicated resources | Dependencies and required checks |
| --- | --- | --- | --- | --- | --- |
| 1 | Blazor Workspace UI | `src/HouseholdLedger.Client/Layout/MainLayout.razor`, `MainLayout.razor.cs`, `MainLayout.razor.css`, `src/HouseholdLedger.Client/wwwroot/css/app.css`, `Directory.Packages.props`, `src/HouseholdLedger.Client/HouseholdLedger.Client.csproj`, and `src/HouseholdLedger.Client/packages.lock.json` | After the completed dependency-governance review, implement the layout regions, local visual primitives, pane state, native accessible toggles, and the narrowly required official Lucide package configuration. It must not edit `Pages/`, tests, other project files, or API code. It publishes a markup/CSS handoff contract to wave 2. | One dedicated terminal; no server and no reserved port. | First, but only after the existing dependency review is recorded and approved. Add one central Lucide version, one Client reference, and regenerate only the Client lock file; inspect the locked closure and retain the policy-required supply-chain/license evidence. Then run locked restore, `dotnet build src/HouseholdLedger.Client/HouseholdLedger.Client.csproj --no-restore`, and `dotnet test tests/HouseholdLedger.Client.ComponentTests --no-build --no-restore` after the test owner releases compatible tests. |
| 2 | Blazor Calendar Surface | `src/HouseholdLedger.Client/Pages/CalendarPage.razor`, `CalendarPage.razor.cs`, and `CalendarPage.razor.css` | Retain the calendar as the central, temporary surface and adapt only its local markup/styles to the wave-1 layout contract. It must not alter shell controls, global CSS, tests, or introduce date selection/data behavior. | One dedicated terminal; no server and no reserved port. | Depends on wave 1's released layout contract. Run the same Client build; hand off rendered selectors and semantic expectations to wave 3. |
| 3 | Test Architecture | Only new or changed feature-specific files under `tests/HouseholdLedger.Client.ComponentTests/` and `tests/HouseholdLedger.EndToEndTests/` | Add/select the narrowest component and browser checks for AC-01 through AC-05. It must not alter Client production files, hosting/project configuration, or browser dependency policy. | One dedicated terminal; unique test-owned publish root, browser profile root, output root, API port, and Client port. Ports must be distinct and confirmed free before use. | Depends on waves 1-2. Run `dotnet test tests/HouseholdLedger.Client.ComponentTests --no-build --no-restore`; then the documented fresh-publish browser workflow in `docs/development/testing.md`, with distinct free `HOUSEHOLDLEDGER_E2E_API_PORT` and `HOUSEHOLDLEDGER_E2E_CLIENT_PORT`. |
| 4 | Research and Documentation | `docs/audit/` only, in a separately assigned audit report | Audit the accepted specification against current code and retained validation evidence. It must not edit production, test, configuration, or this approved feature specification without a new documentation assignment. | One dedicated terminal; read-only evidence access; no ports. | Depends on evidence from waves 1-3. Produce the criterion-to-evidence verdict before Feature 003 can be marked complete. |

Wave 1 and wave 2 deliberately split the layout and calendar files to prevent
overlap. They are sequential, not parallel. Wave 3 begins only after both
production owners have released their files. Wave 1 exclusively owns the three
Lucide package-configuration files so no other wave may modify them. No
implementation wave may modify Feature 001, Feature 002, `BudgetExperiment/`,
artifacts, other solution/project files, or runtime configuration.

## Definition of Done

Feature 003 is done only when all of the following are true:

1. A read-only audit marks AC-01 through AC-06 `Met` with concrete current
   code and retained command/browser evidence.
2. The audit confirms that the calendar is the sole central temporary work
   surface and that both panes are independently controllable without overlaying
  it at the required desktop viewport.
3. The audit confirms the neutral no-selection inspector state and no invented
   finance, selection, persistence, API, or navigation-destination behavior.
4. Focused Client build, component tests, and the authoritative desktop
   browser proof pass under their documented, test-owned resources.
5. The audit confirms the approved Lucide Blazor/.NET package is used only as
  an icon source for Feature 003 controls; native buttons retain accessible
  names and tooltips or equivalent visible explanations; no UI library,
  visual component framework, unapproved dependency, hosting change, or
  undocumented scope expansion was introduced.
6. The audit confirms the repository's dependency-governance review was
  completed before the package-manifest changes, the version is central, the
  reference is Client-only, the Client lock file matches the resolved closure,
  and the required supply-chain/license evidence was retained under existing
  policy.
7. User-facing documentation is updated only when a later approved task shows
   that the implemented shell needs end-user guidance; this shell alone does
   not create a documentation requirement.

## Recommended Follow-On Feature Breakdown

The approved follow-on proposals are separate drafts. They do not authorize
implementation and must retain their own user approval records:

1. [Feature 004: Calendar Item Selection and Inspector Detail Contract](004-calendar-item-selection-and-inspector-detail-contract.md): Define an
   accessible calendar selection model, selection lifecycle, neutral selection
   rules, and the use-case/read-model contract that supplies inspector data.
   This feature must settle selection semantics, data shape, persistence
   boundaries, and whether selection is URL-addressable before properties are
   displayed.
2. [Feature 007: Calendar Entry Recording](007-calendar-entry-recording.md): Define the first real Kakeibo-informed record
   workflow, validation, category language, and calendar representation only
   after the selection/detail contract is approved.
3. [Feature 005: Workspace Navigation Destinations](005-workspace-navigation-destinations.md): Define concrete destinations and
   information architecture only when their user outcomes are known. It should
   not use shell labels as implied commitments to accounts, reports, settings,
   or other unapproved areas.
4. [Feature 006: Workspace Pane Preference Persistence](006-workspace-pane-preference-persistence.md): Decide whether pane state should
   persist and, if so, establish privacy, storage, accessibility, and fallback
   behavior separately from this transient shell state.

## Future Responsive Workspace Behavior Proposal

This is an unnumbered, future proposal only. It is not part of Feature 003,
does not authorize implementation, and must receive its own explicit user
approval before work begins. It may define the supported narrow viewport range,
default pane states, content ordering, no-overlay and overflow behavior,
responsive accessibility evidence, and any browser proof sizes. It must also
decide whether a demonstrated requirement justifies JavaScript viewport
interoperability; Feature 003 neither needs nor authorizes that interop.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Created Feature 003 as a draft limited to an observable workspace shell. | The requested workspace, actual navigation, calendar selection, and inspector content are separate user outcomes; lean slicing keeps the first change verifiable and honest. |
| 2026-08-04 | Choose bespoke local Blazor/CSS primitives over a UI library for this slice. | Native controls, CSS Grid, CSS custom properties, and Blazor state satisfy the shell need without a new dependency or premature generalized component layer. |
| 2026-08-04 | Require normal-flow responsive panes rather than overlay drawers. | The calendar must remain permanently visible, reachable, and central while panes are expanded or collapsed. |
| 2026-08-04 | Defer selection-driven inspector content. | There is no approved calendar interaction model, data shape, persistence contract, or use case from which truthful properties could be rendered. |
| 2026-08-04 | User approved Feature 003 for implementation planning, with desktop-expanded and narrow-collapsed pane defaults. | This is retained as historical context only. The narrow default was expressly deferred by the approved 2026-08-06 scope refinement. |
| 2026-08-04 | At narrow widths, order expanded regions as navigation, calendar, then inspector in normal flow. | This is retained as historical context only. The ordering decision was expressly deferred by the approved 2026-08-06 scope refinement. |
| 2026-08-04 | Display a `Navigation` heading with no destination items, and rename the calendar placeholder heading to `Calendar`. | Neutral labels make the shell intelligible without inventing destinations or calendar behavior. |
| 2026-08-04 | Authorize the official Lucide Blazor/.NET integration strictly as an icon source for familiar Feature 003 controls. | The user explicitly accepted Lucide and its license. Native buttons, equivalent accessible labels, and the bespoke local Blazor/CSS design remain required; this does not authorize a UI component library or visual framework. |
| 2026-08-04 | Require renewed Wave 1 approval after the Lucide amendment. | Superseded by the 2026-08-06 user-approved scope refinement, which confirms that the existing implementation authorization continues for the reduced desktop-only scope. |
| 2026-08-06 | Approve Feature 003 as desktop-only while deferring narrow viewport workspace behavior. | The user expressly deferred narrow viewport acceptance, 500 x 844 proof, narrow default states, normal-flow ordering, and JS viewport interop. The feature remains approved and pending remaining implementation, validation, audit, and completion work. |

## Open Questions Requiring Product Decisions

1. For the later inspector-content feature, what exact calendar object can be
   selected; what read-model fields are safe and useful; where is the selection
   held; whether it is persisted or URL-addressable; and which Application/API
   use case supplies it all remain unresolved.

## Assumptions and Research Limits

- The current root Client route is the existing calendar route and is the only
  route in scope. This is based on read-only inspection of `CalendarPage.razor`.
- `MainLayout` and `CalendarPage` currently form the controlling visible Client
  path, although nearby stylesheet content appears more developed than the
  minimal rendered markup and must be reconciled by the assigned implementation
  owners after approval.
- The existing client component and browser test projects are available to Test
  Architecture, as documented in `docs/development/testing.md`; their exact
  current test names and selectors were not inspected during this lightweight
  documentation research.
- No direct BudgetExperiment inspection was needed: the successor's existing
  Kakeibo/calendar direction and current Client path supplied the relevant
  product evidence.
