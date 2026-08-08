# Feature 004 Calendar Day Selection Final Re-Audit

## Scope and Verdict

**Audit date:** 2026-08-06

**Source of truth:** [Feature 004: Calendar Day Selection and Deferred
Inspector Detail Contract](../features/004-calendar-item-selection-and-inspector-detail-contract.md)

**Verdict:** Audit passed. Feature 004 is complete. AC-01 through AC-06 and
all five numbered Definition of Done items are `Met`.

The direct user request for this final audit explicitly requires Feature 004 to
be completely finished. That is the required explicit approval, notwithstanding
the stale `Draft - awaiting explicit user approval` status retained in the
Feature 004 document. This report does not change the specification.

This is a read-only review of current production and test source, the prior
Feature 004 audit, the applicable Feature 003 boundary and audit, the scoped
worktree diff, and retained validation results supplied with this assignment.
No build, test, browser, server, port, runtime, restore, or product command was
run for this re-audit.

## Evidence Basis

### Current Production Source

- [CalendarPage.razor](../../src/HouseholdLedger.Client/Pages/CalendarPage.razor)
  renders a single `Calendar` main surface, a native named radio group for the
  three presentation modes, a polite period-status element, native date
  buttons, and native previous/next controls. Today has no table or custom key
  handler; weekly and monthly views have named tables with seven headers.
- [CalendarPage.razor.cs](../../src/HouseholdLedger.Client/Pages/CalendarPage.razor.cs)
  injects `TimeProvider`, keeps only transient active-date and mode state,
  derives week boundaries and header order from the current culture, and derives
  each displayed period from the active date. It couples each grid focus move to
  selection and uses the post-render element reference for the selected date,
  active mode, or previous-period control as required by the endpoint behavior.
- [CalendarPage.razor.css](../../src/HouseholdLedger.Client/Pages/CalendarPage.razor.css)
  supplies a visible focus outline and selected-date border/inset indicator, so
  selected and focused controls are not communicated by color alone.
- [MainLayout.razor](../../src/HouseholdLedger.Client/Layout/MainLayout.razor)
  remains Feature 003's unchanged shell owner. It keeps the calendar in the
  central workspace and the complementary inspector at `No calendar item
  selected`.

### Current Test Source and Retained Results

[CalendarPageTests.cs](../../tests/HouseholdLedger.Client.ComponentTests/CalendarPageTests.cs)
now contains 12 deterministic bUnit tests. They use fixed client-local clocks;
read actual rendered attributes and labels; and inspect focus interop requests
after the relevant render. The strengthened tests assert, rather than infer from
their names:

- initial controlled-clock Today state, one checked radio, selected/current date
  state, and its sole date tab stop;
- mode exclusivity and preservation of the active date in containing week and
  month structures;
- pointer, Enter, Space, and all four arrow paths with one selected and roving
  date tab stop;
- chronological Tab and Shift+Tab movement, including a month period change;
- first-week-date Shift+Tab and last-week/month-date Tab exits with no date or
  period mutation, plus a post-render focus request;
- Today with no keydown event handler, native click activation, no grid, no
  focus interop request, and one-day Next navigation;
- restored `de-DE` culture scope with Monday-first German headers and
  left/right day semantics;
- week and month boundary movement with the containing table, heading/status,
  selected date, and post-render focus request; including December/January and
  February 29 to March 1; and
- mode-specific previous/next movement through both December/January directions
  and January 31 to February 29.

The supplied retained validation outcomes are used as evidence and are not
represented as commands run by this audit:

| Retained check | Supplied result | Audit use |
| --- | --- | --- |
| Specific narrow component command | Passed 1/1. | Confirms the focused selection was runnable. |
| Focused `CalendarPageTests` | Passed 12/12 in 1.1 seconds. | Primary deterministic interaction, semantics, boundary, culture, and focus evidence. |
| Client component project build without restore | Passed in 1.6 seconds with 0 warnings and 0 errors. | Confirms the current focused component project compiled. |
| Fresh isolated desktop browser journey | `BrowserCalendarJourneyTests` passed 1/1 in 5.2 seconds. Fresh API and Client publishes passed; API port 51381 and unused one-host Client port 51382 were reserved free and cleanup was verified. | Authoritative 1440 by 900 visible behavior, central layout, neutral inspector, and hosted interaction proof. |
| Test validation diagnostics and scoped diff check | Clean. | Supports the supplied focused test repair's source and diff hygiene. |

