# Feature 022: Collapse and Reveal the Inspector Pane

## Status

Status: Complete.

- Planned: 2026-09-05.
- Implemented: 2026-09-06.
- Validated: 2026-09-06.
- Depends on the existing selected-date state and Feature 019's pane-owned
  collapse pattern.

## Outcome

The inspector pane uses the same pane-owned arrow mechanism as navigation. A
user can collapse it to a narrow rail and restore it from that rail, while
selecting a calendar date automatically expands the inspector so the selected
day's transaction details and controls are immediately visible.

## User Flow

1. On the calendar route, the user sees an arrow control in the inspector
   pane's top corner.
2. The user activates it and the inspector contents collapse to a narrow rail.
3. The user can restore the inspector using the opposite arrow in that rail.
4. If the inspector is collapsed and the user selects a calendar date, the
   inspector automatically expands and loads that date's content.
5. Navigation state remains unchanged throughout the interaction.

## Inspector Control Contract

- Expanded inspector places one native collapse button in the pane header's top
  leading corner. In the default right-side layout, its visible arrow points
  toward the pane edge to communicate collapse.
- Collapsed inspector becomes a narrow normal-flow rail containing only the
  restore button. It is not fully hidden, because a control inside hidden
  content could not restore it.
- The restore arrow points toward the main workspace in the default layout.
  Accessible names remain placement-neutral: `Collapse inspector` and
  `Expand inspector` rather than `Move right` or `Open left`.
- The button exposes accurate `aria-expanded` and `aria-controls`, has a tooltip
  or equivalent accessible explanation, and retains visible keyboard focus.
  Its Lucide arrow is decorative to assistive technology.
- The existing search-style inspector button is removed from the top workspace
  toolbar. No second inspector toggle remains elsewhere.
- Collapsing removes the Inspector heading, selected-date content, forms,
  transaction list, loading/error states, and other pane content from keyboard
  and accessibility navigation while preserving the selected date and loaded
  application state.
- Restoring the inspector shows the current selected-date state; it does not
  reset the date or initiate unrelated navigation.

## Automatic Reveal Contract

- Selecting a calendar date through pointer activation, Enter, Space, managed
  Tab movement, arrow-key movement, or calendar previous/next behavior expands
  the inspector when it is collapsed.
- The same Client-owned selected-date change notification that supplies the
  inspector context triggers expansion. Calendar markup does not directly
  manipulate layout internals.
- Automatic expansion occurs only when the effective selected date changes or
  a calendar interaction deliberately reselects a date for inspection. Merely
  loading, refreshing, or navigating among non-calendar routes does not open
  the inspector unexpectedly.
- After automatic expansion, focus remains with the activated calendar date.
  The inspector does not steal focus; its loading or content change is
  announced through the existing status semantics.
- The inspector loads and displays the newly selected date using existing stale
  response protection. Content from the prior selected date cannot reappear.
- On routes where the inspector is unavailable, no empty rail or inspector
  toggle is rendered. Returning to Calendar restores the route's normal
  inspector availability and current transient pane state.

## Theme and Responsive Behavior

- Arrow, rail, pane header, hover, focus, and boundary styling consume semantic
  tokens and work in Workbench Dark and Workbench Light.
- The control has a stable touch target and does not overlap the Inspector
  heading, transaction content, forms, dialogs, or long status text.
- Expanded and collapsed inspector states remain in normal flow and never cover
  the calendar or navigation.
- At narrow viewports, the collapsed rail remains reachable in document order
  without creating an empty full-height region or page-level horizontal
  overflow.
- A future theme may choose the directionally appropriate existing Lucide
  chevron if it repositions the inspector; behavior and accessible names remain
  unchanged.

## Responsibility Boundaries

| Surface | Responsibility |
| --- | --- |
| Main layout | Relocate the inspector control, render expanded-pane and collapsed-rail states, and preserve independent transient pane state. |
| Selected-date state | Notify the layout when deliberate calendar selection should reveal the inspector. |
| Transaction inspector | Render the current selected date and preserve its existing request and stale-response behavior; it does not own pane visibility. |
| Workspace grid and themes | Allocate the collapsed inspector track and style both states through semantic tokens without overlays. |

