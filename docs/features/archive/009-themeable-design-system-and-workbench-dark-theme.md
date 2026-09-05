# Feature 009: Themeable Design System and Workbench Dark Theme

## Status

Status: Complete.

- Planned: 2026-09-01.
- Completed: 2026-09-02.
- User direction: build a custom, themeable HouseholdLedger design system.
- The first and default theme is inspired by Visual Studio Code's dark
  workbench without copying its branding, assets, or exact stylesheet.
- Focused Client structure tests, the PostgreSQL-backed hosted browser journey,
  and manual desktop/mobile inspection verify the acceptance criteria and
  Definition of Done.

## Outcome

HouseholdLedger opens in a cohesive dark workspace named `Workbench Dark`.
Every current Client surface draws its color, typography, focus, shape, and
state styling from a small semantic design-token contract. The workspace keeps
three major regions inspired by Visual Studio Code: navigation, central work,
and inspection. A future theme can replace visual tokens and reposition those
regions without changing component markup or business behavior.

This feature proves themeability through the token boundary. It does not add a
theme picker, a second production theme, or a user-facing pane-docking command.

## Visual Direction

`Workbench Dark` is a quiet operational interface suited to repeated household
ledger work:

- Neutral charcoal canvas, chrome, panels, inputs, and elevated surfaces create
  hierarchy without a blue-tinted or single-hue dark palette.
- Restrained blue identifies links, primary actions, focus, and selection.
  Green, amber, red, and blue remain distinct semantic status families.
- Compact native controls, one-pixel borders, corners no larger than `0.5rem`,
  and minimal elevation keep the workspace tool-like rather than card-heavy.
- The default desktop arrangement places navigation on the left, the calendar
  work area in the center, and the inspector on the right. The center remains
  the dominant region; surrounding panes support rather than displace it.
- A Windows-native UI font stack supports dense labels and controls. Monospace
  remains limited to code-like or license text. Typography does not scale with
  viewport width and letter spacing is never negative.
- Selected, current, error, success, disabled, hover, active, and focus states
  remain understandable without color alone where the state is meaningful.
- The theme uses no gradients, decorative glows, background blobs, marketing
  composition, or Visual Studio Code logos and product marks.

## Design-System Contract

The custom system uses CSS custom properties and existing Blazor/CSS isolation.
No design-system, CSS-framework, utility-class, CSS-in-JS, or theme package is
introduced.

### Theme Definition

- `wwwroot/index.html` declares `data-theme="workbench-dark"` on the root
  element so the default is established before WebAssembly starts.
- `wwwroot/css/app.css` owns the `Workbench Dark` token values, `color-scheme:
  dark`, page defaults, and the matching browser `theme-color` contract.
- Theme values are separated from component rules. A future production theme
  adds another root theme declaration that supplies the same semantic and
  workspace-layout tokens.
- Components do not read a theme name or contain theme-selection logic.

### Semantic Tokens

Use an `--hl-*` namespace and define only tokens needed by current surfaces:

| Family | Required roles |
| --- | --- |
| Surfaces | canvas, chrome, primary work area, panel, elevated content, input, hover, selected, and status-subtle backgrounds |
| Text | primary, secondary, disabled, inverse, link, link-hover, and status text |
| Borders | default, strong, input, selected, and status borders |
| Actions | primary, primary-hover, primary-text, focus ring, and native-control accent |
| Status | success, warning, error, and information foreground/background pairs |
| Typography | UI and monospace families plus the repeated heading and control weights |
| Shape and motion | repeated control radius, panel radius, focus width/offset, elevation, and short transition duration |
| Workspace layout | expanded and collapsed grid areas, columns, rows, pane extents, and region placement |

Component-local layout dimensions and one-off geometry remain local. Do not
create an exhaustive spacing scale or generic component abstraction without a
current repeated use.

### Themeable Workspace Layout

The layout retains one stable semantic structure: toolbar, a workspace grid
containing navigation, main work area, and inspector, then the auxiliary
footer. Themes control visual placement inside the workspace grid through
named CSS grid areas and sizing tokens.

- `Workbench Dark` uses the default desktop areas `navigation / main /
  inspector`, with navigation on the left and inspector on the right.
- The contract supports a future theme swapping the two side panes or arranging
  navigation above the main area and inspector below it. It also supports the
  inverse top/bottom arrangement.