[BrowserCalendarJourneyTests.cs](../../tests/HouseholdLedger.EndToEndTests/BrowserCalendarJourneyTests.cs)
uses the existing one-host API URL at a 1440 by 900 viewport. Its actual
assertions exercise all three modes, grid structure and one selected tab stop,
direct month-date activation and focus equality, right-arrow selection/focus
equality, next-period heading/status change, normal-flow calendar/inspector
geometry, no page-level horizontal overflow, and the neutral inspector. It is
not treated as a pixel or narrow-viewport test; neither is required by Feature
004.

## Prior-Gap Reconciliation

The prior audit left AC-04 and AC-05 `Not Verifiable` for specific missing
evidence. Each is now resolved by current source and semantic assertions:

| Prior gap | Re-audit finding |
| --- | --- |
| First-date Shift+Tab and last-date Tab exits | `HandleGridKeyDown` sets `ActiveMode` or `PreviousPeriod` focus only at the displayed endpoints. The component tests invoke those exact endpoint key paths, assert unchanged active date and table label, and observe a new post-render focus request. The targets are further verified by the production `OnAfterRenderAsync` switch, rather than assumed from a test name. |
| Today sequential and native behavior | Today markup has only `@onclick`, no grid keydown handler, and a native button. The test expects the framework's missing-key-handler exception for Tab, clicks the date, asserts unchanged selection and no focus interop, then verifies native previous/next day navigation. |
| Culture-derived rendered order and arrows | The `de-DE` test temporarily applies and restores the culture, reads `Mo` through `So` headers, confirms the Monday first date, and exercises both Left and Right. Production derives both headers and week start from `DateTimeFormat.FirstDayOfWeek`. |
| Boundary displayed period, status, selection, and focus | The weekly ArrowLeft test crosses the rendered weekly boundary and asserts the target label, updated containing-week table/status, and a new focus request. Month tests directly cover December-to-January, January-to-December, and February 29-to-March 1 with updated headings/status and focus requests. |
| Focus after pointer, Enter, Space, arrows, and previous/next | Component assertions count post-render focus interop after pointer, Enter, Space, directional movement, endpoint handoffs, boundary moves, and navigation. The browser assertion independently checks actual DOM focus equals the selected date after direct activation and Right Arrow. Production only schedules grid focus for non-Today selection, matching the Today contract. |

No reviewed assertion conflicts with the specification or masks a code mismatch.
In particular, endpoint Tab tests deliberately assert that the date and period
do not change, while the implementation redirects focus out of the managed grid;
this is the specified behavior, not a missing traversal case. The focus-request
assertions establish that a post-render request occurred; source inspection
establishes the concrete referenced target for each request.

## Acceptance Criteria Matrix

