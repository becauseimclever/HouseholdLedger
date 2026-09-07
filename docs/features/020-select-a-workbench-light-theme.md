# Feature 020: Select a Workbench Light Theme

## Status

Status: Complete.

- Planned: 2026-09-05.
- Implemented: 2026-09-06.
- Validated: 2026-09-06.
- Depends on Feature 009's semantic theme-token contract and Feature 016's
  global Settings persistence.

## Outcome

The Settings page includes a global theme selector. A user can switch between
the existing `Workbench Dark` default and a new `Workbench Light` theme
inspired by Visual Studio Code's light workbench, save the choice, and see the
entire application adopt that theme immediately and after refresh or restart.

The inspiration is structural and tonal only. HouseholdLedger does not copy
Visual Studio Code branding, logos, source stylesheets, or exact product
presentation.

## User Flow

1. The user follows Settings in the left navigation.
2. In an Appearance section, the user chooses Workbench Dark or Workbench Light
   from a labeled native dropdown.
3. The user saves the theme setting.
4. The full workspace immediately adopts the authoritative saved theme.
5. The user navigates among Calendar, Accounts, account transactions, Settings,
   and notices and sees one coherent theme.
6. Refreshing or restarting the application restores the saved theme.

## Theme Setting Contract

- The global settings record introduced by Feature 016 gains one supported
  theme identifier. The initial values are `workbench-dark` and
  `workbench-light`.
- Workbench Dark remains the default when no settings row or theme value exists.
- Theme identifiers are stable storage values; visible labels may be refined
  without migrating data.
- The Settings page presents theme selection in a separate Appearance section
  from Global money display. Each setting has a clearly associated control and
  save/status behavior, so saving one does not silently overwrite an unsaved
  change to the other.
- A successful save updates one Client-wide theme state and the root
  `data-theme` attribute without a full browser reload. A failed or obsolete
  save leaves the prior authoritative theme active.
- Every browser and user sees the same persisted theme because authentication
  and per-household preferences do not yet exist.
- During startup, the application uses Workbench Dark until the authoritative
  setting is loaded, then applies the persisted value before exposing the
  interactive workspace. This slice does not add a second browser-local source
  of truth.

## Workbench Light Visual Direction

Workbench Light is a quiet, dense operational workspace informed by familiar
light code-editor chrome while retaining HouseholdLedger's identity:

- Neutral cool-white central work surface, subtly gray chrome and side panels,
  clear one-pixel boundaries, and restrained elevation establish hierarchy.
- Dark neutral primary text and softer secondary text maintain readable
  contrast without a washed-out gray-on-white treatment.
- Restrained blue identifies links, primary actions, current selection, and
  focus. Success, warning, error, and information retain distinct green, amber,
  red, and blue families.
- Inputs and tables remain compact and work-focused. Navigation tiles,
  calendar cells, inspector content, settings controls, statuses, and notices
  use the same semantic hierarchy.
- Corners remain at or below `0.5rem`; there are no gradients, decorative
  glows, background blobs, marketing cards, VS Code logos, or copied branded
  assets.
- The theme changes semantic token values only. It does not change routes,
  content, DOM order, workspace-region placement, typography scale, component
  behavior, or financial meaning.

## Theme Token Contract

- `app.css` defines a complete `[data-theme="workbench-light"]` value set for
  every semantic color token currently supplied by Workbench Dark.
- Component styles continue to consume only semantic `--hl-*` tokens. No
  component branches on theme name or adds light-only raw colors.
- Workbench Light declares `color-scheme: light` and provides matching browser
  chrome where the host can update it safely.
- Both themes support navigation tiles, pane arrow/rail states, calendar,
  inspector, forms, tables, statuses, dialogs, loading UI, error UI, notices,
  and Settings.
- Normal text meets WCAG AA `4.5:1`; large text and meaningful graphical or
  focus boundaries meet `3:1` against adjacent colors.
- Theme changes do not animate every color or cause disruptive transitions;
  reduced-motion preferences remain respected.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Domain/Application | Define supported theme identifiers, default behavior, and update semantics within global settings. |
