# Feature 004 Calendar Day Selection Audit

## Scope and Verdict

**Audit date:** 2026-08-06

**Source of truth:** [Feature 004: Calendar Day Selection and Deferred Inspector Detail Contract](../features/004-calendar-item-selection-and-inspector-detail-contract.md)

**Verdict:** Not audit-passed. AC-01, AC-02, AC-03, and AC-06 are `Met`.
AC-04 and AC-05 are `Not Verifiable`. Definition of Done items 1, 4, and 5
are `Met`; items 2 and 3 are `Not Met`.

The user approval relayed with this assignment satisfies the approval condition.
The current Feature 004 document still says `Draft - awaiting explicit user
approval` and contains a dated decision that implementation is unauthorized.
That stale document status is a documentation inconsistency, not a reason to
disregard the direct user approval supplied for this audit. This report does not
modify the specification.

This was a read-only audit of current source, current test source, the current
Git worktree, Feature 003's boundary and audit, package manifests and the Client
lock file, and retained Phase 1 and Phase 2 validation evidence. No build, test,
restore, browser, server, port, or runtime process was started.

## Evidence Basis

### Current Implementation

- [CalendarPage.razor](../../src/HouseholdLedger.Client/Pages/CalendarPage.razor)
  renders one root-route `main` headed `Calendar`, native radio controls for
  the three presentation modes, a polite period-status element, the three
  documented presentations, native previous/next buttons, and no financial or
  inspector markup.
- [CalendarPage.razor.cs](../../src/HouseholdLedger.Client/Pages/CalendarPage.razor.cs)
  injects `TimeProvider`, initializes one transient `ActiveDate`, derives week
  boundaries and weekday order from `CultureInfo.CurrentCulture`, and derives
  the visible day/week/month from that active date. It also implements the
  documented date deltas, roving date tab stop, and post-render focus target.
- [CalendarPage.razor.css](../../src/HouseholdLedger.Client/Pages/CalendarPage.razor.css)
  provides visible focus treatment, a selected-date border and inset indicator,
  and no new responsive claim. The required scope is desktop-only.
- [MainLayout.razor](../../src/HouseholdLedger.Client/Layout/MainLayout.razor)
  remains the owning shell: the calendar is in the central `calendar-workspace`
  section and the inspector still contains only `No calendar item selected`.
  The Feature 004 production diff contains no layout or inspector file.

### Current Tests and Retained Results

- [CalendarPageTests.cs](../../tests/HouseholdLedger.Client.ComponentTests/CalendarPageTests.cs)
  has six deterministic bUnit tests using a fixed `TimeProvider`. They inspect
  initial state; mode exclusivity and structure; month pointer/Enter/Space and
  arrows; partial month and week Tab/arrow movement; and mode-specific
  previous/next navigation across December/January and January 31 to February
  29.
- [BrowserCalendarJourneyTests.cs](../../tests/HouseholdLedger.EndToEndTests/BrowserCalendarJourneyTests.cs)
  extends the existing 1440 by 900 hosted browser journey. Its current source
  inspects all three modes, month direct activation, one right-arrow focus and
  selection update, one next-period update, central geometry, page scroll width,
  and the neutral inspector.
- Retained Phase 1 evidence reports
  `dotnet build src/HouseholdLedger.Client/HouseholdLedger.Client.csproj --no-restore`
  passed with zero warnings and errors. Retained Phase 2 evidence reports the
  six component tests passed and the focused fresh-publish browser journey
  passed 1/1 at 1440 by 900. Those commands were not rerun. Current source,
  especially the uncommitted code-behind, takes precedence over the older build
  claim where they differ.

### Current Worktree and Dependency Review

`git status --short` and `git diff --name-status` show exactly six modified,
uncommitted paths: the Feature 004 specification, the three CalendarPage files,
and the two calendar test files. Git does not identify the author of any
uncommitted hunk. The audit therefore does not label a change as user-authored
or formatter-authored. The diff contains substantive calendar and test behavior;
the large browser-test diff also includes broad existing-test reformatting or
rewriting, which cannot be reliably separated by ownership from Git metadata.
No change was reverted.

The Feature 004 diff has no package manifest, lock-file, configuration, API,
Application, Domain, Infrastructure, persistence, or inspector change. Current
[Directory.Packages.props](../../Directory.Packages.props),
[HouseholdLedger.Client.csproj](../../src/HouseholdLedger.Client/HouseholdLedger.Client.csproj),
and the Client lock file still contain the pre-existing Feature 003 Lucide
closure. Feature 003's final audit records that its required Lucide
dependency-governance review remains incomplete. That is a Feature 003 blocker;
Feature 004 added no dependency and must not claim Feature 003 is complete.

## Acceptance Criteria Matrix

