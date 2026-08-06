# Feature 007: Calendar Entry Recording

## Status

Status: Draft - awaiting explicit user approval; implementation is not
authorized.

- This is a proposal only. It cannot start implementation until the user
  explicitly approves this document.
- Proposal date: 2026-08-04.
- Depends on user-approved calendar selection/detail and genuine data-contract
  decisions in [Feature 004](004-calendar-item-selection-and-inspector-detail-contract.md).
  Feature 004 is currently only a proposal, so this document does not authorize
  recording design or implementation.

## Context and Outcome

Kakeibo is a household-accounting practice that combines recording, planning,
and reflection. A first recording workflow must earn its place by helping a
household record an approved kind of calendar information in a clear, calm way.
It cannot be inferred from a temporary calendar or an empty inspector.

**Proposed outcome:** After selection/detail and data-contract decisions are
approved, a later slice can define one first Kakeibo-informed calendar entry
workflow. This proposal does not select the user workflow, field set,
categorization, financial calculations, persistence schema, or API shape.

## Goals

1. Identify the first household recording outcome that is valuable enough to
   support in the calendar-centered workspace.
2. Define the user journey only after the selected calendar object, detail
   presentation, and genuine data boundary are approved.
3. Preserve Kakeibo's reflective, nonjudgmental character without assuming a
   specific category system, amount model, calculation, or financial workflow.
4. Establish the design prerequisites needed for a later lean implementation
   specification.

## Non-Goals and Explicitly Out of Scope

- Preselecting entry entities, fields, categories, accounts, currencies,
  amounts, calculations, budgets, balances, rules, imports, attachments, or
  persistence schema.
- Choosing API endpoints, Application use cases, Domain models, validation
  rules, authorization, or migrations before the user workflow requires them.
- Implementing calendar selection, inspector detail, navigation destinations,
  or pane preference persistence.
- Claiming that Kakeibo requires one recording habit, household structure, or
  financial approach.

## Kakeibo and Calendar UX Rules

- The first workflow begins with a user-defined household need, not a generic
  accounting form.
- Language invites clear recording and later reflection without judgment,
  urgency, or assumptions about income, debt, spending, or family structure.
- Calendar context remains visible and understandable throughout the approved
  workflow; the feature must not turn the workspace into a disconnected data
  entry screen without explicit approval.
- Required information, optional reflection, validation, saving, cancellation,
  error recovery, and accessibility are defined only after the actual workflow
  and data are approved.

## Product Decisions and Open Questions

1. What first household need should recording solve, and for whom?
2. What calendar object or context starts the workflow, and how does it relate
   to Feature 004 selection and inspector detail?
3. Which information must the household provide, which information is optional,
   and which terminology is clear and nonjudgmental?
4. Is categorization useful in the first workflow? If so, what user-defined
   language and behavior are needed? If not, it remains out of scope.
5. Are any financial calculations needed for the initial outcome? If so, which
   are observable and how are they explained? If not, none are assumed.
6. What validation, saving, cancellation, recovery, privacy, and accessibility
   behavior is necessary for the approved journey?
7. Which genuine Domain, Application, API, contract, and persistence changes
   follow from those decisions, and what is the smallest valuable vertical
   slice?

## Readiness Criteria

| Criterion | Observable decision or outcome | Completion evidence |
| --- | --- | --- |
| RC-01: User outcome | The user defines one first household recording outcome and its expected result. | Recorded user decision. |
| RC-02: Selection/detail prerequisite | The selected calendar context and its truthful detail contract are approved through Feature 004 or a superseding approved feature. | Approved dependency evidence. |
| RC-03: Honest workflow | Required and optional information, terminology, validation, save/cancel, and recovery behavior are defined without invented financial assumptions. | Reviewed user journey. |
| RC-04: Kakeibo alignment | The workflow supports calm recording and later reflection without judgment or mandatory categorization/calculation. | Product and UX review. |
| RC-05: Minimal contracts | Necessary domain, application, API, persistence, security, and test work is derived from the approved journey. | Dependency map and implementation proposal. |

## Proposed Dependency-Ordered Waves

These are design/readiness waves, not implementation authorization.

1. Obtain approval for Feature 004's selection/detail and data-contract
   decisions.
2. Identify and approve one first household recording outcome.
3. Define the end-to-end user journey, information needs, privacy,
   accessibility, validation, cancellation, and recovery behavior.
4. Derive the smallest necessary domain, application, API, persistence, and
   test responsibilities from that journey.
5. Produce a separately approved lean implementation feature with observable
   acceptance criteria and a proportionate test strategy.

## Definition of Done

This proposal is ready for implementation planning only after user approval,
Feature 004's approved prerequisite, and evidence for every readiness
criterion. It is complete only after a later approved implementation feature
has audited acceptance evidence. This proposal does not authorize or claim
implementation, validation, or completion.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Created as a follow-up proposal after selection/detail. | A truthful recording workflow depends on a chosen calendar context and data contract; preselecting financial models would create false scope. |

## Dependencies

- [Feature 004](004-calendar-item-selection-and-inspector-detail-contract.md)
  is the required selection/detail and data-contract predecessor.
- [Feature 003](003-workspace-navigation-and-ui-foundation.md) remains the
  physical workspace shell and does not provide recording behavior.