- Theme layout values cover all four pane-visibility combinations: both panes
  shown, navigation hidden, inspector hidden, and both hidden. Hiding a pane
  must release its row or column rather than leave an empty dock.
- Main-layout CSS assigns stable region names and consumes the theme's grid
  area, track, and extent tokens. A theme must not select elements by incidental
  child order to position them.
- CSS placement never changes landmark meaning, accessible names, document
  order, or component state. The semantic DOM remains navigation, main work,
  then inspector even when a theme presents another visual arrangement.
- Pane controls and tooltips identify `Navigation` and `Inspector` without
  encoding left, right, top, or bottom in their accessible names or icons.
- Any future shipped arrangement must separately verify that its visual order,
  reading order, and keyboard focus order remain understandable. The token
  contract does not authorize arbitrary overlay, floating, or disconnected
  placement.

Toolbar and auxiliary-footer placement remain shell responsibilities in this
slice. A later feature can add them to the theme layout contract if a concrete
theme needs that flexibility.

### Consumption Rules

- Scoped component CSS consumes semantic tokens for every rendered color,
  focus ring, font family, repeated radius, elevation, and state transition.
- Raw color values are permitted only inside theme token declarations and the
  static browser `theme-color` value. They do not remain in component CSS.
- Existing `--workspace-*` and `--calendar-*` color variables are replaced by
  the common semantic contract rather than retained as a second token system.
- Hard-coded workspace columns and child-order assumptions are replaced by the
  named-area layout contract. Component-local responsive safeguards remain in
  the layout stylesheet.
- Native controls inherit the dark color scheme and expose visible hover,
  active, disabled, validation, and keyboard-focus states.
- Existing reduced-motion behavior remains effective; theme transitions do not
  animate on initial load and respect `prefers-reduced-motion`.

## Current Surface Migration

The initial system covers all shipped Client presentation surfaces:

- static loading and unhandled-error UI;
- workspace toolbar, navigation, calendar work area, inspector, and footer;
- pane toggles and tooltips;
- expanded and collapsed three-region workspace arrangements;
- Today, Week, and Month calendar modes, navigation, selected date, and current
  date;
- transaction fields, primary action, list, loading, empty, validation, error,
  and success states;
- health status, retry action, and availability states;
- not-found and open-source-notices pages, including long license blocks.

This is a visual-system and layout-contract migration only. `Workbench Dark`
retains the existing region locations, semantics, content, keyboard behavior,
responsive flow, pane behavior, transaction behavior, and routes.

## Out Of Scope

- A theme picker, settings page, operating-system theme detection, persistence,
  cross-device synchronization, runtime theme service, or user-facing docking
  controls.
- A second production theme, user-authored themes, theme import/export, or a
  public plug-in contract.
- Shipping a flipped or top/bottom arrangement in `Workbench Dark`; synthetic
  overrides exist only to prove that the layout contract can support a future
  theme.
- New Razor components solely to demonstrate styles, a Storybook-style site,
  general component library, utility-class framework, or package dependency.
- Navigation redesign, responsive-layout redesign, calendar behavior changes,
  transaction workflow changes, API, Domain, Application, Infrastructure,
  persistence, or OpenAPI work.
- Pixel-for-pixel reproduction of Visual Studio Code or use of its logos,
  icons, fonts, source code, or trademarked product presentation.

## Responsibility Boundaries

| Surface | Responsibility |
| --- | --- |
| Static host | Establish the default theme and matching browser chrome before Blazor starts. |
| Global Client CSS | Define visual and workspace-layout tokens, dark native-control behavior, and document-level defaults. |
| Main layout | Preserve semantic region order and consume theme-owned named grid areas and sizing tokens. |
| Scoped Client CSS | Consume semantic tokens while retaining component-owned responsive safeguards and local geometry. |
| Component tests | Enforce the theme declaration and stylesheet contract without asserting implementation-irrelevant exact formatting. |
| Hosted browser test | Prove default computed styles, visual and layout token overrides, contrast, focus, pane collapse, and representative desktop/mobile rendering. |

