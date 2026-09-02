# Feature 007: Correct or Remove a Selected-Day Transaction

## Status

Status: Planned - implementation has not started.

- Planned: 2026-08-06; reconciled: 2026-09-01.
- Depends on Feature 005's implemented selected-day create and read workflow.

## Outcome

From the selected day's inspector, a user can correct an existing transaction's
amount or classification, or permanently remove a transaction recorded in
error. Every successful operation is followed by a backend reread of that day.

## Scope

- Edit only amount and classification; the ledger date remains fixed.
- Reuse Feature 005's positive USD amount, two-decimal precision, and four
  Kakeibo-inspired classifications.
- Require explicit confirmation before permanent removal.
- Report invalid, missing, failed, cancelled, and successful operations
  truthfully without making the Client list authoritative.
- Ignore obsolete responses after the selected date changes.

Date moves, undo, audit history, notes, merchants, accounts, custom categories,
totals, balances, budgets, imports, recurring entries, authentication, and
multi-user conflict handling remain deferred.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Domain | Enforce replacement amount and classification invariants. |
| Application | Update or remove an identified transaction and return invalid or not-found outcomes. |
| Infrastructure | Persist update and deletion through the existing transaction mapping. |
| API and contracts | Publish additive update/delete operations with Problem Details and current OpenAPI. |
| Client | Start from a returned selected-day record, edit allowed fields, confirm removal, and reread the day. |

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Correct a transaction | Valid replacement amount/classification persists and appears after a backend reread. The date cannot be changed. |
| AC-02: Validation and absence | Invalid values create no change. A missing transaction produces a truthful not-found state and is never recreated implicitly. |
| AC-03: Confirmed removal | Confirmation permanently removes the transaction and rereads the day; cancellation leaves it unchanged. |
| AC-04: Selection safety | Changing the selected date prevents pending or completed responses from appearing in the new date context. |
| AC-05: Accessible recovery | Edit, confirmation, validation, status, focus, and retry behavior are keyboard and assistive-technology understandable. |
| AC-06: Bounded slice | No creation redesign, date move, undo/history, totals, category management, account, or reporting behavior is introduced. |

## Implementation Plan

1. Add Application update/delete use cases and the minimum repository methods,
   reusing Domain invariants.
2. Implement EF operations without unrelated schema changes.
3. Add aligned API contracts, endpoints, Problem Details behavior, and OpenAPI.
4. Add inspector edit and confirmed-removal states with backend rereads and
   obsolete-response protection.
5. Add focused Domain/Application, API, persistence, Client, and one hosted
   browser journey test in proportion to the behavior.

## Definition of Done

The feature is complete when correction and confirmed removal persist through
backend-owned operations, the selected-day inspector reflects authoritative
rereads, invalid and missing cases are truthful, focused tests pass, checked
OpenAPI is current, and the full solution builds.

A separate documentation audit is not required unless requested.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-06 | Keep correction/removal separate from first recording. | It is a distinct user outcome and avoids expanding initial entry. |
| 2026-08-06 | Defer date moves, undo, and audit history. | They require additional workflow and persistence policy. |
| 2026-09-01 | Depend directly on combined Feature 005. | Feature 005 now supplies the transaction model, selected-day list, and persisted first entry. |

## Dependencies

- [Feature 005](005-record-a-transaction-for-selected-day.md) supplies the
  transaction contract and selected-day create/read workflow.
