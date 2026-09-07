# Feature 014: Expand Accounts in Workspace Navigation

## Status

Status: Complete.

- Planned: 2026-09-05.
- Implemented: 2026-09-05.
- Validated: 2026-09-05.
- Depends on Feature 011's persisted account catalog and Feature 013's
  account-detail routes.

## Outcome

The left workspace navigation presents Accounts as an expandable hierarchy. A
user can follow the Accounts text to the account-management page or use its
separate chevron button to reveal persisted accounts and follow one directly to
that account's transaction list.

## User Flow

1. The user opens the expanded workspace navigation.
2. The user activates the chevron beside Accounts to reveal account links.
3. The user follows an indented account link.
4. The existing `/accounts/{accountId}` page opens and shows that account's
   transactions.
5. The user can independently follow the Accounts text to `/accounts` to manage
   the account catalog.

## Navigation Contract

- `Accounts` remains a native link to `/accounts`; activating its text never
  toggles the account submenu.
- A separate native icon button beside the link expands or collapses the
  submenu. It uses the existing Lucide icon source, has an accessible name,
  exposes `aria-expanded`, and references the submenu with `aria-controls`.
- The submenu loads the authoritative persisted account catalog and displays
  one indented native link per account in the same deterministic order as the
  Accounts page.
- Each account link targets `/accounts/{accountId}`. On that route, the account
  link and the Accounts branch expose current-route state visually and
  programmatically without relying on color alone.
- Direct navigation to an account route expands the Accounts branch so the
  current item is visible. On other routes, the user's transient expanded or
  collapsed choice is retained for the current application session only.
- Loading, empty, and unavailable account-catalog states are truthful and do
  not remove the Accounts link. An unavailable submenu offers a retry without
  affecting navigation to `/accounts`.
- Creating an account on the Accounts page refreshes the navigation catalog so
  the new account becomes available without a full browser reload.
- Collapsing the entire workspace navigation keeps its existing behavior. The
  branch state is preserved while the pane is hidden.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| API and Application | Reuse the existing ordered account-list contract without adding a navigation-specific endpoint. |
| Client layout | Load the account catalog, own transient branch state, render accessible disclosure and nested links, and reject obsolete responses. |
| Account workflow | Notify or otherwise trigger an authoritative catalog reread after successful account creation. |

No Domain, persistence, migration, or OpenAPI change is expected.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Independent actions | Activating Accounts text navigates to `/accounts`; activating its chevron only expands or collapses the account submenu. |
| AC-02: Authoritative account links | Expanding Accounts displays every persisted account exactly once in deterministic order, with each indented link targeting `/accounts/{accountId}`. |
| AC-03: Current account context | Direct navigation or activation of an account link opens its Feature 013 transaction list, expands the branch, and exposes the account and branch as current without color alone. |
| AC-04: Truthful catalog states | Loading, empty, unavailable, retry, and refreshed-after-create states remain understandable while the Accounts destination stays usable. |
| AC-05: Accessible disclosure | The chevron button is keyboard operable, named, focus-visible, and has accurate `aria-expanded` and `aria-controls`; collapsed children are absent from keyboard and accessibility navigation. |
| AC-06: Responsive hierarchy | Primary links, chevron, indented account names, focus indicators, and state text do not overlap or cause page-level horizontal overflow at supported desktop and narrow viewports. |
| AC-07: Bounded slice | No account editing, deletion, balance, transaction filtering/search, persistent menu preference, or new account-specific backend endpoint is introduced. |

## Implementation Notes

1. Reuse the existing account API client and route-aware native links.
2. Keep link activation and disclosure activation as separate hit targets.
3. Share the existing account-change notification or add the smallest
   Client-local invalidation signal needed after successful creation.
4. Keep branch expansion transient; it is navigation state, not a persisted
   application setting.

No new runtime dependency is expected.

## Validation

- Client component tests cover independent link/disclosure activation,
  semantics, catalog states, deterministic links, current-route expansion,
  refresh after account creation, and collapsed-pane behavior.
- One hosted browser journey proves Home -> expand Accounts -> select an account
  -> view its transactions -> follow Accounts text at desktop and one narrow
  viewport.
- A focused Client build and existing account API regressions pass.

## Definition of Done

The feature is complete when the workspace navigation provides an accessible,
dynamic Accounts disclosure whose separate text and chevron actions work as
documented, persisted account links reach the existing transaction lists,
truthful states and creation refresh work, and focused component and hosted
browser validation pass.

## Deferred Work

Transaction filters and search are Feature 015. Persistent navigation state,
account grouping, account icons, balances, rename, archive, and deletion remain
separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-05 | Keep Accounts text and disclosure as separate controls. | The requested navigation and expand actions must remain predictable for pointer, keyboard, and assistive-technology users. |
| 2026-09-05 | Reuse the account catalog and Feature 013 routes. | Navigation is another view of existing backend truth, not a second account model. |
| 2026-09-05 | Keep branch state transient. | Persisting menu preference is not needed to prove account navigation. |

## Dependencies

- [Feature 011](011-add-a-named-account.md) supplies the persisted account
  catalog and account creation.
- [Feature 013](013-view-an-accounts-transactions.md) supplies
  `/accounts/{accountId}` and the complete account transaction list.