No non-Client production layer participates.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Dark default before startup | The initial static document and the running application use `Workbench Dark`, declare a dark color scheme, and use matching browser chrome without first presenting the former light canvas. |
| AC-02: One semantic theme contract | All current Client surfaces consume the documented `--hl-*` semantic tokens. Scoped CSS contains no raw rendered colors or competing workspace/calendar color-token families. |
| AC-03: Cohesive current UI | Workspace, calendar, inspector, forms, statuses, errors, loading, not-found, and notices surfaces render with the stated charcoal workbench hierarchy and distinct interaction/status families. No text, icon, control, or state becomes unreadable or visually lost. |
| AC-04: Accessible states and contrast | Normal text meets WCAG AA `4.5:1`, large text meets `3:1`, and meaningful focus indicators and non-text control boundaries meet `3:1` against adjacent colors. Keyboard focus is visible, status is not communicated by color alone, and reduced-motion behavior is preserved. |
| AC-05: Visual themeability is real | In a hosted browser check, overriding representative semantic tokens changes the consuming workspace, control, and status computed styles without changing Razor markup, component classes, or C# state. Removing the override restores `Workbench Dark`. |
| AC-06: Layout themeability is real | With the same semantic DOM, synthetic theme-token overrides can swap navigation and inspector sides and can arrange navigation/main/inspector as top/center/bottom. Every expanded/collapsed combination releases hidden tracks, keeps main dominant and reachable, and preserves truthful placement-neutral controls. Removing overrides restores the default arrangement. |
| AC-07: Stable responsive presentation | At `1440 x 900` and `500 x 844`, the default and synthetic representative arrangements have no incoherent overlap, clipping, unreadable text, abandoned empty tracks, or new page-level horizontal overflow. Visual order does not make reading or keyboard focus order confusing. |
| AC-08: Custom and bounded | No design-system or styling dependency is added; no theme picker, docking command, product behavior, route, API, persistence, or non-Client production code changes. |

## Implementation Plan

1. Define the compact `--hl-*` token contract and `Workbench Dark` values in
   `wwwroot/css/app.css`; declare the theme and browser color in
   `wwwroot/index.html`.
2. Introduce named workspace regions and theme-owned expanded/collapsed grid
  tokens. Keep the semantic DOM order stable, make pane controls
  placement-neutral, and retain `Workbench Dark`'s current left/center/right
  arrangement.
3. Migrate global loading/error styles and each existing scoped Client
  stylesheet to semantic tokens. Preserve behavior and component ownership.
4. Add focused structural tests for the root theme declaration, required token
  families, stable semantic region order, placement-neutral controls, removal
  of legacy tokens, and absence of raw colors from scoped CSS.
5. Extend the hosted browser journey to verify representative computed styles,
  visual-token overrides, side-swap and top/bottom layout overrides, all pane
  visibility combinations, keyboard focus, contrast calculations, and
  screenshots at desktop and mobile-sized viewports.
6. Run the Client build and component tests, then the focused hosted browser
   checks. Review screenshots for hierarchy, state clarity, clipping, and
   overlap rather than using brittle pixel-perfect snapshots.

## Definition Of Done

Feature 009 is complete when all current Client surfaces use the custom token
contract, `Workbench Dark` is present before startup and remains the default,
representative visual overrides and alternate three-pane arrangements prove
themeability, every pane-visibility state remains usable, contrast and focus
checks pass, desktop/mobile screenshots show a coherent workspace, focused
Client and browser tests pass, and the full solution builds without warnings.

A second theme, runtime switching, persisted preference, user-facing docking,
and a separate final documentation audit are not required for completion.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-01 | Build a custom design system rather than adopt an existing one. | The maintainer wants direct ownership of the visual contract and no external design-system dependency. |
| 2026-09-01 | Make `Workbench Dark` the first and default theme. | It provides the requested Visual Studio Code dark-mode inspiration while retaining a distinct HouseholdLedger identity. |
| 2026-09-01 | Prove themeability with semantic-token overrides, not a second shipped theme. | It validates the architecture without expanding this foundation into theme choice and preference workflows. |
| 2026-09-01 | Migrate every current Client surface in one bounded visual slice. | A partial token migration would leave two styling systems and would not establish a reliable default theme. |
| 2026-09-01 | Include three-pane placement in the theme contract. | Future themes should be able to swap side panes or dock them above and below the central work area without rewriting components. |
| 2026-09-01 | Keep semantic DOM order stable and controls placement-neutral. | Visual flexibility must not make landmark meaning, reading order, focus order, labels, or icons misleading. |

## Dependencies

- Feature 003's archived workspace established the current layout and local CSS
  ownership that this feature restyles.
- Features 004, 005, 007, and 008 established the calendar, transaction
  inspector, correction/removal, and notices surfaces included in the
  migration.
