# Feature 024: Set a Configurable Kakeibo Spending Plan

## Status

Status: Proposed.

- Planned: 2026-09-07.
- Depends on Feature 010's authoritative period expense totals and Features
  016-017's shared money presentation.

## Outcome

A user can set the money available and intended savings for a configurable spending period, then see the exact amount planned for spending and how much remains after that period's recorded expenses.

This is the first complete planning step in the Kakeibo cycle. It turns the existing expense calendar from observation alone into a comparison between a period intention and actual spending.

## User Flow

1. The user opens a period in the calendar.
2. The period shows either a prompt to create its plan or its saved plan summary.
3. The user selects a period type (e.g., calendar month, biweekly, semimonthly, four-week, weekly) and enters the money available for that period and the amount they intend to save.
4. Before saving, the form shows the resulting planned spendable amount.
5. After saving, the period shows period type, money available, intended savings, planned spendable, recorded expenses, and remaining spendable.
6. Creating, correcting, or removing an expense refreshes the recorded and remaining amounts from the backend.
7. The user can revise the same period's plan without changing another period.

## Kakeibo And Calculation Contract

Each spending period has at most one household-level plan with three entered attributes:

- **Period type** is the configurable cadence for the plan. Supported types include calendar month, biweekly (every 14 days), semimonthly (1st–15th and 16th–end), four-week (28 days), and weekly. The period type determines the start and end dates for expense aggregation.
- **Money available** is the non-negative amount the household chooses as the period's planning base. It may represent expected income, carryover, or other funds; this feature does not create income transactions or infer balances.
- **Intended savings** is the non-negative amount reserved before spending. It cannot exceed money available.

The backend calculates all derived values with `decimal` arithmetic:

$$
\text{planned spendable} = \text{money available} - \text{intended savings}
$$

$$
\text{recorded expenses} = \sum \text{valid expenses dated within the period}
$$

$$
\text{remaining spendable} = \text{planned spendable} - \text{recorded expenses}
$$

- Entered values have at most two decimal places and use the same positive maximum supported by transaction amounts. Zero is valid for either value.
- Planned spendable is always non-negative because intended savings cannot exceed money available.
- Remaining spendable may be negative and must be shown honestly as overspent; it is never clamped to zero.
- Recorded expenses include every account and classification exactly once, matching the authoritative all-account period summary.
- Calculations do not round intermediate values, use binary floating point, or derive totals from formatted Client text.
- The configured display currency changes symbols and number culture only. It does not convert plan values or expense totals.

## Period And Persistence Contract

- A plan is identified by period type and period start date, independent of the date on which it is created or revised.
- At most one plan exists for a period. Saving again updates that plan atomically; concurrent saves use last-successful-write wins.
- Periods without a plan remain distinct from periods whose two entered values are both zero.
- Normal application startup does not seed plans or migrate the database.
- The API returns entered and backend-derived values together so every Client uses one calculation contract.
- Reading a period with no plan returns an explicit no-plan result rather than a fabricated zero plan.
- Period type changes create a new plan for the new period definition; the system does not retroactively reassign existing expenses to a different period type.

## Calendar Presentation

- The active period owns one concise Kakeibo plan region associated with the visible period heading.
- The region presents a create or edit form and a summary without obscuring the calendar or requiring a separate account selection.
- Input labels use `Period type`, `Money available`, and `Intended savings`; the interface does not call money available verified income or an account balance.
- The summary distinguishes the entered values from the derived values and identifies a negative remainder as overspent without relying on color alone.
- Navigating periods loads that period's plan and totals. Late responses from a previous period cannot replace the active period.
- Loading, no-plan, saving, saved, validation, and unavailable states are distinct. A failed save preserves the last authoritative plan and the user's unsaved values.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Domain | Define a valid period plan and enforce amount precision, range, intended-savings invariants, and period type rules. |
| Application | Create or revise one period plan and calculate exact planned, recorded, and remaining values. |
| Infrastructure | Persist one plan per period and combine it with authoritative period expenses without duplication. |
| API and contracts | Expose period-plan read and save operations, validation Problem Details, and aligned OpenAPI. |
| Client | Present period-scoped planning, truthful states, exact backend results, and accessible overspending status. |

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Period-scoped plan | The active calendar period can create and revise one durable plan without changing any other period. |
| AC-02: Configurable period types | The user can select from supported period types including calendar month, biweekly, semimonthly, four-week, and weekly, and the plan is scoped to the chosen period definition. |
| AC-03: Exact planning math | The backend returns planned spendable as money available minus intended savings and remaining spendable as planned spendable minus every recorded expense in that period exactly once. |
| AC-04: Valid financial boundaries | Values accept zero and at most two decimal places, reject negatives and unsupported magnitudes, and reject intended savings greater than money available before persistence. |
| AC-05: Honest overspending | Recorded expenses may produce a negative remaining amount, which is returned unchanged and presented as overspent without being hidden or clamped. |
| AC-06: Authoritative refresh | Creating, correcting, or removing a period expense refreshes recorded expenses and remaining spendable from the backend while preserving the saved plan. |
| AC-07: Missing versus zero plan | A period with no plan shows a create prompt and is observably distinct from a saved plan containing zero values. |
| AC-08: Calendar navigation safety | Period navigation loads the matching plan and totals, and obsolete responses cannot replace the active period's state. |
| AC-09: Accessible responsive operation | Inputs, derived preview, validation, save state, summary, and overspending status are labeled, keyboard operable, announced where appropriate, and usable at supported viewports without overlap or horizontal page scrolling. |
| AC-10: Bounded slice | No income transactions, account balances, transfers, recurring plans, category allocations, automatic carryover, forecasting, exchange conversion, charts, or reflection prompts are introduced. |

