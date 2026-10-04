# Feature 026: Minimize Empty-Day Inspector

## Status

Status: Complete (approved audit refinement).

- Planned: 2026-09-14.
- Approved with intentional income-entry refinement: 2026-10-04.
- Depends on the calendar expense entry and income receipt inspector behavior.

## Outcome

When the selected day has no income or expense records, the inspector presents
the available recording actions without empty income lists. Deliberate income
confirmation stays discoverable even when no schedule exists.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Focused empty day | When authoritative successful reads report no completed income, pending income, or expense transactions, the inspector focuses on expense entry and deliberate income confirmation rather than empty income lists. |
| AC-02: No empty income noise | The focused empty-day state omits empty income lists and no-income messages, while retaining selected-day context and truthful expense states. |
| AC-03: Truthful prerequisites | When an expense cannot be recorded because no account is available, the inspector retains a truthful unavailable or add-account state rather than implying that the entry form can be used. |
| AC-04: Existing-record detail | When any income or expense record exists, the normal inspector detail remains available, including correction and removal controls where supported. |
| AC-05: Bounded slice | Date changes continue to reject stale responses; no API, persistence, or ledger rule changes are introduced. |

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Client | Determine the focused presentation from authoritative inspector data and preserve truthful loading, unavailable, and recorded-data states. |

## Validation

- Client component tests cover empty-day, no-account, loading, unavailable, and
  recorded-income or recorded-expense states.
- Existing date-change tests continue to prove late responses cannot replace the
  active inspector state.
- The 148-test Client suite passed. The isolated Firefox setup journey verified
  absent empty confirmed-income detail and expandable schedule expectations.

## Definition of Done

The feature is complete when an otherwise empty day inspector is focused on
recording actions without hiding errors or information for a day with a record.