# Feature 004: Calendar Day Selection and Deferred Inspector Contract

## Status

Status: Draft - awaiting explicit user approval; implementation is not
authorized.

- This is a proposal only. It cannot start implementation until the user
  explicitly approves this amended document.
- Proposal date: 2026-08-04. Amendment date: 2026-08-06.
- Depends on the approved, pending implementation workspace shell in
  [Feature 003](003-workspace-navigation-and-ui-foundation.md). It does not
  treat Feature 003 as implemented or validated.

## Context and Outcome

HouseholdLedger is a calendar-centered household ledger informed by Kakeibo, a
household-accounting practice of recording, planning, and reflection. Feature
003 establishes the desktop shell, with a central calendar work area and an
unchanged, truthful inspector state. This feature defines an accessible
calendar work surface without introducing ledger data.

**Proposed outcome:** At the root calendar route, a desktop user has one active
selected date and can switch the central calendar between three UI-only
temporal presentations: Today, This Week, and This Month. The selected date is
initially the injected Client-local current date. A mode shows the period that
contains that active date, and previous/next navigation moves the active date
by that mode's period. The calendar remains transient Client presentation
state. No transactions, balances, totals, time values, currency, or inspector
detail appear.

## Goals

1. Provide exactly one active presentation mode: Today, This Week, or This
  Month.
2. Present the active date as a one-day view, its culture-derived week, or its
  displayed month while preserving Feature 003's central desktop calendar
  role.
3. Provide pointer and keyboard date selection in weekly and monthly grids,
  with predictable Tab and arrow-key behavior.
4. Keep the active date visible by changing the displayed period whenever a
  date-selection or date-movement path targets a date outside it.
5. Keep the active selected date, presentation mode, and displayed period as
  transient Client state; preserve Feature 003's unchanged neutral inspector.

## Non-Goals and Explicitly Out of Scope

- Inspector markup, content, commands, navigation, loading/error states, or
  changes to Feature 003's `No calendar item selected` state.
- Transactions, transaction summaries, balances, totals, financial
  calculations, accounts, categories, entry recording, editing, imports, or
  fake data.
- API, Application, Domain, Infrastructure, contracts, OpenAPI, persistence,
  migrations, authorization, API-client changes, or database work.
- Application settings UI or model; persistence or retrieval of settings; and
  configured timezone, currency, first day of week, or 12/24-hour format.
- Persistence of selection or mode; URL addressing, browser-history behavior,
  sharing, restoration, or a command that resets the active date to the
  current date.
- Responsive or narrow viewport behavior, a separate calendar redesign, new
  dependencies, UI libraries, component frameworks, icon sources, or
  configuration changes.

## Calendar Interaction and Accessibility Contract

The calendar has one active selected date and one active presentation mode.
Mode selection controls the period presented around the active date; it does
not change the active date. The visible `Today` label means the one-day
presentation, not a rolling-current-date view or a command to return to today.
The injected current date supplies the initial active date only.

- A native accessible segmented group, or equivalent exclusive native controls
  such as three radio buttons, exposes `Today`, `This Week`, and `This Month`.
  It is keyboard operable and programmatically exposes exactly one active mode.
  It is not an ambiguous collection of independently toggleable buttons.
- **Today** presents only the active selected date. It exposes that date with a
  complete accessible date name and programmatic selected state, but does not
  claim to contain a multi-day calendar grid. Its date control is reachable by
  Tab and supports native activation. Tab otherwise follows normal sequential
  navigation; arrow keys do not implement undisclosed multi-day movement in
  this single-day presentation.
- **This Week** presents the seven dates in the week containing the active
  date. The provisional first weekday and weekday labels derive from the
  Client's current culture. It uses a named calendar grid or table with weekday
  headers and supports pointer and keyboard day selection.
- **This Month** presents the month containing the active date, retaining the
  existing visible-month and calendar-grid semantics: a named seven-column
  calendar table with weekday headers, week rows, and every in-month date.
  Leading and trailing blank cells are not interactive. Direct pointer or
  native keyboard activation of an in-month day remains supported.
- Native accessible previous and next controls move the active date by one day
  in Today, one culture-derived week in This Week, or one month in This Month.
  They automatically update the displayed period around the resulting active
  date. They do not create data, alter the inspector, or reset the date to the
  current date.
