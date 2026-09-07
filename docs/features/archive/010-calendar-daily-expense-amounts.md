# Feature 010: Calendar Daily Expense Amounts

## Status

Status: Complete.

- Planned: 2026-09-04.
- Completed: 2026-09-04.
- Application aggregation, date-range persistence, API contract and endpoint,
  Client loading and rendering, mutation refresh, stale-response protection,
  and zero-day presentation are implemented.
- Focused Release validation passed 5 Application tests, 4 API integration
  tests, and 17 calendar component tests. The final combined regression filter
  passed 26/26 after the last edits.
- The PostgreSQL harness passed 2/2 and its hosted Firefox journey passed 1/1,
  including calendar amount refresh after create, correction, and removal; the
  harness removed its container and temporary resources.
- Checked OpenAPI semantically matches the Release runtime document, and the
  full Release solution build succeeds.
- Feature 005 supplies persisted selected-day expense transactions.
- This feature summarizes all accounts together. Account-specific filtering and
  per-account calendar breakdowns are deferred.

## Outcome

When a user opens the month calendar, each day with recorded expenses shows its
total USD expense amount. After a transaction is created, corrected, or removed,
the affected month's calendar amounts refresh from backend-owned data.

This is a read-model slice. It makes existing ledger activity visible in the
calendar without turning the Client into the source of financial calculations.

## User Flow

1. The user opens or navigates to a month in the calendar.
2. The calendar loads one backend summary for that month.
3. A day with expenses shows the sum of its persisted transaction amounts.
4. A day without expenses remains visually quiet and does not imply missing or
   unavailable data.
5. Saving, correcting, or removing a transaction refreshes the affected month
   and displays the authoritative new daily amount.

## Summary Contract

- A request identifies one valid calendar year and month.
- The backend returns one deterministic entry for each date in that month.
- `DailyTotal` is the sum of all expense transactions recorded on the date.
- `MonthToDateTotal` is the cumulative sum from the first of the month through
  that date. It may support accessible detail, but the daily total is the
  primary visible calendar value.
- Totals use the existing fixed USD and two-decimal transaction rules.
- The summary includes all accounts. Introducing accounts later must not
  silently change this all-account meaning.
- The Client formats returned values but does not calculate or repair totals.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Application | Query the month date range and calculate daily and month-to-date totals. |
| Infrastructure | Return transactions for a half-open date range in deterministic order. |
| API and contracts | Expose a calendar-ready monthly expense-summary response and aligned OpenAPI. |
| Client | Load the visible month, render daily amounts, refresh after mutations, and handle unavailable or obsolete responses. |

No Domain rule is added because this feature derives a read model from valid
transactions and does not change transaction invariants.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Month summary | Opening or navigating to a month requests that month and displays the correct persisted daily total on every day with expenses. |
| AC-02: Correct aggregation | Multiple transactions on one date are summed exactly once; dates without transactions have a zero summary and do not display a fabricated expense. |
| AC-03: Mutation refresh | Creating, correcting, or removing a selected-day transaction refreshes the affected month's summary from the backend. |
| AC-04: Navigation safety | A late response for a previously displayed month cannot replace the current month's amounts. |
| AC-05: Accessible presentation | A day's amount is included in that date control's accessible description and remains understandable without color, hover, or pointer input. |
| AC-06: Bounded slice | No account management, account filtering, balances, income, budgets, category totals, charts, or Client-side financial calculation is introduced. |

## Implementation Notes

1. Reuse the transaction repository's date-range query rather than issuing one
   request per day.
2. Keep the summary endpoint separate from selected-day transaction detail so
   the calendar does not download full transaction records for a month.
3. Use the existing transaction-change notification as an invalidation signal;
   the backend reread remains authoritative.
4. Preserve current calendar selection, focus, keyboard navigation, and period
   semantics while adding the amount text.

No new runtime dependency is expected.

## Validation

- Application tests cover multiple transactions per day, zero days, month
  boundaries, exact decimal sums, and month-to-date accumulation.
- API tests cover valid month summaries and create/correct/remove followed by a
  summary reread.
- Client component tests cover loading, month navigation, stale responses,
  mutation refresh, and accessible date descriptions.
- One hosted browser journey proves that a persisted transaction changes the
  visible daily calendar amount without breaking date selection.

## Definition of Done

The feature is complete when persisted expenses appear as correct daily USD
amounts in the month calendar, mutation and navigation refreshes remain
backend-authoritative, focused tests pass, checked OpenAPI is current, and the
solution builds.

## Deferred Work

- Account creation and transaction ownership are Features 011 and 012.
- Account filters, per-account breakdowns, balances, income, transfers,
  category summaries, planning, and reports remain outside this feature.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-04 | Show one all-account daily total. | The user chose a calm combined calendar value instead of an account filter or a crowded per-account breakdown. |
| 2026-09-04 | Keep summary calculation in the Application/backend path. | Financial totals must remain consistent across clients and authoritative after mutations. |
| 2026-09-04 | Complete the feature after focused and hosted validation. | Backend totals, quiet zero days, mutation refresh, stale-response safety, OpenAPI alignment, PostgreSQL behavior, and browser rendering are verified. |

## Dependencies

- [Feature 005](005-record-a-transaction-for-selected-day.md) supplies
  persisted selected-day transaction creation and reads.
- [Feature 007](007-correct-or-remove-selected-day-transaction.md)
  supplies correction and removal events that invalidate a summary.
