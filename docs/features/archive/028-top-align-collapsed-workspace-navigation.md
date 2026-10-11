# Feature 028: Top-Align Collapsed Workspace Navigation

## Status

Status: Complete.

- Planned: 2026-09-14.
- Implemented: 2026-10-11.
- Depends on Feature 014's workspace navigation and collapse behavior.
- Blazor Client component tests passed (148 total), covering collapse
  semantics, destination labels, route state, and account disclosure behavior.
- The isolated Firefox browser journey passed (1 passed, 0 failed, 0 skipped).
  The collapsed control and first destination were top-aligned at desktop and
  narrow viewports, with no page-level horizontal overflow.

## Outcome

When a user collapses workspace navigation, the collapse control and destination
icons remain at the top of the navigation rail instead of shifting to its
vertical center.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Top-aligned rail | After collapsing navigation, the collapse control and first available destination icon are positioned at the top of the navigation pane. |
| AC-02: Preserved navigation | Current rail destinations, account disclosure behavior, route-current semantics, accessible names, and toggle state continue to work. |
| AC-03: Responsive accessible layout | Desktop and narrow layouts retain usable keyboard navigation, visible focus, stable touch targets, and no page-level horizontal overflow. |
| AC-04: Bounded slice | No route, API, persistence, dependency, account-data, inspector, or workspace-grid behavior changes. |

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Client | Top-align the collapsed rail while preserving its existing layout dimensions and semantic navigation behavior. |

## Validation

- Client component tests cover collapse semantics, destination labels, route
  state, and account disclosure behavior.
- The browser geometry check verifies the collapsed control and first
  destination are top-aligned at 1440px and 500px viewports, with no
  page-level horizontal overflow.

## Definition of Done

The feature is complete when collapsing workspace navigation leaves its control
and destinations top-aligned without regressing accessible navigation behavior.