- Changing mode preserves the active date and immediately displays the
  containing day, week, or month. The UI need not prescribe a visual mechanism
  beyond the exclusive accessible controls and a clearly observable selected
  presentation.

Weekly and monthly presentations use native buttons for selectable days. The
active date is the selected date and the focused grid date. It is visibly
distinguishable without color alone, exposes `aria-pressed="true"`, and is the
only date button with `tabindex="0"`; other date buttons expose
`aria-pressed="false"` and `tabindex="-1"`. A visible current date exposes
`aria-current="date"`; current and selected remain independent states.

- The initial active date and initial containing presentation are supplied by
  a framework-provided or injected Client-local clock. Tests use a controlled
  clock. This is presentation state, not a ledger or transaction time.
- Until a separately approved settings feature exists, the Client's current
  culture and current-time behavior are provisional presentation defaults.
  Week boundaries and weekday labels derive from that culture. No currency or
  time formatting is rendered.
- In a weekly or monthly grid, pointer activation, `Enter`, and `Space` select
  the target date and retain focus on it. Keyboard movement also selects its
  destination. There is never a focus-only date distinct from the active
  selected date.
- Each weekly or monthly grid has one managed date tab stop. On entry from
  outside the grid, Tab lands on the active date. While focus is in the grid,
  `Tab` advances to the next chronological displayed date and `Shift+Tab` to
  the previous one, moving focus, selection, and the sole `tabindex="0"`
  together. At the first date, `Shift+Tab` leaves to the preceding control; at
  the last date, `Tab` leaves to the following control.
- In weekly and monthly grids, horizontal arrows move $+1$ or $-1$ calendar
  day in the rendered weekday order. `ArrowDown` moves $+7$ days and `ArrowUp`
  moves $-7$ days. The order follows the culture-derived weekday headers and is
  not hard-coded to a Sunday-first layout. Today has no multi-day grid, so
  these grid arrow behaviors do not apply there; its previous/next controls
  provide the accessible one-day movement.
- For every weekly or monthly pointer, Tab, arrow, native activation, or
  programmatic focus or selection path, if the target date is outside the
  displayed period, the UI changes the display to the period containing that
  target before retaining focus on its active button. This includes week and
  month boundaries, December-to-January, January-to-December, and leap-day
  transitions. Previous/next navigation follows the same active-date and
  displayed-period rule in every mode.
- A concise status element or the period heading announces automatic displayed
  period changes without stealing focus. Hover, color, or position alone never
  conveys state or an available action.

## Kakeibo and Calendar UX Rules

- The visible day, week, and month presentations support calm review and
  orientation. They do not imply that transactions exist or that a user must
  record, classify, or act on a date.
- A selected date is UI context only. It does not select a calendar item or
  ledger data, calculate a balance, or enable an inspector action. Feature
  003's inspector remains unchanged and neutral.
- At the Feature 003 desktop viewport, controls, labels, focus, and selection
  states remain reachable without page-level horizontal scrolling, overlap, or
  an overlay pane.

## Deferred Follow-On: Persisted Application Settings

This unnumbered proposal is deliberately outside Feature 004 and allocates no
feature number or implementation authority. A later separately approved
feature owns the configuration UI, application-settings model, database
persistence and retrieval, and application of timezone, currency, first day of
week, and 12/24-hour format. It may change calendar mode formatting or week
boundaries only after deciding defaults, validation, migration, privacy, and
how setting changes affect calendar and financial presentation. Feature 004
neither creates nor anticipates its data model, API, persistence, or retrieval
path.

## Future Inspector Follow-On Contract

This section records later product intent only. No inspector code, data, or
behavior is implemented or altered by Feature 004. After a separately approved
data/detail feature supplies truthful business-owned data, the inspector may
receive a selected day's transaction summary, opening balance, closing balance,
and allowed links or commands to transaction detail and approved add/edit
workflows.

The API and Application layers will own the business-facing day-detail data and
capabilities. The Domain will own domain rules introduced for them. The Client
will render received data and capabilities, not calculate balances, totals, or
transaction lists. A later approved feature must freeze the smallest truthful
cross-layer contract before changing the inspector.

