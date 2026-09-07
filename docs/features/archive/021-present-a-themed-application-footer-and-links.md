# Feature 021: Present a Themed Application Footer and Links

## Status

Status: Complete.

- Planned: 2026-09-05.
- Implemented: 2026-09-06.
- Validated: 2026-09-06.
- Depends on the semantic theme-token contract, Feature 008's open-source
  notices route, and Feature 020's two production themes.

## Outcome

Every application route ends with a compact themed footer containing
`© 2026 HouseholdLedger` and a clearly styled `Open-source notices` link. Every
link shipped by the Client has an intentional role-appropriate presentation in
both production themes rather than falling back to generic browser styling.

## User Flow

1. The user reaches the footer from any application route.
2. The footer identifies the application copyright and presents Open-source
   notices as a distinct auxiliary destination.
3. The user follows the notices link and reaches the existing notices page.
4. Links throughout the application remain recognizable, keyboard accessible,
   and visually consistent with the active Workbench theme.
5. Switching between Workbench Dark and Workbench Light preserves link clarity,
   focus, and current-route states.

## Footer Contract

- The application footer contains the exact visible copyright text
  `© 2026 HouseholdLedger` and one native `Open-source notices` link to
  `/open-source-notices`.
- Copyright text is plain text, not a link, and does not imply registration,
  warranty, licensing terms, or ownership beyond the existing repository
  copyright convention.
- The notices link remains an auxiliary/legal destination outside primary
  workspace navigation. It is styled as a compact secondary action or
  equivalent theme-aligned link, with a visible boundary or underline and
  stable hover, active, current, and focus states.
- The footer uses semantic surface, border, text, link, hover, current, and
  focus tokens. It visually belongs to the workspace chrome in both Workbench
  Dark and Workbench Light.
- At wider viewports the copyright and notices destination may share one row.
  At narrow widths they wrap in normal flow without overlap, clipping, or
  page-level horizontal overflow.
- The footer remains after the primary workspace in semantic and keyboard
  order. It does not become a sticky overlay or obscure page content.

## Application Link Presentation Contract

Every rendered `<a>` or Blazor `NavLink` must belong to an intentional visual
role and receive complete theme-aware styling. Native link semantics and
browser navigation behavior remain intact.

| Link role | Examples | Required presentation |
| --- | --- | --- |
| Brand | HouseholdLedger home link | Restrained wordmark treatment, no generic underline at rest, clear hover/current/focus states. |
| Primary navigation | Home, Accounts, Settings | Feature 018 workspace tiles with current and focus treatment. |
| Nested navigation | Account links | Indented subordinate tiles with current and focus treatment. |
| Content/action | Account-card links, no-account route links, retry-adjacent navigation | Theme link color plus persistent underline or another non-color link cue, with hover and focus states. |
| Auxiliary/legal | Open-source notices | Compact footer action treatment distinct from plain body text. |
| External reference | Project and license/source links on the notices page | Theme link color, persistent non-color cue, visible external-link focus, and no loss of native new-tab behavior. |
| Static recovery | Blazor error UI Reload link | Theme-compatible recovery-action styling that remains legible before or after Client startup. |

- A global baseline supplies theme-aware color, underline, focus, visited, and
  disabled conventions where applicable. Role-specific classes refine that
  baseline without resetting links to browser defaults.
- Links remain distinguishable from surrounding non-link text without relying
  only on color. Navigation and brand roles may use shape, boundary, weight, or
  current markers instead of an underline; inline and external links retain a
  persistent text-decoration cue.
- Hover never supplies the only indication that text is interactive.
- `:focus-visible` is at least as prominent as hover and meets the existing
  focus contrast contract. Focus is never removed without an accessible
  replacement.
- Visited styling may remain visually stable for application routes. External
  content links may expose a themed visited state, but it must retain required
  contrast.
- Link text wraps safely. Long URLs are not shown when meaningful labels exist,
  and long account or package names do not force page-level overflow.
- Disabled actions use buttons rather than anchors without valid destinations.
  Generic clickable containers and `href="#"` placeholders are prohibited.

## Responsibility Boundaries

| Surface | Responsibility |
| --- | --- |
| Main layout | Render copyright and the existing notices destination in one semantic footer. |
| Global Client CSS | Establish the accessible theme-aware link baseline and static recovery-link styling. |
| Scoped component CSS | Apply intentional brand, navigation, nested, content, auxiliary, and external-link role styling through semantic tokens. |
| Theme definitions | Supply every token needed by link and footer states in Workbench Dark and Workbench Light. |

