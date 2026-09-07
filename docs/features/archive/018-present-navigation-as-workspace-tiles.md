# Feature 018: Present Navigation as Workspace Tiles

## Status

Status: Complete.

- Planned: 2026-09-05.
- Implemented: 2026-09-06.
- Validated: 2026-09-06.
- Depends on the Workbench design-token contract and Feature 014's account
  navigation hierarchy.

## Outcome

The expanded left navigation presents its destinations as a compact vertical
stack of bordered workspace tiles that belongs to the active theme rather than
as visually generic text links. The HouseholdLedger name in the top-left
workspace chrome is a native link back to the calendar home page.

## User Flow

1. The user opens any application route with navigation expanded.
2. The user sees Home, Accounts, Settings, and the expandable account hierarchy
   as one aligned stack of themed navigation tiles.
3. The user follows any tile using pointer or keyboard navigation.
4. The current destination remains visibly and programmatically distinct.
5. From any route, the user follows the HouseholdLedger name to `/`.

## Navigation Tile Contract

- Each primary destination remains a native link. Styling makes the links read
  as a vertical stack of compact workspace controls with a visible boundary,
  stable height, aligned content, and restrained corners no greater than the
  existing design-system maximum.
- Tiles consume semantic theme tokens for surfaces, borders, text, hover,
  current, active, disabled, and focus states. Component CSS contains no raw
  colors and does not assume a dark background.
- Tiles are adjacent peers, not cards nested inside a navigation card. Spacing
  keeps the stack scannable without turning the pane into a collection of
  oversized dashboard cards.
- Feature 014's Accounts text link and disclosure button share one primary tile
  row while remaining separate controls. Expanded account links form a clearly
  indented subordinate stack and do not visually compete with primary
  destinations.
- Current route remains visible without relying on color alone, using the
  existing programmatic current state plus a stable border, marker, weight, or
  equivalent theme-compatible treatment.
- Long account names wrap or truncate with an accessible full name and never
  overlap the disclosure control or pane boundary.

## Workspace Name Contract

- `HouseholdLedger` in the top-left workspace toolbar becomes a native link to
  `/` with an accessible name and visible keyboard focus.
- On the home route it exposes current-page state consistently with the Home
  navigation tile. Multiple links to Home are intentional: the brand link is a
  stable escape to the primary calendar, while the Home tile anchors the
  navigation hierarchy.
- The link retains the existing restrained wordmark treatment and does not add
  a logo, badge, marketing treatment, or oversized heading.

## Responsibility Boundaries

| Surface | Responsibility |
| --- | --- |
| Main layout markup | Preserve native links, disclosure semantics, hierarchy, and current-route behavior; make the workspace name a home link. |
| Main layout styles | Present primary and nested navigation as themed tile stacks using semantic tokens. |
| Theme contract | Supply every visual value used by the navigation in both current and future themes. |

No API, Application, Domain, Infrastructure, persistence, OpenAPI, route, or
runtime dependency change is expected.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Themed primary stack | Expanded navigation presents Home, Accounts, and Settings as aligned bordered tiles whose normal, hover, active, current, and focus states fit the active application theme. |
| AC-02: Account hierarchy | Feature 014's Accounts disclosure remains a separate operable control in the Accounts tile, and expanded account links appear as an indented subordinate tile stack. |
| AC-03: Native navigation | Every destination remains a native link with its existing route and programmatic current state; styling introduces no clickable generic containers or custom keyboard emulation. |
| AC-04: Home-linked app name | Activating the top-left HouseholdLedger link from any route navigates to `/`; it has visible focus and exposes current-page state on Home. |
| AC-05: Accessible visual states | Current, focus, expanded, and unavailable states remain understandable without color, hover, or icon recognition alone. |
| AC-06: Responsive layout | Tile labels, account names, disclosure control, hierarchy, and workspace name do not overlap, clip incoherently, or create page-level horizontal overflow at supported desktop and narrow viewports. |
| AC-07: Theme boundary | Navigation CSS consumes semantic tokens and remains legible under Workbench Dark and Feature 020's Workbench Light without page-specific color overrides. |
| AC-08: Bounded slice | No route, account behavior, persistent navigation preference, theme selector, pane-control relocation, API, or data behavior is introduced. |

## Implementation Notes

1. Extend the existing navigation list and link classes rather than building a
   generic card or menu framework.
2. Keep the Accounts disclosure hit target distinct from its text link while
   presenting both inside one stable row.
3. Use `NavLink` for the workspace name so current-route semantics stay aligned
   with the existing navigation mechanism.
4. Validate against both theme token sets after Feature 020 is available; this
   feature itself does not add the second theme.

No new runtime dependency is expected.

## Validation

- Client component tests cover native destinations, current-route semantics,
  separate Accounts disclosure behavior, workspace-name destination, and
  keyboard operation.
- Structural style checks verify semantic-token consumption and absence of raw
  colors in scoped navigation CSS.
- Hosted browser checks verify the tile hierarchy, focus/current treatments,
  long account names, and no overlap or page-level overflow at desktop and one
  narrow viewport.

## Definition of Done

The feature is complete when primary destinations visibly form a coherent
stack of theme-owned workspace tiles, account links retain their accessible
nested hierarchy, the HouseholdLedger name links home, and focused component
and hosted responsive validation pass without changing routes or data behavior.

## Deferred Work

Moving the navigation pane control is Feature 019. Theme selection and the
second production theme are Feature 020. Navigation icons, user-reorderable
items, custom groups, badges, counts, and persistent branch state remain
separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-05 | Style native links as compact tiles. | The requested boxed appearance should improve hierarchy without replacing accessible platform navigation. |
| 2026-09-05 | Keep nested account links visually subordinate. | Accounts remain one primary destination with account-specific routes beneath it. |
| 2026-09-05 | Make the workspace name a second Home link. | A stable brand-home action is conventional and does not remove the explicit navigation destination. |

## Dependencies

- [Feature 014](014-expand-accounts-in-workspace-navigation.md) supplies the
  Accounts disclosure and nested account links.
- [Feature 016](016-configure-global-display-currency.md) supplies the Settings
  destination consumed by the final primary stack.
- [Feature 009](009-themeable-design-system-and-workbench-dark-theme.md)
  supplies the semantic token and Workbench Dark contracts.