## Acceptance Criteria

| Criterion | Observable outcome | Completion evidence |
| --- | --- | --- |
| AC-01: Initial calendar state | The root route initially presents the injected Client-local current date as selected and exactly one active presentation mode. No financial, transaction, currency, time, or inspector-detail content appears. | Focused Client component checks with a fixed clock; desktop browser proof at Feature 003's `1440 x 900` viewport. |
| AC-02: Exclusive presentation modes | A keyboard or pointer user can choose Today, This Week, or This Month through accessible exclusive controls. Exactly one mode is active; changing it preserves the active date and displays its containing day, culture-derived week, or month. `Today` remains a selected-date presentation, not a reset-to-current-date command. | Focused Client interaction and semantic assertions for exclusivity, mode changes, and preserved date; browser proof of each presentation. |
| AC-03: Presentation semantics | Today exposes the active date and selected state without claiming a multi-day grid. This Week exposes its containing seven-day culture-derived week with weekday headers. This Month exposes the active date's month with existing seven-column grid semantics, all in-month dates, and noninteractive blank leading/trailing cells. | Focused Client structural and accessibility assertions with a controlled clock; browser-visible proof for all three presentations. |
| AC-04: Date selection and grid keyboard behavior | Weekly and monthly grids support pointer, `Enter`, `Space`, Tab, Shift+Tab, and all four arrows according to the documented selection, focus, rendered-order, and managed-tab-stop rules. Today supports its reachable selected date and native activation without claiming grid arrow behavior. The inspector, route, URL, and data state remain unchanged. | Focused Client interaction and semantic tests for weekly and monthly keyboard movement, Today sequential focus behavior, focus retention, and active-state updates. |
| AC-05: Period navigation and boundary behavior | Previous/next controls move the active date by one day, week, or month for the active mode and update the containing displayed period. Any weekly or monthly grid target outside its displayed period updates that period before retaining focus. Coverage includes week/month boundaries, 31st-to-1st, both December/January directions, and leap-year February transitions. | Deterministic focused Client tests using fixed dates and controlled clocks; browser proof for cross-period navigation. |
| AC-06: Accessible, bounded Client-only slice | The exclusive mode controls, date controls, names, selected/current state, managed grid tab sequence, announcements, and visible focus follow this document. The calendar remains central at the desktop viewport. State is transient Client presentation state; no inspector, data, settings, API, Domain, Application, Infrastructure, persistence, dependency, or configuration work is introduced. | Scoped diff review, focused Client checks, markup/accessibility review, and browser-visible inspection. |

## Authoritative Validation Boundaries

The published hosted Client at the existing one-API HTTPS root URL, observed at
Feature 003's `1440 x 900` desktop viewport, is authoritative visible proof.
It verifies each presentation, exclusive mode selection, previous/next
navigation, a cross-period transition, central calendar layout, neutral
inspector, and absence of financial content.

Focused Client component tests are primary proof for deterministic date
calculation, controlled-clock initialization, mode exclusivity, culture-derived
week containment, Today semantics, managed Tab behavior, directional arrows,
period boundaries, active state, focus, and accessibility semantics. Test
Architecture chooses exact tests and must cover leap-year and
December/January transitions. Browser evidence supplements rather than replaces
these checks.

No build, test, runtime, Markdown formatter, linter, link checker, spelling, or
grammar command is available in the current documentation assignment. A future
approved implementation uses the narrow Client validation selected by Test
Architecture; API, database, OpenAPI, persistence, full-solution, and
browser-matrix validation remain outside this slice.

## Dependency-Ordered Implementation Waves

Implementation is not authorized. After explicit user approval, the
orchestrator assigns exclusive ownership sequentially:

