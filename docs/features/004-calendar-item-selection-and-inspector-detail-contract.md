# Feature 004: Calendar Item Selection and Inspector Detail Contract

## Status

Status: Draft - awaiting explicit user approval; implementation is not
authorized.

- This is a proposal only. It cannot start implementation until the user
  explicitly approves this document.
- Proposal date: 2026-08-04.
- Depends on the approved, pending implementation workspace shell in
  [Feature 003](003-workspace-navigation-and-ui-foundation.md). It does not
  treat Feature 003 as implemented or validated.

## Context and Outcome

HouseholdLedger is a calendar-centered household ledger informed by Kakeibo, a
household-accounting practice that combines recording, planning, and
reflection. The Feature 003 inspector truthfully says that no calendar item is
selected because selection and detail data have not been defined.

**Proposed outcome:** A future approved slice can establish what a user may
select in the calendar, how that selection works accessibly, and which neutral,
genuine read-model details the inspector may display. No inspector detail is
shown until the selected object and its supporting data contract are approved
and available.

## Goals

1. Define accessible calendar selection semantics, lifecycle, focus behavior,
   clearing behavior, and selection ownership.
2. Define the minimum neutral presentation/read-model information that may be
   shown only when supplied by genuine data.
3. Decide whether selection is transient, URL-addressable, persisted, or
   restored, including privacy and navigation consequences.
4. Identify only the necessary Domain, Application, API, contract, and
   persistence boundaries after product decisions establish what data exists.

## Non-Goals and Explicitly Out of Scope

- Recording, editing, deleting, categorizing, calculating, importing, or
  saving financial entries; Feature 007 is the proposal for first recording.
- Inventing a ledger workflow, financial field set, entities, aggregates,
  persistence schema, API shapes, or sample records.
- Choosing accounts, reports, settings, or navigation destinations.
- Changing the Feature 003 physical shell, pane placement, default pane state,
  or pane controls.
- Assuming a selection must be stored, synchronized, or URL-addressable.

## Kakeibo and Calendar UX Rules

- Selection must strengthen calm calendar review rather than imply that a
  household must record, classify, or act on a date.
- A selected state needs a programmatically determinable name, state, focus
  behavior, and keyboard lifecycle appropriate to the approved calendar
  structure. The eventual implementation must not rely on color alone.
- The inspector shows only facts supported by the approved read model. Until
  data is available, it retains an honest neutral state rather than invented
  values, totals, or actions.
- The calendar remains the primary work area; inspector detail supports review
  and must not replace it with a form or unrelated dashboard.

## Product Decisions and Open Questions

The following require user decisions before implementation planning can become
specific:

1. What is the first selectable calendar object: a day, an existing entry, a
   planning/reflection item, or another user-defined object?
2. What does selection do for pointer, keyboard, touch, assistive technology,
   route changes, browser back/forward, focus movement, and clearing?
3. Which neutral fields are useful and safe to show for that object, and which
   of them exist as genuine data at selection time?
4. Which layer owns selection state, and is it transient, URL-addressable,
   persisted, restored, shareable, or deliberately none of those?
5. What Domain concepts, Application query/use case, API endpoint/contract,
   authorization boundary, and persistence read path are necessary after the
   preceding questions are answered?

## Readiness Criteria

| Criterion | Observable decision or outcome | Completion evidence |
| --- | --- | --- |
| RC-01: Selection object | The user-approved document names one selectable calendar object and its selection lifecycle, including clearing and navigation behavior. | Recorded product decision and UX review. |
| RC-02: Accessible interaction | The selection model specifies keyboard, pointer, touch, focus, and assistive-technology expectations without relying on color alone. | Accessible interaction specification reviewed against the actual calendar structure. |
| RC-03: Honest inspector detail | Every proposed inspector field has a named, genuine source and the empty/unavailable states remain neutral. | Approved read-model field list and source mapping. |
| RC-04: State boundary | The document records whether URL, persistence, restoration, or sharing is desired, including privacy implications. | Explicit product decision; unresolved choices remain out of implementation. |
| RC-05: Contract necessity | Any Domain, Application, API, contract, and persistence work is justified by approved user outcomes, not assumed in advance. | Dependency map approved after RC-01 through RC-04. |

## Proposed Dependency-Ordered Waves

These are design/readiness waves, not implementation authorization.

1. Resolve the selectable object, user value, and accessible selection
   lifecycle.
2. Approve the neutral inspector read model and its empty, unavailable, and
   loading states only when genuine data requirements are known.
3. Decide selection ownership, URL behavior, persistence, restoration, and
   privacy boundaries.
4. Have the responsible architecture specialists identify the smallest
   necessary Domain, Application, API, contract, and persistence work.
5. Create a separately approved implementation specification with scoped
   acceptance criteria and test strategy.

## Definition of Done

This proposal is ready for implementation planning only when the user has
explicitly approved it and each readiness criterion is supported by recorded
product decisions. It is complete only after a later approved implementation
feature has its own audited acceptance evidence. This proposal does not claim
implementation, validation, or completion.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Created as a separate proposal from Feature 003. | Selection and truthful inspector data require product, accessibility, and data-contract decisions beyond the physical workspace shell. |

## Dependencies

- [Feature 003](003-workspace-navigation-and-ui-foundation.md) provides the
  approved shell boundary and neutral inspector empty state.
- [Feature 007](007-calendar-entry-recording.md) must not define its recording
  workflow until this selection/detail proposal is approved.
