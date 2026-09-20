# Feature 028: Top-Align Collapsed Workspace Navigation

## Status

Status: Proposed.

- Planned: 2026-09-14.
- Depends on Feature 014's workspace navigation and collapse behavior.

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
- A focused browser geometry check verifies the collapsed rail is top-aligned
  at the supported desktop viewport; responsive checks preserve narrow layout
  behavior and no horizontal overflow.

## Definition of Done

The feature is complete when collapsing workspace navigation leaves its control
and destinations top-aligned without regressing accessible navigation behavior.