No API, Application, Domain, Infrastructure, persistence, OpenAPI, transaction
contract, or runtime dependency change is expected.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Pane-owned collapse | On Calendar, the expanded inspector has one arrow button in its top corner and the former top-toolbar search-style inspector button is absent. |
| AC-02: Reachable restoration | Activating collapse leaves a narrow normal-flow inspector rail with an Expand inspector arrow that restores the current inspector content. |
| AC-03: Automatic date reveal | Selecting a calendar date by every supported pointer or keyboard path expands a collapsed inspector and presents that selected date's authoritative state. |
| AC-04: Focus and announcement | Automatic expansion does not steal focus from the selected calendar date; inspector loading/content updates retain accessible status announcements. |
| AC-05: Accurate accessible state | Both manual states use a native button with accurate accessible name, `aria-expanded`, `aria-controls`, visible focus, and explanation; arrow direction is not the only cue. |
| AC-06: Content and request safety | Collapsed inspector content is absent from keyboard and accessibility navigation; selected date is preserved, and obsolete date responses cannot replace the current content after expansion. |
| AC-07: Independent panes and routes | Inspector changes never alter navigation expansion, Accounts branch state, route, or calendar mode; routes without an inspector render neither its pane nor a stranded rail/control. |
| AC-08: Stable themed layout | Expanded pane and collapsed rail remain in normal flow and do not overlap content or cause page-level horizontal overflow in either production theme at supported viewports. |
| AC-09: Bounded slice | No persistent pane preference, focus transfer into the inspector, inspector redesign, transaction behavior, draggable resizing, pane docking, or backend change is introduced. |

## Implementation Notes

1. Mirror Feature 019's expanded-pane and collapsed-rail structure so both side
   panes share behavior without forcing one reusable component abstraction.
2. Keep one logical inspector button across states so focus remains on it after
   manual collapse or expansion.
3. Preserve the existing layout subscription to selected-date changes and make
   its automatic expansion behavior explicit and fully tested.
4. Distinguish inspector availability by route from user-collapsed state; an
   unavailable inspector must not reserve a collapsed rail.
5. Reuse directionally appropriate Lucide chevrons from the existing package;
   add no dependency.

## Validation

- Client component tests cover toolbar-button removal, pane-header placement,
  manual collapse/restore, accessible state, focus retention, content
  exclusion, route availability, and navigation independence.
- Calendar/inspector component tests exercise pointer, Enter, Space, Tab,
  Shift+Tab, arrow, and previous/next date paths with the inspector initially
  collapsed, confirming expansion and selected-date continuity.
- Hosted browser checks collapse the inspector, select a date, verify automatic
  expansion without focus theft, and measure both pane states at desktop and
  one narrow viewport under Workbench Dark and Workbench Light.
- A focused Client build and existing calendar, transaction-inspector, and
  workspace regressions pass.

## Definition of Done

The feature is complete when the inspector collapses and restores through one
reachable pane-owned arrow, date selection reliably reveals it without moving
focus or showing stale content, routes without an inspector reserve no rail,
other workspace state remains unchanged, and focused component and hosted
responsive validation pass.

## Deferred Work

Persistent pane state, automatic focus transfer, inspector resizing, docking,
multiple inspectors, route-wide inspector availability, and redesign of
transaction content remain separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-05 | Mirror navigation's collapsed-rail pattern. | Both workspace panes should use one predictable interaction model and retain a reachable restore control. |
| 2026-09-05 | Automatically expand from selected-date state. | Date selection is the user action that gives the inspector useful context, while layout remains the owner of visibility. |
| 2026-09-05 | Keep focus on the calendar date. | Revealing related details should not interrupt keyboard exploration of the calendar. |
| 2026-09-05 | Render no rail on unsupported routes. | A restore control is misleading when the route has no inspector content. |

## Dependencies

- [Feature 004](archive/004-calendar-item-selection-and-inspector-detail-contract.md)
  supplies supported calendar selection and keyboard paths.
- [Feature 005](005-record-a-transaction-for-selected-day.md) supplies the
  selected-day inspector content and request lifecycle.
- [Feature 019](019-collapse-navigation-from-its-pane.md) supplies the shared
  pane-owned arrow and collapsed-rail interaction pattern.
- [Feature 020](020-select-a-workbench-light-theme.md) supplies the second
  production theme used for cross-theme validation.