No route, notice content, package inventory, API, Application, Domain,
Infrastructure, persistence, OpenAPI, or runtime dependency change is expected.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Copyright footer | Every application route ends with a semantic non-overlay footer containing the exact visible text `© 2026 HouseholdLedger`. |
| AC-02: Styled notices destination | The footer contains one native Open-source notices link to `/open-source-notices` with intentional auxiliary styling and visible hover, current, and keyboard-focus states. |
| AC-03: No generic links | Every shipped Client anchor or `NavLink`, including static recovery and notices-page external links, is covered by the documented baseline and one intentional visual role in both production themes. |
| AC-04: Native semantics | Links retain valid destinations, native activation, expected new-tab behavior where already specified, and programmatic current state where route-aware; no generic clickable container or placeholder anchor replaces them. |
| AC-05: Non-color recognition | Inline, external, navigation, brand, and auxiliary links remain distinguishable from surrounding text without color or hover alone. |
| AC-06: Accessible themed states | Normal, visited where applicable, hover, active, current, and `focus-visible` states remain readable and meet the existing text and focus contrast requirements in Workbench Dark and Workbench Light. |
| AC-07: Responsive footer and links | Footer content and all representative long link labels wrap or reflow without overlap, clipping, inaccessible focus rings, or page-level horizontal overflow at supported desktop and narrow viewports. |
| AC-08: Bounded slice | No new legal page, license text, route, footer navigation group, social link, marketing content, sticky footer behavior, copyright editor, or backend setting is introduced. |

## Implementation Notes

1. Inventory anchors in Razor and static host markup and assign each an explicit
   role; do not rely solely on a universal `a { ... }` rule.
2. Keep a small global accessible baseline so a future unclassified anchor is
   still theme-legible, then enforce role classes through focused structural
   tests or review.
3. Extend semantic tokens only when existing link, text, border, action, and
   focus roles cannot express a required state in both themes.
4. Preserve Feature 008's `target="_blank"` and `rel="noopener noreferrer"`
   behavior for external notice links.
5. Keep the year fixed at `2026` for this initial copyright statement; changing
   ownership text or adopting a year range requires an explicit documentation
   decision rather than deriving legal text from the system clock.

No new runtime dependency is expected.

## Validation

- Client component tests verify exact footer text, one notices destination,
  route/current semantics, footer ordering, and representative role classes.
- Structural checks enumerate rendered/source anchors and prove each is covered
  by the themed baseline and an intentional role, including the static Reload
  link and external notice links.
- Hosted browser checks verify footer layout, navigation to and from notices,
  keyboard focus, non-color cues, representative computed styles, and contrast
  under Workbench Dark and Workbench Light at desktop and one narrow viewport.
- A focused Client build and existing Feature 008 notices tests pass.

## Definition of Done

The feature is complete when every route presents the exact themed copyright
footer and styled notices destination, every shipped link has an intentional
accessible role in both production themes, no browser-default link appearance
leaks into the application, and focused structural, component, responsive, and
hosted browser validation pass.

## Deferred Work

Additional legal destinations, privacy or terms pages, dynamic copyright year
ranges, organization/legal-entity naming changes, social links, footer site
maps, and user-configurable footer content remain separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-05 | Use `© 2026 HouseholdLedger` as the exact initial text. | It matches the repository's existing HouseholdLedger copyright convention without inventing another legal entity. |
| 2026-09-05 | Keep notices auxiliary rather than primary navigation. | Attribution must stay reachable without competing with daily ledger workflows. |
| 2026-09-05 | Define role-specific link treatments over one safe baseline. | Links need a coherent theme while navigation, inline references, and legal destinations should not all look like the same control. |
| 2026-09-05 | Preserve native anchors. | Visual consistency must not reduce browser navigation, keyboard, or assistive-technology behavior. |

## Dependencies

- [Feature 008](008-open-source-notices-page.md) supplies the existing
  notices route, content, and external reference links.
- [Feature 018](018-present-navigation-as-workspace-tiles.md) supplies brand,
  primary navigation, and nested navigation link roles.
- [Feature 020](020-select-a-workbench-light-theme.md) supplies the second
  production theme and cross-theme validation boundary.