| Infrastructure | Persist the selected theme in the existing singleton settings model through an explicit migration if the schema requires it. |
| API and contracts | Return and update theme without overwriting unrelated settings; align OpenAPI and validation errors. |
| Client settings | Render the Appearance selector, save independently, publish authoritative theme changes, and expose truthful states. |
| Global theme CSS | Supply a complete accessible Workbench Light semantic token set without component-specific theme branches. |

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Theme selector | Settings contains a labeled Appearance theme dropdown with Workbench Dark and Workbench Light and an explicit save action/status that does not overwrite an unsaved money-setting edit. |
| AC-02: Default and persistence | With no saved theme, Workbench Dark remains active; saving either supported theme restores it after refresh and application restart. |
| AC-03: Immediate global application | A successful save updates the root theme and every visible application surface without a full browser reload; navigation to other routes retains it. |
| AC-04: Cohesive light theme | Workbench Light presents readable light workspace chrome, panels, work areas, navigation tiles, controls, calendar, inspector, tables, statuses, settings, and notices through one semantic token contract. |
| AC-05: Accessible states and contrast | In both themes, text, controls, focus, current selection, validation, disabled states, and semantic statuses meet the documented contrast and non-color requirements. |
| AC-06: Truthful resilient behavior | Loading, unavailable/retry, unchanged, saving, saved, invalid, and obsolete-save states are distinct; unsupported values cannot be persisted or applied. |
| AC-07: Responsive stability | Both themes preserve normal-flow workspace geometry with no incoherent overlap, clipping, abandoned tracks, or page-level horizontal overflow at supported desktop and narrow viewports. |
| AC-08: Theme-only visual change | Switching theme does not change data, monetary formatting, routes, account/menu state, pane state, calendar selection, transaction behavior, or semantic DOM order. |
| AC-09: Bounded slice | No system-theme detection, automatic time-based theme, arbitrary custom themes, theme import/export, per-user preference, typography selector, pane docking, or third-party styling dependency is introduced. |

## Implementation Notes

1. Extend Feature 016's global settings contract atomically rather than adding
   an unrelated persistence mechanism.
2. Keep money and theme saves independent at the UI and update boundary so one
   form cannot overwrite stale values from the other.
3. Apply the selected identifier only after validating it against the explicit
   supported set.
4. Audit every current raw color location; theme values belong in root token
   declarations, not scoped component CSS.
5. Update browser theme color when the root theme changes without embedding
   persisted settings or secrets in static Client configuration.

No new runtime dependency is expected.

## Validation

- Application and API tests cover default theme, supported values, rejection,
  independent money/theme updates, and checked OpenAPI.
- PostgreSQL tests cover migration, persistence, and preservation of the money
  setting when only theme changes.
- Client component tests cover Settings states, independent dirty/save state,
  root attribute updates, failed/obsolete saves, and route continuity.
- Structural tests verify complete semantic token parity and no theme-name
  branches or raw rendered colors in component styles.
- Hosted browser checks switch both directions and inspect representative
  computed styles, keyboard focus, contrast, pane states, navigation tiles,
  tables, forms, statuses, and screenshots at desktop and one narrow viewport.
- Real PostgreSQL 18 integration tests passed the explicit migration, default,
  independent column-update, persistence, and check-constraint contracts. The
  checked OpenAPI matched runtime generation.
- Live HTTPS checks switched both directions without reload, retained the saved
  value across navigation and hard refresh, preserved USD, ignored system-theme
  preference, and stayed within 1440- and 390-pixel viewports. Measured light
  contrast ratios were 15.8:1 for primary text, 7.56:1 for secondary text,
  5.67:1 for primary actions, and 6.31:1 for focus against the work surface.

## Definition of Done

The feature is complete when the global Settings page can durably switch the
application between Workbench Dark and a cohesive accessible Workbench Light,
all surfaces update immediately through semantic tokens, unrelated settings and
behavior remain unchanged, checked OpenAPI is current, and focused persistence,
Client, contrast, responsive, and hosted browser validation pass.

## Deferred Work

System-theme detection, browser-local overrides, per-user or per-household
preferences, additional themes, custom palettes, theme scheduling,
typography/layout selectors, and theme import/export remain separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-05 | Ship selector and second theme in one slice. | A selector with only one choice has no manually testable user value. |
| 2026-09-05 | Keep Workbench Dark as the no-setting default. | This preserves current startup behavior and existing visual expectations. |
| 2026-09-05 | Persist theme globally with other application settings. | The application has no identity boundary and should avoid a second preference store. |
| 2026-09-05 | Use VS Code light only as inspiration. | HouseholdLedger should gain familiar workbench clarity without copying another product's branding or stylesheet. |
| 2026-09-05 | Keep theme and money saves independent. | Editing one setting must not accidentally commit or overwrite stale state in another. |

## Dependencies

- [Feature 009](archive/009-themeable-design-system-and-workbench-dark-theme.md)
  supplies Workbench Dark and the semantic theme-token boundary.
- [Feature 016](016-configure-global-display-currency.md) supplies the global
  Settings route, persistence model, and read/update lifecycle.
- [Feature 018](018-present-navigation-as-workspace-tiles.md) and
  [Feature 019](019-collapse-navigation-from-its-pane.md) define navigation
  surfaces that both production themes must support.