## Implementation Notes

1. Use a period type and period start date as the validated key rather than an arbitrary date pretending to represent a period.
2. Reuse the authoritative period expense query behind Feature 010; do not sum formatted calendar responses in the Client.
3. Keep entered plan values and derived totals distinct in contracts and UI so future planning or reflection features can extend them without changing their meaning.
4. Store exact decimals and enforce the critical relationship and range rules in both Domain behavior and PostgreSQL constraints.
5. Apply the existing shared money formatter only after calculations are complete.
6. Period type selection is per-plan, not global. Users can have different period types for different periods.

No new runtime dependency is expected.

## Validation

- Domain tests cover zero, two-decimal boundaries, maximum values, savings equal to available money, rejection of negative, over-precision, over-maximum, and savings-greater-than-available inputs, and valid period type values.
- Application tests cover exact formulas, no expenses, multiple same-day and multi-account expenses, period boundaries for each supported period type, negative remainder, missing versus zero plan, and mutation rereads.
- PostgreSQL tests cover migration, one-plan-per-period uniqueness, constraints, insert/update durability, and period isolation.
- API tests cover no-plan reads, create/update, validation Problem Details, derived values, expense mutation refresh, and checked OpenAPI.
- Client component tests cover create/edit, preview, all states, period navigation, stale responses, formatting-only currency changes, accessible overspending text, and period type selection.
- One hosted browser journey creates a plan with a non-monthly period type, records an expense, observes the exact reduced remainder, refreshes, revises the plan, and navigates away and back to verify period isolation and persistence.

## Definition of Done

The feature is complete when a user can durably set one period’s period type, money available, and intended savings, see exact backend-derived planned spending, recorded expenses, and remaining spending in the calendar, revise the plan safely, and observe truthful missing, error, navigation, and overspending states with focused cross-layer validation.

## Deferred Work

Income transactions, recurring income or plans, fixed-expense commitments,
classification-level spending allocations, savings account transfers,
automatic carryover, forecasts, charts, and end-of-period reflection remain
separate outcomes. The strongest next Kakeibo slice after this feature is a
classification-level plan-versus-actual review.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-07 | Start with money available rather than an income transaction model. | It enables honest period planning without prematurely defining income, recurrence, balances, or transfers. |
| 2026-09-07 | Reserve intended savings before calculating spendable money. | This follows the Kakeibo practice of choosing savings intentionally rather than treating savings as whatever remains. |
| 2026-09-07 | Allow a negative remaining amount. | Overspending is meaningful reflection data and must not be hidden by clamping. |
| 2026-09-07 | Keep one household-level plan per spending period. | Current expense summaries are all-account and the product has no household identity or account-budget allocation contract yet. |
| 2026-09-07 | Support configurable period types including calendar month and common pay periods. | Many users live paycheck to paycheck and need planning aligned with their pay cycle rather than calendar months. |

## Dependencies

- [Feature 010](archive/010-calendar-daily-expense-amounts.md) supplies the authoritative all-account period expense aggregation.
- [Feature 012](archive/012-require-an-account-for-every-transaction.md) ensures every recorded expense has valid account ownership.
- [Features 016 and 017](archive/016-configure-global-display-currency.md) supply shared formatting-only money presentation.
