# Feature 023: Navigate from the Collapsed Icon Rail

## Status

Status: Complete.

- Planned: 2026-09-05.
- Implemented: 2026-09-06.
- Validated: 2026-09-06.
- Depends on Feature 014's account hierarchy, Feature 018's navigation tiles,
  and Feature 019's collapsed navigation rail.

## Outcome

Every left-navigation destination has a corresponding icon. Expanded
navigation shows each icon with its text, while collapsed navigation keeps the
icons as usable links and clearly marks the user's current application
location.

## User Flow

1. The user sees an icon beside every primary and account-specific navigation
   label in the expanded pane.
2. The user collapses navigation using the pane-owned arrow.
3. The narrow rail retains the expand arrow and icon-only navigation links.
4. The icon for the current route remains visibly and programmatically current.
5. The user can identify an icon from its tooltip or accessible name and follow
   it without first expanding the pane.
6. Expanding navigation restores the same destinations with icons and labels.

## Icon Assignment Contract

Use existing Lucide icons from the approved icon source. Initial assignments
are explicit and stable:

| Navigation item | Icon meaning |
| --- | --- |
| Home | Calendar or house icon representing the calendar home workspace. |
| Accounts | Wallet or accounts-group icon representing the account catalog. |
| Settings | Settings/gear icon representing application settings. |
| Persisted account | One consistent wallet, landmark, or account icon identifying an account-specific transaction destination. |

- Each item has one icon in expanded and collapsed states. Icons do not change
  meaning between themes or pane states.
- Persisted account links may share one account icon because the account name is
  dynamic. Their accessible names and tooltips contain the full persisted
  account name; icon shape alone is not expected to distinguish accounts.
- Icons are decorative inside labeled links and hidden from assistive
  technology. The native link text supplies the expanded accessible name; an
  equivalent visually hidden label supplies it in the collapsed rail.
- No icon is selected solely because it is visually attractive. The chosen
  symbol must have a familiar relationship to its destination.
- The feature adds no icon package or hand-authored replacement SVG.

## Expanded Navigation Contract

- Feature 018's primary tiles show a stable leading icon followed by the text
  label. Icon and text alignment is consistent across Home, Accounts, and
  Settings.
- The Accounts disclosure remains a separate chevron control and is not
  confused with the Accounts destination icon.
- Expanded persisted-account links show the account icon with the account name
  in the existing indented subordinate stack.
- Long labels wrap or truncate safely without moving the icon, disclosure
  control, current marker, or tile dimensions incoherently.
- Current, hover, active, unavailable, and focus states style the complete link,
  not only the icon.

## Collapsed Icon Rail Contract

- This feature extends Feature 019: the collapsed navigation rail contains the
  Expand navigation arrow followed by icon-only native links for every
  currently available navigation item. It is no longer limited to the restore
  control.
- Primary icons retain the same order as expanded navigation. When the Accounts
  branch is expanded, persisted-account icons follow Accounts as a visually
  subordinate group; when it is collapsed, those account links are absent.
- Direct navigation to `/accounts/{accountId}` expands the Accounts branch as
  required by Feature 014, so the current account's icon is present in the
  collapsed rail.
- Every icon-only link has a hover and keyboard-focus tooltip containing its
  full destination label. Tooltips do not cover the active control, are
  constrained to the viewport, and are supplemental to accessible names.
- The current route exposes `aria-current="page"` on the matching native link.
  A persistent non-color marker, boundary, shape, or equivalent treatment
  distinguishes it without hover. On an account-detail route, both the Accounts
  group context and the exact current account remain understandable, but only
  the exact destination claims `aria-current="page"`.
- The collapsed rail remains keyboard navigable in document order. Focus rings
  are not clipped, and expanding the pane preserves the current route and
  Accounts branch state.
- Unavailable account-catalog state keeps Home, Accounts, and Settings icons
  usable and provides a named retry action without inventing account icons.
- If enough persisted accounts exceed the available viewport height, the
  navigation-items region scrolls internally while the expand control remains
  reachable. The page itself does not gain horizontal overflow.

## Theme and Responsive Behavior

- Icons inherit semantic foreground colors from their links. Current, hover,
  focus, disabled, and unavailable states consume semantic tokens and remain
  legible under Workbench Dark and Workbench Light.
- Icon slots and rail width use stable dimensions so loading text, labels,
  tooltips, current markers, and account count changes do not shift the main
  workspace unpredictably.
- At narrow viewports, the collapsed rail stays in normal document flow and
  remains usable without overlaying the main content. The implementation may
  present the rail horizontally if required by the existing narrow workspace
  arrangement, provided semantic order and all destinations remain intact.
- Icons never provide the sole accessible name or sole indication of current,
  expanded, unavailable, or actionable state.

## Responsibility Boundaries

| Surface | Responsibility |
| --- | --- |
| Navigation model/layout | Assign one supported icon to each destination and render shared expanded/collapsed link semantics. |
| Account navigation | Render dynamic account links in both states and preserve Feature 014's authoritative catalog and branch behavior. |
| Navigation styles | Provide stable icon slots, collapsed rail layout, tooltips, current markers, internal overflow, and theme-token consumption. |
| Route state | Mark the exact current destination and retain understandable parent Accounts context. |

