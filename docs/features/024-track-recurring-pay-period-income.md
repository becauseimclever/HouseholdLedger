# Feature 024: Track Recurring Pay-Period Income

## Status

Status: Proposed.

- Planned: 2026-09-12.
- Depends on Features 010-012's calendar expense record and authoritative
  account catalog, and Features 016-017's shared money presentation.

## Outcome

A user can configure a recurring pay period with its pay date, net income, and
automatic allocations across multiple household accounts. Each occurrence is a
durable income receipt that appears in the calendar on its pay date with its
account allocations.

This starts the receive step of the Kakeibo cycle. It records when money enters
the household and where it is intended to be held before a later feature plans
or reflects on spending.

## User Flow

1. The user opens the calendar and opens pay-period tracking.
2. The user creates a pay schedule by entering a name, a first pay date, a
   recurrence cadence, and the net income for each pay period.
3. The user adds one or more account allocations whose total equals the net
   income, then saves the schedule.
4. The system creates the first income receipt and its allocations and shows
   the receipt on the calendar's pay date.
5. On later pay dates, the system creates each due receipt once from the saved
   schedule and shows it in the matching calendar period.
6. The user can revise future pay periods without altering durable past
   receipts, or pause a schedule to stop future receipt creation.

## Pay Period And Income Contract

Each pay schedule has a user-visible name, first pay date, recurrence cadence,
net income amount, and one or more account allocations.

- **Recurrence cadence** is one of weekly, biweekly (every 14 days),
  semimonthly (two specified days of each month), four-week (every 28 days),
  or monthly (one specified day of each month). A cadence defines future pay
  dates from the first pay date; it is not inferred from expenses or calendar
  navigation.
- **Net income** is a strictly positive amount received for each occurrence.
  It is not an account balance, a transfer, or a forecast.
- An **account allocation** credits one existing account with a strictly
  positive amount from the receipt. A receipt may allocate to multiple
  accounts, but may list an account only once.
- The allocation total must equal the net income exactly, using `decimal`
  arithmetic. The backend rejects an under- or over-allocation rather than
  inventing a remainder.

$$
\text{receipt net income} = \sum \text{receipt account allocations}
$$

- An occurrence is identified by its pay schedule and pay date. The system
  creates at most one receipt for that key, including when a due-period check
  is retried or runs concurrently.
- Generated receipts preserve the schedule values effective when they were
  created. Revising a schedule changes only future, not already materialized,
  receipts.
- Each receipt has a stable backend-generated identifier and preserves its pay
  date, net income, and allocation snapshot. A later bank-import feature must
  match an imported deposit to this receipt and does not recalculate or rewrite
  its historical values.
- A paused schedule produces no new receipts. Resuming uses its unchanged
  cadence from the next eligible pay date and does not silently backfill missed
  dates.
- All entered money values have at most two decimal places and use the same
  positive maximum supported by transaction amounts. Calculations never use
  binary floating point or formatted Client text.

## Calendar Presentation

- Pay-period tracking is reachable from the calendar and does not replace the
  date-centered expense record.
- A calendar day with an income receipt identifies the receipt separately from
  expenses, including its schedule name and net income. The selected-day
  inspector lists each account allocation with its account name and amount.
- The pay-period surface shows the next scheduled pay date, cadence, net
  income, allocations, and whether the schedule is active or paused.
- The create and edit forms present an account-allocation list with an exact
  allocation-total preview. Save is unavailable until the total equals net
  income, and backend validation remains authoritative.
- Calendar navigation retrieves the receipts and allocations for the visible
  date range. Late responses for a previous range cannot replace the active
  calendar state.
- Loading, empty, saving, saved, validation, unavailable, and paused states
  are distinct. Failed saves preserve the user's unsaved entries and the last
  authoritative schedule.

## Persistence And Lifecycle Contract

- A pay schedule and its account allocations are durable records. Accounts
  referenced by allocations use restrictive foreign keys.
- Income receipts and their allocations are durable historical records. The
  schedule, receipt, and allocation relationships are protected by database
  uniqueness and check constraints that mirror the critical Domain invariants.
- Receipt creation is an explicit application operation. Normal application
  startup does not create receipts, migrate, seed, or mutate a database.
- The calendar read returns receipt and allocation data from the backend; the
  Client does not synthesize occurrences from a schedule.
- This feature records intended destinations for received income but does not
  establish account balances, reconciliation, bank synchronization, or money
  transfers between accounts.
- Bank-import deposit matching is a later reconciliation outcome. It will keep
  the imported deposit's separate source identity distinct from the receipt identifier,
  allow at most one confirmed match per receipt and deposit, and preserve both
  records when a proposed match is corrected or removed.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Domain | Define valid recurrence rules, schedules, income receipts, and exact allocation invariants. |
