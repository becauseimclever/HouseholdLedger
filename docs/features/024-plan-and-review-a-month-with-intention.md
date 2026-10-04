# Feature 024: Plan and Review a Month with Intention

## Status

Status: Complete.

- Planned: 2026-09-12.
- Implemented: 2026-10-04.
- Solution build passed with zero warnings and errors. Domain (32), Application
  (29), API integration (45), API contract (9), and focused Blazor component
  (52) tests passed.
- PostgreSQL integration tests passed against an isolated PostgreSQL 18
  database (8 tests), including monthly review through repositories sharing
  one scoped database context.
- A real Firefox fresh-setup journey passed against disposable PostgreSQL 18
  (1 passed, 0 failed, 0 skipped), covering account and schedule setup,
  expectation-versus-receipt separation, explicit income confirmation,
  reconciled monthly planning, and an expense reflected in calendar, account
  history, and monthly review after reload.
- Builds on the calendar expense record, the four Kakeibo-inspired expense
  classifications, authoritative account catalog, and shared money
  presentation.

## Outcome

Before or during a month, a user can deliberately decide how expected
take-home income will be divided between intended savings and spending across
Necessities, Optional, Culture, and Unexpected. During and after the month,
they can compare confirmed income and recorded expenses with that plan.

The plan expresses intention; it is not an account balance, an automatic
transfer, or a forecast presented as fact. Pay schedules may help the user
anticipate income, but only a user-confirmed receipt counts as actual income.

## User Flow

1. The user opens a month from the calendar and creates or reviews its plan.
2. The user enters expected take-home income, an intended savings amount, and
   planned amounts for each of the four expense classifications.
3. The application shows the remaining planned amount and saves only when the
   plan reconciles exactly.
4. During the month, the user confirms each income receipt when it is actually
   received. A pay schedule may remind or suggest an expected date, but it
   never creates a receipt automatically.
5. The user continues recording expenses on their actual dates using the
   existing classifications.
6. The monthly review compares planned amounts with confirmed income and
   recorded expenses, making differences visible without silently changing
   the plan or ledger.

## Monthly Plan Contract

A plan belongs to one calendar month and contains expected take-home income,
an intended savings amount, and one planned amount for each existing expense
classification: Necessities, Optional, Culture, and Unexpected.

$$
\text{expected income}
= \text{intended savings}
+ \sum \text{planned classification amounts}
$$

- All amounts are nonnegative, use exact `decimal` arithmetic, have at most
  two decimal places, and do not exceed the existing supported money maximum.
- The four classifications reuse the existing expense vocabulary; this
  feature does not add, rename, or reinterpret classifications.
- The plan must reconcile exactly before it can be saved. The interface shows
  the remaining amount as the user edits it; the backend is authoritative.
- One plan exists per calendar month. The user may revise it deliberately.
  A revision changes the plan used for review but never changes recorded
  receipts or expenses. The review identifies the latest plan revision date;
  preserving a complete plan-revision history is outside this slice.
- A pay schedule is an optional expectation aid only. Its dates and amounts
  are not actual income and do not alter the monthly plan unless the user
  chooses to edit and save that plan.

## Actuals and Review Contract

- Actual income is the sum of durable receipts the user confirms after money
  is received. A receipt preserves its actual date and amount; a schedule
  cannot generate receipts, including for past or missed dates.
- When confirming a receipt, the user records where the received money is
  held using existing account identifiers. One or more positive allocations
  must total exactly to the receipt amount. These account destinations are
  ledger context, not Kakeibo spending categories or savings contributions.
- Actual expenses are derived from existing persisted expense transactions,
  grouped by their recorded date and classification. The Client does not
  synthesize or repair actual totals.
- The monthly review shows expected income and intended savings alongside
  confirmed income, planned and actual spending for each classification, and
  each spending variance. Spending variance is actual minus planned, so a
  positive amount means spending exceeded the plan. Income variance is
  confirmed actual income minus expected income. The review identifies the
  latest plan revision date, if any.
- Any actual income remaining after recorded expenses is labeled as an
  **unallocated remainder**. It is not labeled as savings: this feature does
  not infer savings from account allocations, balances, or unspent money.
- Review and calendar navigation use authoritative backend data. A late
  response for a prior month cannot replace the active month.
- Missing plans, no recorded activity, unavailable data, and validation
  failures are distinct states. Failed saves preserve unsaved user input.

## Explicit Boundaries

- No receipt is generated merely because a scheduled date is due. The user
  confirms actual receipt date and amount.