| Criterion | Verdict | Concrete evidence and limits |
| --- | --- | --- |
| AC-01: Initial calendar state | Met | Injected `TimeProvider` initializes active date once. The fixed-date test asserts Today as the sole checked mode and selected/current/tab-stop date. Markup has no financial, transaction, currency, time-value, or inspector-detail content. The retained desktop browser proof confirms the visible root state and neutral inspector. |
| AC-02: Exclusive presentation modes | Met | Native same-name radios form the exclusive control set. `SetMode` changes mode only, preserving active date; week/month structures derive from it. Component assertions read one checked radio and containing dates; browser proof selects all three modes. Today is a presentation label, with no reset command. |
| AC-03: Presentation semantics | Met | Today is an accessible selected-date section and not a multi-day table. Week is a seven-date culture-derived table. Month is a seven-column table with every in-month native button and `aria-hidden` noninteractive blank cells. Source keeps current and selected states independent; component and browser assertions inspect those structures. |
| AC-04: Date selection and grid keyboard behavior | Met | Weekly/monthly buttons route pointer, Enter, Space, Tab, Shift+Tab, and four arrows through `SelectDate`, preserving one selected/roving tab stop and scheduling focus. Current component tests semantically cover each required input class, both grid endpoint exits, Today no-custom-key native behavior, culture-sensitive horizontal movement, and focus requests. Browser proof independently observes actual direct-activation and arrow focus equality. No route, URL, inspector, or data state is changed in the reviewed code. |
| AC-05: Period navigation and boundary behavior | Met | `MoveActiveDate` applies mode-specific day, seven-day, or month deltas; selection-derived rendering always contains the target date. The deterministic tests cover weekly boundary movement, 31st-to-1st month movement, both December/January directions, January 31 to February 29, February 29 to March 1, updated heading/status, and focus requests. The retained browser journey verifies hosted next-period change. |
| AC-06: Accessible, bounded Client-only slice | Met | Source uses native radios/buttons, labels, tables, `aria-pressed`, conditional `aria-current`, a sole grid tab stop, polite status, and visible CSS focus/selection treatment. The retained browser proof verifies the 1440 by 900 central calendar, neutral inspector, normal-flow geometry, and no horizontal overflow. Scoped diff review finds Feature 004 changes only in the three CalendarPage files and focused Calendar test sources; no inspector, financial data, API, Domain, Application, Infrastructure, persistence, settings, dependency, or configuration change is introduced. |

## Definition of Done Matrix

| Definition of Done item | Verdict | Concrete evidence and limits |
| --- | --- | --- |
| 1. User explicitly approves the amended specification before implementation. | Met | The user explicitly directed final completion in this assignment. The source document's retained draft status is stale documentation, not a countermand of direct user approval. |
| 2. A read-only audit marks AC-01 through AC-06 `Met` using current code and retained focused component and authoritative browser evidence. | Met | This read-only report marks every acceptance criterion `Met` from current source, supplied 12/12 component results, and supplied fresh 1/1 desktop browser result. |
| 3. Audit verifies deterministic coverage for modes, controlled clock, Today, culture, Tabs, arrows, navigation, boundaries, focus, semantics, and period updates. | Met | The twelve tests directly cover every listed category, including restored `de-DE`, both grid endpoints, all arrows across the suite, December/January in both directions, January 31 to February 29, and February 29 to March 1. The report distinguishes source-established focus targets from test-established post-render focus requests. |
| 4. Audit confirms the Client-only boundary: no inspector, data, settings, API, persistence, dependency, or Feature 005-007 overlap. | Met | Diff/source inspection limits Feature 004 production work to CalendarPage and test work to the focused Client/browser tests. No manifests, lock files, configuration, non-Client layer, inspector, or Features 005-007 artifact changed for this feature. |
| 5. Audit confirms accessible central desktop calendar work area and controlled clock behavior. | Met | `TimeProvider` plus fixed-clock tests provide controlled time. Native semantics, visible focus, selected state, status, and central shell layout are source-supported; the supplied 1440 by 900 browser proof verifies visible desktop geometry and no page-level horizontal overflow. |

## Feature 003 Dependency Boundary

Feature 003 remains the owner of the desktop shell and its final audit still
records an unresolved Lucide dependency-governance requirement. That finding is
not a Feature 004 scope change: Feature 004 adds no dependency, package
reference, lock-file change, configuration change, or Lucide use. It therefore
does not prevent Feature 004 from satisfying its own Client-only Definition of
Done, but it remains a separate blocker to declaring Feature 003 complete.

## Residual Limits

- This audit did not rerun commands. Build, component, browser, publish,
  resource-isolation, and cleanup outcomes are retained results supplied with
  the assignment.
- This feature is deliberately desktop-only. It makes no narrow viewport,
  mobile, pixel-comparison, screen-reader-session, transaction, inspector
  detail, settings, persistence, or financial-data claim.
- The written Feature 004 status remains stale. Reconciling it requires a
  separately assigned specification-document edit and is not necessary to the
  completion verdict based on direct user approval.

## Documentation Validation Record

- This report is the only file created or modified by this audit assignment.
- Markdown diagnostics and a CRLF-aware scoped `git diff --check` for this
  report are recorded after creation.
- No old audit, specification, status document, production source, test source,
  configuration, or `BudgetExperiment` file was modified.
