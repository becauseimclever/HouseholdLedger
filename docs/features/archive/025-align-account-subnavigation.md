# Feature 025: Align Account Subnavigation

## Status

Status: Complete.

- Planned: 2026-09-14.
- Implemented: 2026-10-11.
- Depends on Feature 014's workspace navigation and account catalog routes.
- Blazor Client component tests passed (148 total), covering the existing
  navigation, route-current, disclosure, loading, error, and empty-state
  behavior.
- The isolated Firefox browser journey passed (1 passed, 0 failed, 0 skipped).
  At desktop and narrow viewports, expanded account links aligned with the
  Accounts destination within one CSS pixel without page-level horizontal
  overflow.

## Outcome

When a user expands Accounts in the workspace navigation, each account link
aligns with the Accounts destination rather than receiving an additional nested
left indent.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Aligned account links | Expanded account links have their outer left edge aligned with the Accounts destination. |
| AC-02: Preserved navigation | Account links remain distinct targets with current-route semantics, disclosure behavior, loading, error, and empty states. |
| AC-03: Accessible layout | Keyboard navigation, focus visibility, rail behavior, and supported viewport layouts remain usable without horizontal overflow. |
| AC-04: Bounded slice | No API, persistence, routing, or dependency change is introduced. |

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Client | Adjust account subnavigation layout while preserving existing semantic navigation behavior. |

## Validation

- Client component tests preserve route, disclosure, loading, error, and
  empty-state behavior.
- The browser geometry check verifies alignment and no horizontal overflow at
  1440px and 500px viewports.

## Definition of Done

The feature is complete when expanded account links align with Accounts and the
existing accessible workspace navigation behavior remains intact.
