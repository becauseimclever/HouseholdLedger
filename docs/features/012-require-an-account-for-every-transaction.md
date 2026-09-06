# Feature 012: Require an Account for Every Transaction

## Status

Status: Complete.

- Planned: 2026-09-04.
- Depends on Feature 011's persisted account catalog.
- The application is not deployable and has no production data. Until the user
  changes that assumption, schema changes may reset disposable development and
  test data instead of preserving or backfilling it.

## Outcome

When a user selects a calendar day, the transaction form includes a required
Account field populated from persisted accounts. Every created transaction
belongs to exactly one account, and the selected-day list identifies that
account after an authoritative backend reread.

Existing transaction correction also permits fixing the account. The date
continues to come from calendar selection and is not independently editable.

## User Flow

1. The user follows `Accounts` in the workspace navigation and creates at least
  one account.
2. The user follows `Home` to return to the calendar.
3. The user selects a calendar day.
4. The inspector loads both that day's transactions and the available accounts.
5. The user enters amount and classification, chooses an account, and saves.
6. The backend validates the account, persists the ownership relationship, and
   returns the transaction with its account identity and name.
7. The inspector rereads the selected day and displays the account on the saved
   transaction. Calendar daily totals continue to include all accounts.

## Transaction Account Contract

- Each transaction has one required, immutable-in-storage relationship to an
  existing account identifier. The relationship may be corrected through the
  existing transaction edit workflow.
- Create and update requests carry `AccountId`; free-text account names are not
  accepted as ownership.
- Transaction responses carry both `AccountId` and the current account display
  name so the Client does not join independent responses to label a record.
- A missing, empty, or nonexistent account identifier creates no transaction
  change and returns a truthful validation response.
- Account deletion remains unavailable, so this slice does not define cascade,
  reassignment, or orphan behavior. The database foreign key uses restrictive
  behavior.
- Selected-day reads remain ordered by backend creation sequence and are not
  filtered by account.

## Existing Data and Migration Policy

The application is pre-deployment, so this feature does not preserve or
backfill existing development transactions. The implementation may reset and
recreate disposable development and test databases with the required account
relationship. It does not fabricate an `Unassigned` account or guess ownership
for pre-account rows.

The resulting schema requires a non-null account identifier and a restrictive
foreign key without a synthetic default. Database reset or migration remains an
explicit development or test operation; the API does not migrate or delete data
automatically at startup.

## Development and Test Transactions

After resetting the disposable database, Feature 012 uses Feature 011's fixed
account identifiers and the following deterministic transactions:

| Sequence | Identifier | Account | Ledger date | Amount | Classification |
| --- | --- | --- | --- | ---: | --- |
| 1 | `20000000-0000-0000-0000-000000000001` | `Household Checking` | 2026-09-01 | $82.45 | Necessities |
| 2 | `20000000-0000-0000-0000-000000000002` | `Cash Wallet` | 2026-09-01 | $12.00 | Optional |
| 3 | `20000000-0000-0000-0000-000000000003` | `Household Checking` | 2026-09-03 | $14.00 | Culture |
| 4 | `20000000-0000-0000-0000-000000000004` | `Household Checking` | 2026-09-03 | $6.50 | Optional |
| 5 | `20000000-0000-0000-0000-000000000005` | `Cash Wallet` | 2026-09-15 | $25.25 | Necessities |
| 6 | `20000000-0000-0000-0000-000000000006` | `Household Checking` | 2026-09-30 | $40.00 | Unexpected |
| 7 | `20000000-0000-0000-0000-000000000007` | `Cash Wallet` | 2026-10-01 | $9.75 | Culture |

`Rainy Day Savings` intentionally has no transactions. The fixture covers all
classifications, two accounts, an empty account, multiple transactions on one
day, inactive dates within a month, and a month boundary.

The expected all-account September daily totals are $94.45 on September 1,
$20.50 on September 3, $25.25 on September 15, and $40.00 on September 30. The
September month total is $180.20; October 1 is $9.75 and does not contribute to
September.