| Order | Specialist | Exclusive writable ownership | Responsibility and boundary | Required checks |
| --- | --- | --- | --- | --- |
| 1 | Blazor Calendar UI | `src/HouseholdLedger.Client/Pages/CalendarPage.razor`, `CalendarPage.razor.cs`, and `CalendarPage.razor.css` | Implement only local transient active-date, mode, and displayed-period state; exclusive native mode controls; day/week/month presentation; date movement; and this accessibility contract. Do not edit layout, inspector, non-Client layers, project/configuration files, or tests. Publish the mode, keyboard, and semantic handoff required by wave 2. | Focused Client build after wave 2 releases compatible tests. |
| 2 | Test Architecture | Only feature-specific files under `tests/HouseholdLedger.Client.ComponentTests/` and `tests/HouseholdLedger.EndToEndTests/` | Add deterministic component tests for AC-01 through AC-06, including exclusive modes; initial controlled-clock state; weekly and monthly Tab/Shift+Tab and arrow behavior; Today semantics; active state/focus; mode-period navigation; week/month boundaries; both December/January directions; and leap-year boundaries. Add the narrow desktop browser evidence. Do not edit production, configuration, or inspector files. | Focused component tests and documented hosted browser proof. |

There is no API, Application, Domain, Infrastructure, inspector, or settings
wave. No wave may retrofit the future follow-on proposals into Feature 004.

## Definition of Done

Feature 004 is done only when all of the following are true:

1. The user explicitly approves this amended specification before implementation.
2. A read-only audit marks AC-01 through AC-06 `Met` using current code and
   retained focused component and authoritative browser evidence.
3. The audit verifies deterministic coverage for exclusive mode selection,
   controlled-clock initialization, Today semantics, culture-derived weekly
   containment, chronological Tab traversal, rendered-order arrows,
   mode-specific previous/next movement, 31st-to-1st, December/January,
   leap-year boundaries, focus retention, selection semantics, and displayed
   period updates.
4. The audit confirms the Client-only boundary: no inspector change, financial
   data or calculation, settings/persistence work, API/data work, dependency
   change, or overlap with Features 005 through 007.
5. The audit confirms the calendar remains the accessible central desktop work
   area and clock-dependent behavior uses controlled time.

## Open Questions Requiring Product Decisions

1. Which separately approved feature will define the persisted application
  settings experience and its defaults, validation, migration, privacy, and
  the effect of configured week boundaries or formatting on these modes?
2. Which later feature will freeze the inspector's truthful day-detail read
   model and allowed transaction capabilities before inspector implementation?
3. Should a later user-approved feature add a distinct command that returns the
  active date to the current date? This feature deliberately does not infer
  one from the `Today` label.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Created as a separate proposal from Feature 003. | Calendar interaction and truthful detail data are distinct from the desktop shell. |
| 2026-08-06 | Keep Feature 004 a draft; no implementation is authorized. | Only explicit user approval can authorize implementation. |
| 2026-08-06 | Define one Client-only calendar work surface with exclusive Today, This Week, and This Month presentations. | The user requires toggleable temporal presentations while calendar selection remains a narrow UI-only concern. |
| 2026-08-06 | Make mode selection preserve the active date; use `Today` for the one-day active-date presentation rather than a rolling-current-date reset. | Selection and navigation already establish an active date; preserving it avoids surprising loss of context. |
| 2026-08-06 | Move the active date by day, culture-derived week, or month through previous/next controls, and update the containing presentation automatically. | Mode-specific movement provides a coherent, testable navigation contract across period boundaries. |
| 2026-08-06 | Retain managed Tab and arrow movement in weekly/monthly grids, but give Today a reachable selected date without pretending it is a multi-day grid. | The requested keyboard contract applies where multiple calendar dates are presented; truthful semantics prevent fictitious grid behavior. |
| 2026-08-06 | Use Client-local current culture/time and culture-derived week start as provisional presentation defaults. | Persisted application settings remain a separate product responsibility; the injected clock keeps initial-date behavior deterministic. |
| 2026-08-06 | Keep inspector data, settings, persistence, API/data work, and financial semantics deferred. | No truthful business data or approved cross-layer contract exists for them. |

## Dependencies

- [Feature 003](003-workspace-navigation-and-ui-foundation.md) provides the
  desktop workspace shell and owns the inspector structure and neutral state.
- [Feature 005](005-workspace-navigation-destinations.md) remains responsible
  for workspace destinations and routes.
- [Feature 006](006-workspace-pane-preference-persistence.md) remains
  responsible for any pane-preference persistence decision.
- [Feature 007](007-calendar-entry-recording.md) still requires a separately
  approved truthful transaction/data and use-case contract before recording can
  be designed or implemented.
