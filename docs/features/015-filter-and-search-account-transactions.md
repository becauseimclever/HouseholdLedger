# Feature 015: Filter and Search Account Transactions

## Status

Status: Complete.

- Planned: 2026-09-05.
- Implemented: 2026-09-05.
- Validated: 2026-09-05.
- Depends on Feature 013's account-scoped transaction list.

## Outcome

A user viewing one account can narrow its transaction table with controls above
the table and can search the fields that transactions currently expose. The
backend applies the criteria to that account only, and the URL preserves the
active view through refresh and browser history.

## User Flow

1. The user opens `/accounts/{accountId}`.
2. Above the transaction table, the user chooses optional date,
   classification, or amount criteria and enters optional search text.
3. The user applies the criteria.
4. The table shows every matching transaction for that account in the existing
   newest-first order and reports the result count.
5. The user clears all criteria to restore the complete account history.

## Query Contract

The existing account-history endpoint accepts these optional criteria:

| Criterion | Meaning |
| --- | --- |
| From date | Inclusive minimum ledger date. |
| To date | Inclusive maximum ledger date. |
| Classification | One existing expense classification or all classifications. |
| Minimum amount | Inclusive non-negative minimum stored amount. |
| Maximum amount | Inclusive non-negative maximum stored amount. |
| Search | Case-insensitive match against the transaction's visible classification, ISO ledger date, or invariant plain-number amount text. |

- Criteria combine with logical AND. Search matches any one of its supported
  visible fields.
- Empty criteria retain Feature 013's complete history response and ordering.
- Filtering and search occur in the backend query before materialization; the
  Client never downloads unrelated transactions and hides them locally.
- Results remain scoped to the route account and ordered by ledger date
  descending, then creation sequence descending.
- Invalid dates, amounts, classification values, reversed ranges, and overly
  long search text return validation Problem Details and do not run an
  unbounded fallback query.
- The initial search limit is 100 trimmed characters. Search does not claim to
  match payee, merchant, memo, tags, or description because those fields do not
  exist yet.

## Filter Bar and URL Behavior

- A semantic filter form appears immediately above the account transaction
  table or its filtered empty state.
- Controls are labeled and include From, To, Classification, Minimum amount,
  Maximum amount, Search transactions, Apply, and Clear.
- Apply writes normalized criteria to the URL query string and requests the
  backend. Refresh, direct navigation, and browser back/forward restore the
  same criteria and result set.
- Clear removes all filter query parameters and reloads the complete history.
- Changing accounts retains only criteria valid for the destination account;
  no prior account's rows or late responses can replace the current result.
- No matches is distinct from an account with no transactions, an unavailable
  request, and an invalid filter form.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Application | Validate normalized criteria and request an account-scoped filtered result. |
| Infrastructure | Translate criteria to one PostgreSQL query and preserve deterministic ordering. |
| API and contracts | Bind optional query parameters, return validation Problem Details, and keep OpenAPI aligned. |
| Client | Render the filter form, synchronize criteria with the URL, show counts/states, and reject obsolete responses. |

No Domain invariant or database migration is expected.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Filter controls above table | The selected account view presents the complete labeled filter form immediately above its transaction results. |
| AC-02: Combined backend filtering | Date, classification, and amount criteria can be applied alone or together and return every and only matching rows for the selected account. |
| AC-03: Search current fields | Case-insensitive search matches visible classification text, ISO date, or invariant plain-number amount text without claiming nonexistent description fields. |
| AC-04: Addressable criteria | Applied criteria are normalized in the URL; refresh, direct navigation, and browser back/forward restore the same form and results; Clear restores complete history. |
| AC-05: Honest states and validation | Full-history empty, filtered no-match, loading, unavailable, and invalid-criteria states are distinct, accessible, and never show stale rows. |
| AC-06: Accessible responsive operation | Labels, validation, result count, status announcements, keyboard submission, and focus are understandable; controls and table remain usable without page-level horizontal overflow at supported viewports. |
| AC-07: Query safety | PostgreSQL performs account scoping and filtering in one bounded query, and late responses cannot cross account or criteria state. |
| AC-08: Bounded slice | No payee, merchant, memo, tag, description, sorting control, pagination, export, transaction mutation, or calendar filtering is introduced. |

## Implementation Notes

1. Extend Feature 013's account-history request instead of creating a parallel
   search endpoint.
2. Use typed criteria from transport through persistence; do not build SQL by
   concatenating user input.
3. Submit intentionally with Apply rather than issuing a request for every
   keystroke.
4. Preserve current formatting rules; Feature 016 later centralizes currency
   display without changing stored filter values.

No new runtime dependency is expected.

## Validation

- Application tests cover every criterion, combined criteria, boundaries,
  validation, account isolation, and deterministic ordering.
- PostgreSQL tests prove server-side translation and no cross-account rows.
- API tests cover query binding, validation Problem Details, and checked
  OpenAPI.
- Client component tests cover URL restoration, Apply/Clear, all states,
  result count, account changes, and stale-response rejection.
- One hosted browser journey applies combined filters, searches, observes no
  matches, clears, refreshes, and uses browser history at desktop and one
  narrow viewport.

## Definition of Done

The feature is complete when the controls above an account's table produce an
addressable, backend-filtered and searched result with truthful states,
validation, account isolation, deterministic ordering, current OpenAPI, and
focused PostgreSQL, API, Client, and hosted browser validation.

## Deferred Work

Adding searchable transaction descriptions, payees, merchants, tags, custom
classifications, sorting controls, pagination, saved filters, and export remain
separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-05 | Search only fields that transactions currently expose. | Search must be useful without inventing unrequested transaction data. |
| 2026-09-05 | Apply criteria in PostgreSQL. | Account histories can grow, so filtering an already downloaded complete list would not scale honestly. |
| 2026-09-05 | Put normalized criteria in the URL. | Filtered account views survive refresh and browser navigation and can be linked directly. |

## Dependencies

- [Feature 013](013-view-an-accounts-transactions.md) supplies the account
  route, account-history endpoint, table, ordering, and truthful base states.
