# Feature 019: Collapse Navigation from Its Pane

## Status

Status: Complete.

- Planned: 2026-09-05.
- Implemented: 2026-09-06.
- Validated: 2026-09-06.
- Depends on the existing independently collapsible workspace panes.

## Outcome

The navigation pane owns its collapse control. An arrow button in the pane's
top corner collapses the navigation to a narrow rail, and the opposite arrow in
that rail restores it. The former hamburger-style navigation button is removed
from the top-right workspace toolbar.

## User Flow

1. The user sees an arrow control in the top corner of the expanded navigation
   pane.
2. The user activates it and the navigation contents collapse without covering
   the main workspace.
3. A narrow navigation rail remains with a restore arrow in the corresponding
   corner.
4. The user activates the restore arrow and the full navigation returns with
   its prior Accounts branch state intact.
5. Inspector controls continue to work independently.

## Pane Control Contract

- Expanded navigation places one native collapse button in the pane header's
  top trailing corner. In the default left-side layout, its visible arrow
  points toward the pane edge to communicate collapse.
- Collapsed navigation becomes a narrow normal-flow rail containing only the
  restore control. It is not fully hidden, because a control inside hidden
  content could not restore it.
- The restore arrow points toward the main workspace in the default layout.
  Accessible names remain placement-neutral: `Collapse navigation` and
  `Expand navigation` rather than `Move left` or `Open right`.
- The button exposes accurate `aria-expanded` and `aria-controls` state and has
  a tooltip or equivalent accessible explanation. The icon is decorative to
  assistive technology.
- The existing top-toolbar navigation hamburger/menu button is removed. The
  toolbar retains the HouseholdLedger home link and any inspector control that
  remains valid for the current route.
- Collapsing removes navigation destinations, account links, loading states,
  and headings from keyboard and accessibility navigation while preserving
  transient Accounts branch state for restoration.
- Navigation and inspector state remain independent. Neither pane overlays the
  central work area, and collapsing navigation releases all width except the
  stable restore rail.

## Theme and Responsive Behavior

- Arrow, rail, pane header, hover, focus, and boundary colors consume existing
  semantic tokens and work in Workbench Dark and Feature 020's Workbench Light.
- The control occupies a stable touch target and does not overlap the
  Navigation heading, tile stack, account disclosure, or long labels.
- At narrow viewports, the expanded navigation remains in normal flow. Its
  collapsed rail remains reachable before the main content without creating an
  empty full-height region or page-level horizontal overflow.
- Theme-owned workspace placement remains supported. If a future theme moves
  navigation, the accessible names remain correct; a theme may select a
  directionally appropriate existing Lucide chevron without changing behavior.

## Responsibility Boundaries

| Surface | Responsibility |
| --- | --- |
| Main layout | Relocate the control, render expanded-pane and collapsed-rail states, and preserve independent transient pane state. |
| Workspace grid | Allocate a stable collapsed navigation track and release the expanded track without overlays or abandoned space. |
| Theme styles | Style the pane-owned arrow and rail through semantic tokens. |

No route, API, Application, Domain, Infrastructure, persistence, OpenAPI, or
runtime dependency change is expected.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Pane-owned collapse | Expanded navigation has one arrow button in its top corner; the former top-toolbar hamburger navigation button is absent. |
| AC-02: Reachable restoration | Activating collapse leaves a narrow normal-flow rail with an Expand navigation arrow that restores the full pane. |
| AC-03: Accurate accessible state | Both states use a native keyboard-operable button with accurate accessible name, `aria-expanded`, `aria-controls`, visible focus, and explanatory tooltip; arrow direction is not the only cue. |
| AC-04: Content removal and restoration | Collapsed navigation content is absent from keyboard and accessibility navigation; restoration returns the destinations and preserves the prior Accounts branch state. |
| AC-05: Independent panes | Collapsing or expanding navigation does not alter inspector state, route, selected date, account selection, or main-workspace content. |
| AC-06: Stable themed layout | Expanded pane and collapsed rail remain in normal flow, use semantic tokens, and do not overlap content or create page-level horizontal overflow in either production theme at supported viewports. |
| AC-07: Bounded slice | No persistent pane preference, draggable resizing, pane docking, navigation redesign, theme selection, or product-data behavior is introduced. |

## Implementation Notes

1. Replace the current hidden-navigation grid state with explicit expanded and
   collapsed-rail states.
2. Keep one logical control across both states so focus remains on the button
   after activation.
3. Reuse directionally appropriate Lucide chevrons from the existing approved
   icon package; add no dependency.
4. Preserve semantic DOM order and normal-flow layout regardless of visual
   theme placement.

## Validation

- Client component tests cover toolbar removal, pane-header placement,
  accessible state, focus retention, collapsed content exclusion, restoration,
  branch-state preservation, and inspector independence.
- Hosted browser checks measure expanded and collapsed geometry and verify no
  overlap or page-level overflow at desktop and one narrow viewport in both
  Workbench themes.
- A focused Client build and existing workspace/navigation regressions pass.
- The owned Firefox journey passed against a fresh Release publish and isolated
  PostgreSQL 18 database. Direct live checks confirmed the collapsed rail kept
  keyboard focus, exposed no destinations or headings, occupied a stable
  60-pixel normal-flow track, and restored without changing route content.

## Definition of Done

The feature is complete when navigation collapses and restores exclusively from
a reachable pane-owned arrow control, the collapsed rail preserves access and
layout, the old hamburger control is gone, other workspace state is unchanged,
and focused component and hosted responsive validation pass.

## Deferred Work

Persistent pane state, draggable resizing, user-selected pane placement,
activity bars, and docking commands remain separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-05 | Retain a narrow rail while collapsed. | A control located in a fully hidden pane cannot provide the requested restore action. |
| 2026-09-05 | Use arrows visually and placement-neutral accessible names. | Direction helps sighted users while names remain correct if theme layout changes later. |
| 2026-09-05 | Preserve collapse state only in memory. | The request changes control placement, not preference persistence. |

## Dependencies

- [Feature 003](archive/003-workspace-navigation-and-ui-foundation.md) supplies
  independently controlled, normal-flow workspace panes.
- [Feature 014](014-expand-accounts-in-workspace-navigation.md) supplies the
  transient Accounts branch state preserved through pane collapse.
- [Feature 018](018-present-navigation-as-workspace-tiles.md) supplies the final
  pane header and navigation tile presentation.