| Application | Create, revise, pause, resume, and materialize receipts idempotently; return calendar-facing income data. |
| Infrastructure | Persist schedules, receipts, and allocations with exact decimals, constraints, unique occurrence keys, and restrictive account references. |
| API and contracts | Expose schedule management, due-receipt materialization, calendar income reads, validation Problem Details, and aligned OpenAPI. |
| Client | Present schedule and allocation editing, truthful states, and date-centered income receipts alongside expenses. |

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Recurring pay schedule | A user can create a named schedule with a first pay date and weekly, biweekly, semimonthly, four-week, or monthly cadence. |
| AC-02: Exact income allocation | A schedule accepts one or more unique-account allocations only when their exact decimal sum equals its positive net income. |
| AC-03: Durable receipt per pay date | Materializing a due pay date creates one durable income receipt and its allocations; retries and concurrent requests never duplicate it. |
| AC-04: Calendar-visible income | The calendar and selected-day inspector show income receipts on their pay dates, distinguish them from expenses, and identify each credited account and amount. |
| AC-05: Future-only revision | Revising a schedule changes its future receipts while past materialized receipts retain the amount and allocations received on their pay dates. |
| AC-06: Paused schedule | Pausing prevents new receipts and resuming does not fabricate historical receipts for skipped dates. |
| AC-07: Account integrity | Missing, duplicate, or nonexistent allocation accounts, nonpositive amounts, precision violations, and allocation totals that differ from income produce no persisted change and return truthful validation. |
| AC-08: Calendar safety | Navigating calendar ranges loads matching income data, and obsolete responses cannot replace the active range. |
| AC-09: Accessible responsive operation | Schedule fields, allocation controls, total preview, receipt details, and all states are labeled, keyboard operable, announced where appropriate, and usable at supported viewports without overlap or horizontal page scrolling. |
| AC-10: Bounded slice | No account balance, transfer, income tax calculation, irregular or one-off income, automatic backfill, bank synchronization, recurring expense, spending plan, category allocation, forecasting, or reflection is introduced. |

## Implementation Notes

1. Model a pay schedule separately from its materialized income receipts; do
   not recalculate historical receipts from a later revision.
2. Use the repository's existing account identifiers and calendar date types;
   never accept free-text account names as an allocation destination.
3. Materialize a receipt through an idempotent application operation backed by
   a database unique key for schedule and pay date.
4. Reuse calendar date-range reads, but extend their contracts so income and
   expense records remain explicit, separate record types.
5. Keep the receipt identifier and historical snapshot available to a future
  imported-deposit matching feature; do not encode an external bank source
  identifier in the schedule or allocation identity.
6. Use the shared money formatter only after exact calculations are complete.

No new runtime dependency is expected.

## Validation

- Domain tests cover all recurrence cadences, valid pay dates, positive and
  two-decimal money boundaries, duplicate accounts, and exact allocation-sum
  validation.
- Application tests cover first and later occurrences, schedule revisions,
  pause/resume behavior, no automatic backfill, receipt snapshots, and
  idempotent/concurrent materialization.
- PostgreSQL tests cover migrations, exact-decimal and allocation constraints,
  account foreign keys, schedule/pay-date uniqueness, receipt durability, and
  isolation between schedules.
- API tests cover schedule CRUD operations, pause/resume, materialization,
  calendar reads, validation Problem Details, and checked OpenAPI.
- Client component tests cover schedule editing, allocation total previews,
  states, receipt and allocation presentation, calendar navigation, and stale
  responses.
- One hosted browser journey creates a biweekly schedule split between two
  accounts, materializes its first receipt, verifies the calendar and inspector
  details, revises a future allocation, and confirms the original receipt is
  unchanged.

## Definition of Done

The feature is complete when a user can durably configure a recurring pay
period, divide each receipt exactly among existing accounts, materialize each
pay date once, see the income and allocations in the calendar, safely revise
future periods or pause the schedule, and observe truthful validation and
loading states with focused cross-layer validation.

## Deferred Work

Account balances, transfers, bank synchronization, imported-deposit matching,
income-tax treatment, irregular income, recurring expenses, spending plans,
category allocations, automatic carryover, forecasting, charts, and
end-of-period reflection remain separate outcomes. A later Kakeibo planning
feature can use these durable income receipts as an honest source of money
available.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-12 | Track recurring pay periods rather than impose a standard household cadence. | Pay schedules vary; users need a cadence that follows their actual paycheck. |
| 2026-09-12 | Materialize each pay date as an income receipt. | Calendar review requires a durable dated record rather than a calculated projection. |
| 2026-09-12 | Require each receipt to allocate its full net income across accounts. | The receipt should explain exactly where the received money is intended to be held without an implicit remainder. |
| 2026-09-12 | Preserve past receipts on schedule revision. | Historical calendar records must remain honest after a paycheck or allocation changes. |
| 2026-09-12 | Reserve receipts as the future bank-deposit matching anchor. | Imported deposits should reconcile to durable dated income records without mutating the original schedule or receipt snapshot. |

## Dependencies

- [Feature 010](archive/010-calendar-daily-expense-amounts.md) supplies the
  calendar date-range and refresh foundations.
- [Feature 012](archive/012-require-an-account-for-every-transaction.md)
  supplies the authoritative account catalog and account identifiers.
- [Features 016 and 017](archive/016-configure-global-display-currency.md)
  supply shared formatting-only money presentation.