- Creating or resuming a schedule never backfills income. Past income is
  entered only when the user explicitly records and confirms it.
- Account allocation describes where received money is held; it does not
  replace an intentional monthly plan.
- No account balances, reconciliation, transfers, bank synchronization,
  recurring expenses, automatic carryover, forecasting, or implied savings
  are introduced.
- This slice does not add a separate savings-deposit ledger. The review
  compares the intended savings target with the rest of the monthly plan, but
  does not claim to know how much was actually saved.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Domain | Validate monthly plan reconciliation and the actual income receipt invariants. |
| Application | Save and revise monthly plans; record user-confirmed income; query monthly actuals and calculate exact variances. |
| Infrastructure | Persist plans and confirmed receipts; query income and expenses by date range; enforce keys, money constraints, and account references. |
| API and contracts | Expose plan, receipt, and monthly review operations with truthful validation and aligned OpenAPI. |
| Client | Support deliberate plan entry, explicit receipt confirmation, calendar context, review, and accessible states. |

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Monthly intention | A user can create one plan per month with expected income, intended savings, and amounts for all four existing classifications. |
| AC-02: Exact reconciliation | A plan is saved only when expected income equals intended savings plus all four planned amounts exactly. |
| AC-03: Deliberate revision | A user can revise a plan; the review shows the latest revision date, and no actual receipt or expense changes. |
| AC-04: Confirmed income | A user can record actual income with its actual date, amount, and one or more existing-account allocations totaling exactly to the receipt amount. |
| AC-05: No synthetic receipts | Scheduled dates can prompt the user, but do not create receipts. Creating or resuming a schedule does not backfill past income. |
| AC-06: Category comparison | The monthly review shows planned and persisted actual expense totals by classification and the exact variance for each. |
| AC-07: Income comparison | The monthly review distinguishes expected income from confirmed actual receipts and does not count schedule expectations as income received. |
| AC-08: Honest savings language | The review distinguishes intended savings from unallocated remainder and never infers actual savings from account destinations or balances. |
| AC-09: Calendar integrity | Confirmed income and expenses remain distinct dated records; month navigation displays authoritative data and ignores obsolete responses. |
| AC-10: Accessible operation | Plan fields, reconciliation feedback, receipt confirmation, review totals, and states are labeled, keyboard operable, and usable at supported viewports. |

## Validation

- Domain tests cover valid and invalid plan totals, nonnegative amounts,
  decimal precision and maximums, and confirmed receipt invariants.
- Application tests cover create/revise plan, exact monthly aggregation,
  receipt confirmation, and the rule that schedule operations never create or
  backfill receipts.
- PostgreSQL tests cover plan uniqueness per month, exact money constraints,
  account references, durable confirmed receipts, and date-range queries.
- API tests cover plan and receipt operations, monthly review responses,
  validation Problem Details, and checked OpenAPI.
- Client component tests cover plan entry and reconciliation, explicit receipt
  confirmation, review comparisons, revised-plan labeling, and stale
  responses.
- One hosted browser journey creates a reconciled monthly plan, confirms
  income from a scheduled date, records expenses in multiple classifications,
  and verifies that the review compares persisted actuals without treating
  schedule expectations or unspent remainder as actual savings.

## Definition of Done

The feature is complete when a user can intentionally reconcile expected
income, savings intent, and the four spending classifications for a month;
confirm income only after it is received; and review the saved plan against
authoritative actual income and expenses with clear, honest labels.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-10-03 | Make the monthly intention and review the primary outcome; keep pay schedules as optional expectation aids. | Recording income alone does not help a user decide how to use it or learn from actual spending. |
| 2026-10-03 | Require users to confirm actual income; never materialize scheduled or historical receipts automatically. | Automation must not turn an expectation into a false ledger record or obscure what actually happened. |
| 2026-10-03 | Keep account destinations distinct from Kakeibo savings and spending intent. | Where money is held does not establish what the user intends to save or spend. |
| 2026-10-03 | Label the post-expense remainder without calling it savings. | The application cannot claim money was saved without an explicit savings record. |

## Dependencies

- [Feature 010](archive/010-calendar-daily-expense-amounts.md) supplies the
  calendar date-range and refresh foundations.
- [Feature 012](archive/012-require-an-account-for-every-transaction.md)
  supplies the authoritative account catalog and account identifiers.
- [Features 016 and 017](archive/016-configure-global-display-currency.md)
  supply shared formatting-only money presentation.