Automated tests load this data into independently isolated stores. An optional
local-development seed action is explicit, deterministic, and allowed only
against an empty disposable database. It must fail rather than append to a
nonempty store. Migrations and normal API startup never seed data.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Domain | Require a non-empty account identifier on every transaction and preserve it through transaction behavior. |
| Application | Validate that the selected account exists; create, list, and revise account-owned transaction read models. |
| Infrastructure | Add the required restrictive foreign key and query account labels without per-row database calls. |
| API and contracts | Require account identity in create/update requests, return account identity/name, map validation outcomes, and update OpenAPI. |
| Client | Load account choices, add the required Account field to selected-day create/edit forms, and display ownership on each transaction. |

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Account field on day selection | Selecting a calendar day opens the inspector with a labeled required Account field populated only from persisted accounts. |
| AC-02: Account-owned creation | Choosing an existing account with otherwise valid values creates exactly one transaction linked to that account; the backend reread displays its account name. |
| AC-03: Required ownership | Missing, empty, or nonexistent account identity creates no transaction, and the database rejects any transaction without a valid account relationship. |
| AC-04: Correctable ownership | Editing a transaction can change its account to another existing account while preserving its identifier, date, and creation order; invalid account changes leave it unchanged. |
| AC-05: Truthful account states | With no accounts, the inspector explains that an account must be added and links to `/accounts` instead of offering a fake or disabled placeholder choice. Account-load failures are distinct from an empty catalog. |
| AC-06: Selection and request safety | Changing dates or completing obsolete account/transaction requests cannot show or save data under the wrong selected date. |
| AC-07: Calendar consistency | Calendar daily and month-to-date totals continue to aggregate all accounts and refresh after account-owned transaction mutations. |
| AC-08: Navigation continuity | Feature 011's `Home` and `Accounts` links remain available with correct current-route semantics; the no-account inspector link reaches the same `/accounts` page. |
| AC-09: Deterministic fixtures | Isolated tests and the explicit empty-database development seed can create the documented accounts and transactions with stable identifiers, ownership, dates, amounts, classifications, and ordering; startup and migrations create none. |
| AC-10: Bounded slice | No account deletion, archival, balance, account filter, per-account calendar breakdown, transfer, income, or transaction date move is introduced. |

## Implementation Notes

1. Add account identity to the Domain constructor and revision operation so an
   unowned transaction cannot exist in memory.
2. Validate account existence in the Application boundary and back it with a
   PostgreSQL foreign key for race-safe integrity.
3. Extend the existing selected-day contracts and workflow rather than adding
   parallel account-specific transaction endpoints.
4. Load account labels efficiently with selected-day records and preserve the
   existing stale-response protections.

No new runtime dependency is expected.

## Validation

- Domain and Application tests prove required ownership, nonexistent-account
  handling, correction, and unchanged date/sequence behavior.
- PostgreSQL integration tests apply the account and transaction migrations to
  an empty isolated database, load the deterministic fixture, and prove
  non-null and foreign-key enforcement.
- API tests cover account-owned create/list/update and invalid account Problem
  Details; checked OpenAPI matches runtime generation.
- Client component tests cover account loading, empty/unavailable states,
  required create/edit fields, ownership display, navigation continuity, the
  no-account link destination, and obsolete responses.
- One hosted PostgreSQL-backed browser journey proves Accounts -> add account ->
  Home -> select day -> save transaction to account -> see account and refreshed
  calendar amount.
- Fixture-focused checks prove exact September daily totals, the $180.20 month
  total, account isolation, same-day ordering, the empty savings account, and
  exclusion of October 1 from September.

Completed validation:

- Domain, Application, API contract/integration, and Client component tests pass
  for required ownership, correction, truthful states, stale responses, and
  all-account calendar totals.
- The checked OpenAPI document matches the runtime-generated contract.
- The owned PostgreSQL 18 harness passes all three provider tests, including the
  required restrictive foreign key, and removes its isolated container.
- The published Firefox journey passes the complete Accounts -> add account ->
  Home -> select day -> save, reread, revise, remove, and calendar-refresh flow.
- The solution builds in Release configuration, and the exact deterministic
  seven-transaction fixture produces the documented September totals and month
  boundary behavior.

## Definition of Done

The feature is complete when every transaction is account-owned in Domain and
PostgreSQL, the selected-day create/edit workflows require a persisted account,
the account is visible after backend rereads, all-account calendar totals remain
correct, focused tests pass, checked OpenAPI is current, the explicit migration
is present, and the solution builds.

## Deferred Work

- Feature 013 owns selection of an account card and its complete transaction
  listing.
- Account filtering, per-account calendar breakdowns, balances, account
  deletion/reassignment, transfers, income, reconciliation, imports, and
  reporting remain separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-04 | Require one real account for every transaction. | The user requires complete ownership rather than an optional label. |
| 2026-09-04 | Treat existing data as disposable until deployment readiness changes. | The application has no production data, so reset-and-recreate is simpler and more honest than designing a premature backfill path. |
| 2026-09-04 | Keep calendar totals combined across accounts. | The user chose one all-account daily amount rather than filtering or per-account cell breakdowns. |
| 2026-09-04 | Allow account correction in the existing edit flow. | Account assignment can be entered incorrectly and should be correctable without recreating the transaction. |
| 2026-09-04 | Preserve `Home` and `Accounts` navigation through the transaction workflow. | Account setup and calendar entry form one understandable round trip without relying on browser history. |
| 2026-09-04 | Use one deterministic account-owned fixture in tests and optional local development. | Stable realistic records exercise ownership, ordering, empty states, classifications, and calendar arithmetic without becoming production seed data. |

## Dependencies

- [Feature 010](010-calendar-daily-expense-amounts.md) supplies the all-account
  calendar summary and mutation-refresh contract.
- [Feature 011](011-add-a-named-account.md) supplies the authoritative account
  catalog and account identifiers.
- [Feature 005](archive/005-record-a-transaction-for-selected-day.md) and
  [Feature 007](archive/007-correct-or-remove-selected-day-transaction.md)
  supply the selected-day create/list/edit workflows extended here.
