# Feature 016: Configure the Global Display Currency

## Status

Status: Complete.

- Planned: 2026-09-05.
- Implemented: 2026-09-06.
- Validated: 2026-09-06.
- Clarified: 2026-09-07 as a user-selected display culture and indicator that
  never converts ledger values; saving requires explicit acknowledgement.
- Depends on existing USD-denominated transaction amounts and all current money
  presentation surfaces.

## Outcome

A user can follow Settings in the left navigation, choose a supported display
currency, and save it as one global application setting. Every monetary value
in the application immediately uses the selected currency format and continues
to do so after refresh or restart.

This is display metadata only. Changing the currency never converts, rounds, or
rewrites stored transaction amounts or calculated totals.

## User Flow

1. The user follows `Settings` in the workspace navigation.
2. The `/settings` page loads the authoritative global money setting.
3. The user chooses a named currency from a native dropdown.
4. The page explains that values will not be converted, and the user
  acknowledges that display-only behavior before saving.
5. The page confirms the persisted choice.
6. Calendar totals, selected-day transactions, account history, forms, and
   confirmation text immediately render monetary values in that currency.
7. Refreshing or restarting the application retains the saved choice.

## Initial Currency Contract

The initial named choices are deliberately finite and explicit:

| Code | Display name | Formatting culture |
| --- | --- | --- |
| USD | US Dollar | `en-US` |
| CAD | Canadian Dollar | `en-CA` |
| EUR | Euro | `de-DE` |
| GBP | Pound Sterling | `en-GB` |
| AUD | Australian Dollar | `en-AU` |

- USD is the default when no setting row exists.
- The persisted value is the stable ISO 4217 code, not a culture name or
  localized display label.
- The Client uses the documented culture only to format symbols, grouping,
  decimal separators, and the currency's normal decimal precision.
- No exchange-rate lookup or conversion occurs. A stored amount of `82.45`
  displays as `$82.45` under USD and `82,45 €` under the specified EUR culture;
  its numeric ledger value remains `82.45`.
- Unsupported values are rejected by the backend and do not change the saved
  setting.

## Settings and Persistence Contract

- `Settings` is a native primary navigation link to `/settings` and exposes
  current-route state consistently with Home and Accounts.
- The page has one Global money display section with a labeled native select,
  explicit Save command, loading state, unavailable/retry state, validation,
  unchanged state, saving state, and saved confirmation.
- An unsaved currency selection exposes a notice that only the symbol and
  number format change. Save remains unavailable until the user acknowledges
  that no amount conversion occurs.
- One backend-owned global setting applies to every user and browser because
  authentication and per-household profiles do not yet exist.
- A single-row persisted settings model has a stable identity and a required
  supported currency code. Concurrent saves are last-successful-write wins;
  the response returns the authoritative saved setting.
- The database migration is explicit. Normal API startup does not migrate,
  seed, or rewrite settings. Absence of the row means USD without requiring
  seed data.
- Saving publishes a Client-local setting change so every currently rendered
  money surface updates without a full browser reload. New pages first load or
  reuse the authoritative global value and must not fall back to local fixed
  USD formatting after that load succeeds.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Domain/Application | Define supported display-currency values, USD default behavior, read, and update use cases. |
| Infrastructure | Persist the singleton setting with an explicit migration and atomic update. |
| API and contracts | Expose read/update operations, validation Problem Details, and aligned OpenAPI. |
| Client | Add Settings navigation/page, maintain authoritative global presentation state, and format all monetary text through one shared service. |

Stored transaction amounts and backend calculations remain decimal ledger
values and carry no per-record currency conversion.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Settings destination | The expanded left navigation includes Settings linking to `/settings`, with accessible current-route behavior on the Settings page. |
| AC-02: Default USD | With no persisted setting, the Settings page selects USD and every monetary value retains the current USD presentation. |
| AC-03: Persist named currency | Saving any supported named currency first requires acknowledgement that values are not converted, then persists its code, confirms success, and restores that choice after refresh and application restart. |
| AC-04: Application-wide update | After a successful save, every visible calendar total, inspector amount/label/confirmation, account-history amount, and other monetary value uses the selected format without reloading the browser. |
| AC-05: No conversion | Changing currency changes only presentation; API transaction amounts, database values, sums, filters, validation, and transaction behavior remain numerically unchanged. |
| AC-06: Truthful resilient states | Loading, unavailable/retry, invalid, unchanged, saving, and saved states are distinct; failed or obsolete saves do not publish an unsaved currency. |
| AC-07: Accessible responsive settings | The page has a descriptive heading, associated select label, keyboard-operable save, announced status/errors, visible focus, and no overlap or page-level horizontal overflow at supported viewports. |
| AC-08: Durable global contract | PostgreSQL stores at most one valid global currency code; normal startup does not migrate or seed it, and unsupported values cannot be persisted. |
| AC-09: Bounded slice | No exchange rates, amount conversion, per-account currency, mixed-currency ledger, localization setting, user profile, authentication, or no-currency option is introduced. |

## Implementation Notes

1. Centralize money formatting before replacing the current fixed `en-US`
   formatting in Calendar, TransactionInspector, and Accounts.
2. Keep ISO currency codes explicit rather than accepting arbitrary cultures.
3. Return decimal amounts unchanged from existing APIs; currency is global
   presentation state, not duplicated on each transaction contract.
4. Use an explicit migration command against the persistent manual database;
   do not reuse empty-database fixture initialization to alter nonempty data.

No new runtime dependency is expected.

## Validation

- Domain/Application tests cover default, supported values, rejection, and
  last-successful-write behavior.
- PostgreSQL tests cover migration, singleton persistence, default absence, and
  update durability.
- API tests cover read/update/default/validation and checked OpenAPI.
- Client component tests cover Settings states and every current money surface
  through the shared formatter and live setting-change notification, including
  prevention of save before display-only acknowledgement.
- One hosted PostgreSQL-backed browser journey changes USD to EUR, verifies
  unchanged numeric data with updated formatting across Settings, Calendar,
  inspector, and account history, then refreshes and verifies persistence.

## Definition of Done

The feature is complete when Settings persists one supported global named
currency, USD remains the no-row default, all current monetary presentation
updates immediately and after restart without numeric conversion, migrations
remain explicit, checked OpenAPI is current, and focused cross-layer and hosted
browser validation pass.

## Deferred Work

The no-currency generic-symbol mode is Feature 017. Exchange rates, conversion,
per-account or per-transaction currency, mixed-currency totals, broader locale
preferences, authentication, and per-household settings remain separate
outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-05 | Persist one ISO currency code globally. | The application has no user or household identity boundary yet. |
| 2026-09-05 | Treat selection as formatting only. | Re-labeling must not silently perform financial conversion or mutate ledger history. |
| 2026-09-05 | Use USD when no row exists. | The requested default needs no automatic startup seed. |
| 2026-09-05 | Start with five explicit two-decimal currencies. | A finite list gives deterministic symbols and formatting while leaving broader currency support for evidence-driven expansion. |
| 2026-09-07 | Keep named currencies as display cultures and indicators without conversion. | The setting intentionally changes presentation rather than ledger values; an explicit acknowledgement prevents users from mistaking it for conversion. |

## Dependencies

- [Feature 010](010-calendar-daily-expense-amounts.md) supplies calendar money
  totals.
- [Feature 012](012-require-an-account-for-every-transaction.md) supplies
  inspector transaction amounts.
- [Feature 013](013-view-an-accounts-transactions.md) supplies account-history
  amounts.