No API, Application, Domain, Infrastructure, persistence, OpenAPI, route, or
runtime dependency change is expected.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Icons for every destination | Expanded navigation shows a meaningful icon beside Home, Accounts, Settings, and every persisted-account link without changing their labels or routes. |
| AC-02: Usable collapsed rail | Collapsing navigation leaves the expand arrow and icon-only native links for every item allowed by the current Accounts branch state; each link remains directly navigable. |
| AC-03: Current location | In expanded and collapsed states, the exact current link exposes `aria-current="page"` and a persistent non-color visual marker; account routes also retain understandable Accounts group context. |
| AC-04: Understandable icons | Every icon-only link has the same accessible name as its expanded link and a viewport-safe hover/focus tooltip; icons are decorative and never the sole accessible cue. |
| AC-05: Account hierarchy continuity | Direct account routes expose the current account icon, expanding/collapsing Accounts controls account-icon visibility, and catalog loading/error/refresh behavior remains truthful. |
| AC-06: Keyboard and focus operation | Rail links, Accounts disclosure, retry, and expand arrow follow logical keyboard order with visible unclipped focus and no custom anchor keyboard emulation. |
| AC-07: Stable responsive themes | Icon slots, rail, internal overflow, tooltips, and current states remain coherent without overlap or page-level horizontal overflow in both production themes at supported desktop and narrow viewports. |
| AC-08: Existing behavior preserved | Expanding restores icon-plus-label tiles and prior branch state; icon navigation does not alter inspector state, calendar state, settings, or account data. |
| AC-09: Bounded slice | No new icon dependency, custom SVG artwork, editable icon choice, navigation reordering, badges, counts, favorites, persistent collapse preference, or backend behavior is introduced. |

## Implementation Notes

1. Represent destination label, route, icon, hierarchy, and current-match rule
   in one Client navigation model or equivalent shared rendering path so
   expanded and collapsed states cannot drift.
2. Reuse `NavLink` and native link behavior in both states. Do not turn icons
   into generic buttons that manually navigate.
3. Keep Accounts destination and disclosure as separate controls in both states;
   use distinct accessible names and visual symbols.
4. Extend Feature 019's rail width only as much as stable icon touch targets and
   focus treatment require.
5. Reuse the existing tooltip visual language and approved Lucide closure.

No new runtime dependency is expected.

## Validation

- Client component tests enumerate the authoritative navigation items and prove
  each has one expanded icon-plus-label link and one corresponding collapsed
  icon link with the same route and accessible name.
- Tests cover exact current semantics for Home, Accounts, one account route,
  Settings, branch expansion, catalog unavailable/retry, keyboard order, and
  preservation of inspector and route state.
- Structural checks verify icons come from the existing Lucide source and are
  hidden from assistive technology inside named links.
- Hosted browser checks navigate through every primary icon and one account
  icon while collapsed, inspect tooltips/focus/current markers, exercise many
  accounts with internal scrolling, and verify desktop/narrow layouts under
  Workbench Dark and Workbench Light.
- A focused Client build and existing navigation, account, workspace, and
  theme regressions pass.

## Definition of Done

The feature is complete when every navigation destination has a stable icon,
the collapsed rail remains a fully usable and accessible navigation surface,
the exact current location stays apparent without color or hover alone,
account hierarchy and route behavior remain truthful, and focused component and
hosted cross-theme responsive validation pass.

## Deferred Work

User-selected icons, per-account icon or color customization, favorites,
badges, counts, navigation reordering, activity-bar concepts, and persistent
pane or branch preferences remain separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-05 | Keep navigation links active in the collapsed rail. | The user should retain orientation and navigation rather than receiving only a pane restore control. |
| 2026-09-05 | Use one shared icon for dynamic account links. | Account names provide identity; inventing unique symbols or colors would add unsupported account metadata. |
| 2026-09-05 | Show account icons only when the Accounts branch is expanded. | Collapsed-pane presentation should preserve the hierarchy's existing disclosure state rather than flatten it. |
| 2026-09-05 | Mark only the exact destination with `aria-current`. | Parent context can be styled without falsely announcing two current pages. |
| 2026-09-05 | Reuse the existing Lucide package. | The current icon source already covers the required familiar symbols without another dependency. |

## Dependencies

- [Feature 014](014-expand-accounts-in-workspace-navigation.md) supplies the
  authoritative dynamic account hierarchy and disclosure behavior.
- [Feature 018](018-present-navigation-as-workspace-tiles.md) supplies expanded
  navigation tile presentation and the Settings destination.
- [Feature 019](019-collapse-navigation-from-its-pane.md) supplies the
  pane-owned arrow and collapsed rail extended by this feature.
- [Feature 020](020-select-a-workbench-light-theme.md) supplies both production
  themes used for visual validation.
