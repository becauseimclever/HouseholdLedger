# Feature 027: Focus Account Detail Gallery

## Status

Status: Complete.

- Planned: 2026-09-14.
- Approved: 2026-10-04.
- Depends on the account catalog and account detail history routes.

## Outcome

When a user views a specific account, the page presents the selected account
card and its detail without also rendering cards for every other account.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Catalog remains complete | The `/accounts` route shows all persisted account cards in backend order and retains account creation. |
| AC-02: Focused account detail | The `/accounts/{accountId}` route renders only the selected persisted account card; other account cards are absent from the DOM and keyboard navigation. |
| AC-03: Detail behavior remains intact | Account history, filters, direct-link behavior, and truthful missing, unavailable, and empty states remain unchanged. |
| AC-04: Bounded slice | The account query, account-history API, persisted data, and the separate new-account creation surface remain unchanged. |

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Client | Select the catalog or detail presentation from the existing route and loaded account catalog. |

## Validation

- Client component tests use multiple accounts to prove the catalog route shows
  all cards and an account-detail route shows only the selected card.
- Existing direct-link, unavailable, and stale-response tests continue to cover
  account-detail state integrity.
- The 148-test Client suite and isolated Firefox desktop/mobile journey passed.
  The fresh-setup journey verified a single account card in the detail DOM.

## Definition of Done

The feature is complete when account detail routes are focused on their selected
account without regressing catalog or account-history behavior.