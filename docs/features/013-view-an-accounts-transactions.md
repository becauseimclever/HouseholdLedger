# Feature 013: View an Account's Transactions

## Status

Status: Complete.

- Planned: 2026-09-04.
- Implemented: 2026-09-05.
- Validated: 2026-09-05.
- Depends on Feature 011's account-card gallery and Feature 012's mandatory
  transaction ownership.

## Outcome

When a user selects a persisted account card, the Accounts workspace shows that
account and a complete backend-owned listing of every transaction belonging to
it. The selection is represented in the URL so refresh, direct navigation, and
browser history preserve the account context.

## User Flow

1. The user follows `Accounts` in primary navigation.
2. The user selects a persisted account card.
3. The route changes to `/accounts/{accountId}` and identifies the selected
   account by name.
4. The page loads every transaction belonging to that account.
5. Each row shows the ledger date, amount, and classification.
6. The user can select another account card or follow `Home` to the calendar.

## Account Selection and Listing Contract

- Persisted account cards use native links to `/accounts/{accountId}`. Generic
  card containers do not receive custom click or keyboard behavior.
- The selected card exposes current selection programmatically and visually
  without relying on color alone. The primary `Accounts` navigation link
  remains current on account-detail routes.
- The backend returns only transactions whose required `AccountId` matches the
  route account. The Client does not filter an all-transactions response.
- The complete list is ordered by ledger date descending, then backend creation
  sequence descending, so the newest ledger activity appears first and ordering
  is deterministic.
- Each item displays date, existing fixed-USD amount, and classification. The
  selected account heading supplies account context, so the account name need
  not be repeated in every item.
- An account with no transactions has a truthful empty state. A missing account
  has a not-found state distinct from an empty account; transport failures have
  a retryable unavailable state.
- “Complete” means every current persisted transaction for the account is
  returned in this pre-deployment slice. Pagination, virtualization, date-range
  filters, and incremental loading are deferred until data volume requires a
  separate contract.

## Fixture Expectations

Feature 013 reuses the exact deterministic accounts and transactions defined by
Features 011 and 012. It does not own a second seed dataset.

- `Household Checking` returns four transactions in this order: September 30;
  September 3 sequence 4; September 3 sequence 3; September 1.
- `Cash Wallet` returns October 1, September 15, then September 1.
- `Rainy Day Savings` returns the truthful empty state.
- No account history contains a transaction assigned to another account.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Application | Resolve the account and return its transactions in deterministic newest-first order. |
| Infrastructure | Query by account identifier without loading unrelated transactions or issuing per-row queries. |
| API and contracts | Expose account detail/history with not-found behavior and aligned OpenAPI. |
| Client | Represent selection in the route, render selected-card and list states, and reject obsolete responses after selection changes. |

No new Domain invariant is needed because Feature 012 already owns mandatory
account identity on every transaction.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Select an account card | Activating a persisted account card navigates to `/accounts/{accountId}`, identifies that account, and gives the card a programmatic and visible selected state. |
| AC-02: Complete account history | The selected account shows every and only its persisted transactions with date, USD amount, and classification, ordered by date and creation sequence descending. |
| AC-03: Honest states | Loading, empty account, unavailable history, and missing account states are distinct. No prior account's transactions remain visible after selection changes. |
| AC-04: Addressable navigation | Refresh and direct navigation preserve a valid selected account; browser back/forward and selecting another card update the detail without losing `Home` and `Accounts` navigation. |
| AC-05: Accessible listing | Account links, selected state, account heading, transaction list or table semantics, status announcements, and retry behavior are keyboard and assistive-technology understandable. |
| AC-06: Responsive workspace | The account gallery and transaction listing remain readable without overlap or page-level horizontal scrolling at supported desktop and narrow viewports. |
| AC-07: Fixture coverage | The shared deterministic fixture produces the documented checking and cash histories in newest-first order and the savings empty state, with no cross-account records. |
| AC-08: Bounded slice | No transaction create/edit/delete controls, account rename/delete, totals, balances, search, filtering, sorting controls, pagination, export, or per-account calendar view is added. |

## Implementation Notes

1. Extend the existing Accounts page with an optional route account identifier
   rather than creating a disconnected second account-management surface.
2. Add a dedicated account-scoped backend query. Do not fetch all transactions
   and filter them in the browser.
3. Use the existing transaction read fields and money formatting rules.
4. Cancel or ignore obsolete account-detail requests when route selection
   changes.

No new runtime dependency is expected.

## Validation

- Application tests cover account isolation, empty accounts, deterministic
  newest-first ordering, missing accounts, and the exact shared fixture
  expectations.
- PostgreSQL integration tests prove the account-scoped query returns all and
  only matching rows in the required order.
- API tests cover populated, empty, and missing account responses; checked
  OpenAPI matches runtime generation.
- Client component tests cover native card links, route selection, selected and
  current semantics, all truthful states, stale responses, and list content.
- One hosted PostgreSQL-backed browser journey proves Accounts -> select card ->
  complete history -> select another card -> browser Back -> Home at desktop and
  one narrow viewport.

## Definition of Done

The feature is complete when selecting an account card produces an addressable,
accessible, backend-owned complete transaction listing for only that account;
empty, missing, unavailable, and stale states are truthful; focused tests pass;
checked OpenAPI is current; and the solution builds.

## Deferred Work

- Transaction mutation from the account page, account rename/deletion,
  balances, totals, search, filters, sorting controls, pagination,
  virtualization, export, and account-specific calendar summaries remain
  separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-04 | Use `/accounts/{accountId}` for selection. | Account context survives refresh and browser navigation and can be linked directly. |
| 2026-09-04 | Return the complete account history newest first. | The user requested all account transactions, while recent ledger activity is the most useful starting position. |
| 2026-09-04 | Keep the history read-only. | Viewing an account's ledger is one outcome; transaction mutations remain in the selected-day inspector. |
| 2026-09-04 | Reuse the account-owned fixture from Features 011 and 012. | One canonical dataset prevents test drift while proving populated, empty, ordering, and isolation behavior. |

## Dependencies

- [Feature 011](011-add-a-named-account.md) supplies primary navigation,
  persisted account cards, and the Accounts workspace.
- [Feature 012](012-require-an-account-for-every-transaction.md) guarantees
  every transaction has a valid account identifier.
