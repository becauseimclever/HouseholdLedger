# Feature 005: Workspace Navigation Destinations

## Status

Status: Draft - awaiting explicit user approval; implementation is not
authorized.

- This is a proposal only. It cannot start implementation until the user
  explicitly approves this document.
- Proposal date: 2026-08-04.
- Depends on the approved, pending implementation workspace shell in
  [Feature 003](003-workspace-navigation-and-ui-foundation.md). It does not
  reopen that feature's physical layout or controls.

## Context and Outcome

Feature 003 defines a `Navigation` region heading without destination items so
the workspace does not promise unavailable product areas. HouseholdLedger needs
user-defined outcomes before routes or navigation choices can be meaningful.

**Proposed outcome:** A later approved slice can define the first real
navigation destination or destinations, their user value, routes, active states,
and accessible navigation behavior while preserving the calendar-centered
Kakeibo workspace.

## Goals

1. Establish information architecture from approved household tasks and
   outcomes, not from generic application menus.
2. Define the first destination outcomes, routes, labels, and active-state
   behavior only after the user identifies a need.
3. Define keyboard, focus, landmark, current-page, responsive, and error or
   unavailable-route behavior for approved destinations.
4. Retain the calendar as the primary workspace unless an explicitly approved
   user outcome justifies a different destination.

## Non-Goals and Explicitly Out of Scope

- Choosing or implying accounts, reports, settings, budgets, categories, or
  any other generic destination without a user-defined outcome.
- Altering Feature 003 pane dimensions, default states, normal-flow order,
  toggle controls, or its `Navigation` heading-only shell.
- Calendar selection, inspector data, entry recording, persistence, API
  contracts, or financial product modeling.
- A speculative sitemap, generic settings framework, or routing abstraction
  before real destinations justify it.

## Kakeibo and Calendar UX Rules

- Navigation supports orientation and does not displace the calendar-centered
  review experience with a generic finance dashboard.
- Labels describe approved household tasks in plain, nonjudgmental language.
- A visible active state and programmatic current-page state must agree; color
  or position alone cannot convey the current destination.
- Keyboard and assistive-technology users can discover, enter, and leave each
  approved destination predictably.

## Product Decisions and Open Questions

1. Which first user outcome needs a distinct destination beyond the calendar?
2. Does the calendar remain the only destination until a later feature makes a
   different user outcome real?
3. What route, label, ordering, and active-state behavior express each
   approved destination without creating false product commitments?
4. What happens for unavailable, unauthorized, bookmarked, or invalid routes?
5. Are destination labels and routes stable enough to require user-facing
   documentation or migration guidance?

## Readiness Criteria

| Criterion | Observable decision or outcome | Completion evidence |
| --- | --- | --- |
| RC-01: User-defined destination | Each proposed destination has a specific household user outcome and a reason it cannot remain in the calendar workspace. | Recorded user decision. |
| RC-02: Bounded information architecture | The proposed route and label set contains only approved destinations and does not imply accounts, reports, or settings. | Reviewed route/label inventory. |
| RC-03: Accessible navigation behavior | Active states, landmarks, keyboard movement, focus behavior, and route error states are specified as observable behavior. | UX and accessibility review. |
| RC-04: Calendar-centered continuity | The proposal explains how calendar review remains primary or records an explicit user-approved exception. | Recorded product decision. |

## Proposed Dependency-Ordered Waves

These are design/readiness waves, not implementation authorization.

1. Identify the first user-defined outcome that needs a destination.
2. Approve its route, label, placement, active state, and unavailable-route
   behavior.
3. Specify accessible and responsive navigation behavior against the existing
   Feature 003 shell.
4. Create a separately approved implementation specification with the smallest
   route and navigation changes required.

## Definition of Done

This proposal is ready for implementation planning only after explicit user
approval and recorded resolution of its readiness criteria. It is complete only
when a later approved implementation feature has audited evidence for its own
acceptance criteria. This proposal does not authorize or claim implementation,
validation, or completion.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Created separately from Feature 003. | A navigation shell label does not establish a need for, or a choice among, product destinations. |

## Dependencies

- [Feature 003](003-workspace-navigation-and-ui-foundation.md) owns the
  physical shell and remains unchanged by this proposal.
