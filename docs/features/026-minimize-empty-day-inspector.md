# Feature 026: Minimize Empty-Day Inspector

## Status

Status: Proposed.

- Planned: 2026-09-14.
- Depends on the calendar expense entry and income receipt inspector behavior.

## Outcome

When the selected day has no income or expense records, the inspector presents
only the available path for recording a new expense.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Focused empty day | When authoritative successful reads report no completed income, pending income, or expense transactions, the inspector shows only the available new-expense entry path. |
| AC-02: No empty-state noise | The focused empty-day state omits income sections, no-income messages, no-transaction messages, selected-day metadata, and transaction-list controls. |
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

## Definition of Done

The feature is complete when an otherwise empty day inspector is focused on
expense entry without hiding information for a day that has a record.