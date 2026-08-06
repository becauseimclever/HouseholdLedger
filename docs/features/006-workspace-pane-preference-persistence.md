# Feature 006: Workspace Pane Preference Persistence

## Status

Status: Draft - awaiting explicit user approval; implementation is not
authorized.

- This is a proposal only. It cannot start implementation until the user
  explicitly approves this document.
- Proposal date: 2026-08-04.
- Depends on the approved, pending implementation workspace shell in
  [Feature 003](003-workspace-navigation-and-ui-foundation.md), which defines
  transient desktop and narrow-screen pane defaults only.

## Context and Outcome

Feature 003 makes pane state responsive for the current session. Whether a
household benefits from remembering navigation and inspector visibility is a
separate product, privacy, accessibility, and migration decision.

**Proposed outcome:** A later approved slice can decide whether pane preference
persistence is useful and appropriate. If approved, it can define a respectful,
understandable persistence lifecycle with a fallback and reset path. No storage
mechanism is selected by this proposal.

## Goals

1. Decide whether pane state should persist at all, and for which contexts.
2. Define the intended scope: current session, browser/device, user identity,
   or another explicitly approved boundary.
3. Evaluate privacy, shared-device, retention, clearing, reset, unavailable
   storage, and version/state-migration behavior.
4. Define accessible restoration and reset behavior so a stored state does not
   surprise or trap keyboard and assistive-technology users.

## Non-Goals and Explicitly Out of Scope

- Assuming persistence is desired or choosing browser storage, cookies,
  server-side storage, synchronization, or any other mechanism before a
  product decision.
- Changing Feature 003's initial desktop-expanded and narrow-collapsed defaults
  for users without a valid approved preference.
- Cross-device synchronization, accounts, authentication, analytics, or
  profile management.
- Calendar selection, inspector details, navigation destinations, entry
  recording, or financial data persistence.

## Kakeibo and Calendar UX Rules

- Remembered pane state may reduce repetition, but it must not hide the
  calendar or make a household feel required to configure the workspace.
- The default and reset experience remains calm, predictable, and
  nonjudgmental.
- Any stored state is limited to what is necessary for the approved outcome and
  is explained clearly enough for a shared-device user to understand.

## Product Decisions and Open Questions

1. Is persistence desired, and which pane states, breakpoints, and contexts
   should it cover?
2. Is the preference scoped to a session, device/browser, authenticated user,
   or not stored at all?
3. What privacy risk exists on shared devices, and what disclosure, consent,
   retention, clearing, or opt-out behavior is appropriate?
4. How does the experience behave when storage is unavailable, blocked,
   corrupted, stale, or from an earlier state version?
5. Where can users reset the preference, and how do restored states preserve
   focus, landmarks, keyboard reachability, and narrow-screen usability?
6. If a mechanism is later approved, what state versioning and migration rules
   are required?

## Readiness Criteria

| Criterion | Observable decision or outcome | Completion evidence |
| --- | --- | --- |
| RC-01: Persistence decision | The user explicitly chooses persistence or no persistence, with its intended user benefit. | Recorded product decision. |
| RC-02: Scope and privacy | The proposed scope, shared-device implications, disclosure, clearing, and retention behavior are defined. | Privacy review and approved behavior. |
| RC-03: Fallback and reset | Users have defined behavior for unavailable or invalid state and a usable reset path. | UX/accessibility review. |
| RC-04: State evolution | Any approved stored state has versioning and migration or discard rules. | Approved state-lifecycle decision. |
| RC-05: Mechanism justification | A storage mechanism is selected only after RC-01 through RC-04 prove it meets the approved need. | Architecture decision linked to requirements. |

## Proposed Dependency-Ordered Waves

These are design/readiness waves, not implementation authorization.

1. Decide whether remembering pane state serves a real household outcome.
2. Define scope, privacy, reset, accessibility, fallback, and state-evolution
   requirements.
3. Evaluate storage mechanisms against those requirements using platform-
   supported options where appropriate.
4. Create a separately approved implementation specification only if
   persistence remains justified.

## Definition of Done

This proposal is ready for implementation planning only after user approval and
the recorded decisions satisfy every readiness criterion. It is complete only
when a later approved implementation feature has audited acceptance evidence.
This proposal does not authorize or claim implementation, validation, or
completion.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Created separately from Feature 003. | Transient responsive defaults do not establish that persistent user preferences are wanted or safe. |

## Dependencies

- [Feature 003](003-workspace-navigation-and-ui-foundation.md) defines only
  the baseline transient pane behavior to which any future reset falls back.