| Criterion | Verdict | Evidence and limits |
| --- | --- | --- |
| AC-01: Initial calendar state | Met | The injected clock supplies the initial active date; Today is the initial checked native radio and its one date button is selected, current, and the sole date tab stop. The fixed-clock component test and retained browser result support this. Current markup contains no transaction, balance, currency, time-value, or inspector-detail content. |
| AC-02: Exclusive presentation modes | Met | One named native radio group exposes Today, This Week, and This Month. `SetMode` changes only the mode, preserving `ActiveDate`; all visible periods derive from that date. The component test asserts one checked radio and the containing week/month, while the current browser source selects all three presentations. Today has no reset-to-current-date command. |
| AC-03: Presentation semantics | Met | Today is a labeled selected-date section, not a table. Week is a named table with seven culture-derived headers and seven date buttons. Month is a named seven-column table with in-month buttons and `aria-hidden` noninteractive blank cells. Current date and selected state are independent in source. Fixed-clock component assertions and retained browser evidence cover the structural modes. |
| AC-04: Date selection and grid keyboard behavior | Not Verifiable | Current handlers implement pointer, Enter, Space, four arrows, chronological Tab and Shift+Tab, one selected/roving tab stop, and pending post-render focus. The code sets focus to the active date after a grid selection or movement; at the displayed endpoints it directs focus to the active mode or previous-period control. However, current tests do not exercise first-date Shift+Tab exit, last-date Tab exit, entry from outside a grid, Today Tab/native activation, or focus retention for week, Enter, Space, and boundary traversal. Browser source verifies focus only after a month pointer activation and one right arrow. The required end-to-end behavior cannot be confirmed from the retained partial evidence without executing or adding the missing focused checks. |
| AC-05: Period navigation and boundary behavior | Not Verifiable | `MoveActiveDate` uses day, seven-day, and month deltas by mode, and every grid movement derives a new period directly from its selected active date. Source therefore supports January 31 to February 29, December/January, and grid boundary rendering. Existing component tests cover previous/next in all modes, both December/January directions through mode navigation, January 31 to February 29, a month right-arrow January 31 to February 1, and a week arrow across a week. They do not prove focus retention and displayed-period status for every documented cross-period input path, nor a leap-day grid path, culture-variant rendered-order arrows, or keyboard boundary exits. The retained browser result proves only a runtime heading change after one next-period action. |
| AC-06: Accessible, bounded Client-only slice | Met | Current markup uses native radios and buttons with names; tables have labels and weekday headers; selected buttons expose `aria-pressed`, `tabindex`, and conditional `aria-current`; a polite status announces period changes; CSS supplies focus and selected indicators. The central desktop relationship and neutral inspector are retained from the shell and checked by the current browser source. Scoped diff inspection finds Feature 004 changes only in the allowed Client calendar and test surfaces plus its specification. No Feature 004 dependency or configuration change exists. The prior Feature 003 Lucide governance blocker is retained separately and is not evidence that Feature 004 introduced a dependency. |

## Keyboard and Accessibility Review

The current implementation is aligned with the specified movement calculations:
left/right use minus/plus one date; up/down use minus/plus seven dates; the
week begins at the current culture's `FirstDayOfWeek`; and period rendering
always contains `ActiveDate`. This gives direct source evidence for crossing
week/month boundaries, both directions across December/January, and leap-date
calculation. There is no separate focus-only date state.

The audit cannot elevate those source paths to complete behavioral evidence.
The test suite does not set a non-default culture to demonstrate that rendered
weekday order, arrow meaning, and week containment agree under a culture with a
different first day. It also does not assert the boundary Tab hand-offs or that
the live status update leaves focus on the target. Those are material gaps
because the specification calls for deterministic focused coverage of them.

The accessible shell relationship remains valid on source inspection: the one
`h1` is inside `main`, the calendar is central in the Feature 003 workspace,
and the unchanged complementary inspector is neutral. There is no fake ledger
data. Static inspection cannot replace an assistive-technology session or a new
browser execution, but the markup uses platform-native semantics rather than
custom role emulation.

## Definition of Done Matrix

| Definition of Done item | Verdict | Evidence and limits |
| --- | --- | --- |
| 1. User explicitly approves the amended specification before implementation. | Met | The user explicitly relayed approval in this audit assignment. The written status is stale and should be reconciled by the documentation owner, but this audit does not override direct user authority. |
| 2. A read-only audit marks AC-01 through AC-06 `Met` with current code and retained evidence. | Not Met | This is a read-only audit, but AC-04 and AC-05 are `Not Verifiable` because their required deterministic behavior evidence is incomplete. |
| 3. Audit verifies deterministic coverage for every listed interaction, boundary, focus, semantic, and period-update behavior. | Not Met | Fixed-clock coverage exists for the initial state, modes, several grid movements, December/January previous/next, and January 31 to February 29. Missing coverage includes culture-variant rendering, grid endpoint Tab exits, Today sequential/native behavior, and focus/announcement retention over the required cross-period paths. |
| 4. Audit confirms the Client-only boundary and no inspector, data, settings, API, persistence, dependency, or Feature 005-007 overlap. | Met | Scoped status/diff review identifies no Feature 004 change outside the three allowed CalendarPage files, the two test files, and its specification. Manifests and lock files are unchanged by Feature 004. The inspector source is unchanged and no data layer changes are present. |
| 5. Audit confirms an accessible central desktop calendar work area and controlled clock behavior. | Met | The central shell and inspector relationship is present in current source; retained focused browser evidence reports 1440 by 900 geometry with no page-level horizontal overflow. The calendar injects `TimeProvider`, and all six component cases use a fixed provider. The retained browser/build results were not rerun. |

## Required Next Action

The smallest responsible action is for the test specialist to add deterministic
component coverage for grid entry and both Tab endpoints, Today sequential and
native activation behavior, focus retention after every specified selection and
cross-period movement path, a non-default-culture week/arrow case, and a
leap-day grid transition. It should then run the focused component suite and
the existing 1440 by 900 browser journey. Re-audit AC-04, AC-05, and DoD 2-3
after retaining those results. No production change is indicated by this audit.

The Feature 003 Lucide governance record remains independently blocked as
described in its audit. It is not Feature 004 implementation work and should be
delegated separately; Feature 004 must not be declared to have resolved it.

## Validation Record

- This report is the only file created by this assignment. No product, test,
  configuration, lock, specification, or `BudgetExperiment` file was modified.
- No build, test, restore, browser, server, or runtime command was run.
- After creation, Markdown diagnostics and a CRLF-aware scoped `git diff
  --check` are run only for this report; their exact results are appended by the
  completion report.
