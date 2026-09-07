# Feature 011: Add a Named Account

## Status

Status: Complete.

- Planned: 2026-09-04.
- Completed: 2026-09-05.
- This feature creates the minimum account catalog needed before transactions
  can require account ownership.

## Outcome

A user can open an Accounts workspace, see each persisted account as an account
card, and add a named account from a dedicated creation card. After a successful
save, the backend-owned card gallery is reread and includes the new account.

The account is deliberately small: it identifies where a transaction belongs.
It does not claim to know a balance, institution, account number, or financial
accounting behavior that the product has not yet defined.

## User Flow

1. The user follows an `Accounts` destination in the workspace navigation.
2. The Accounts page loads the authoritative account-card gallery.
3. The user enters a name in the `New account` card and submits the form.
4. The backend validates and persists the account.
5. The Client rereads and displays the new persisted account as a card.
6. The user follows `Home` to return to the calendar.

## Workspace Navigation Contract

Feature 011 replaces the navigation pane's empty placeholder with exactly two
primary destinations:

| Link | Route | Purpose |
| --- | --- | --- |
| `Home` | `/` | Return to the calendar workspace. |
| `Accounts` | `/accounts` | Open the account-management page. |

- Both destinations use native link navigation and remain present on the
  calendar and Accounts routes whenever the navigation pane is expanded.
- The link for the current route is exposed programmatically and visibly
  without relying on color alone. Exactly one primary destination is current on
  `/`, `/accounts`, and future account-detail routes beneath `/accounts`.
- Link labels do not encode pane position, and keyboard focus remains visible.
- Collapsing the navigation pane continues to remove its links from sequential
  keyboard navigation; expanding it restores both links without changing the
  current route.
- The existing `Open-source notices` auxiliary footer link remains outside the
  primary navigation and is unchanged.

## Account Card Gallery

- The page heading is `Accounts`. Its primary content is one responsive gallery
  containing one card per persisted account and one visually consistent `New
  account` card.
- Each persisted account card shows its name. Cards remain noninteractive until
  Feature 013 adds the account-detail route and turns the name into a native
  selection link; Feature 011 does not expose a dead destination.
- The `New account` card contains the labeled name field, validation feedback,
  and explicit create command. The card itself is not a clickable container.
- Account cards and the creation card remain separate peers; cards are not
  nested. Corners remain at or below the design system's 0.5rem maximum.
- Gallery reflow must keep names, controls, validation, and focus indicators
  visible without overlap at supported desktop and narrow viewports.
- Account cards do not invent balances, institutions, masked account numbers,
  transaction counts, icons, or account types.

## Minimum Account Contract

| Field | Rule |
| --- | --- |
| Identifier | Backend-generated opaque identifier; stable and not user-editable. |
| Name | Required display name, trimmed, 1 to 100 characters, and unique without regard to case. |

Names retain their entered casing after trimming. Whitespace-only names and a
name that differs from an existing account only by case are invalid. Concurrent
duplicate creation is rejected by the durable uniqueness constraint, not only
by Client validation.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Domain | Define account identity and valid display-name rules. |
| Application | Provide create and ordered-list use cases with duplicate-name outcomes. |
| Infrastructure | Persist accounts with EF Core/Npgsql, including an explicit migration and case-insensitive uniqueness. |
| API and contracts | Expose additive account list/create operations, validation Problem Details, and aligned OpenAPI. |
| Client | Add the Accounts destination and card-gallery page, render truthful states, submit through the creation card, and reread backend data. |

Accounts are returned by display name using deterministic case-insensitive
ordering with identifier as the tie-breaker.

## Development and Test Accounts

Feature validation uses the following deterministic account fixtures. Feature
012 retains these identifiers when it adds account-owned transactions.

| Identifier | Name | Purpose |
| --- | --- | --- |
| `10000000-0000-0000-0000-000000000001` | `Household Checking` | Primary account with multiple dates and same-day transactions. |
| `10000000-0000-0000-0000-000000000002` | `Cash Wallet` | Second populated account used to prove account isolation. |
| `10000000-0000-0000-0000-000000000003` | `Rainy Day Savings` | Empty account used to prove truthful empty states. |

The fixtures are test/development data, not automatic product records. Automated
tests create them in isolated stores. A documented explicit local-development
seed action may create them only in an empty disposable database; normal API
startup and EF migrations do not seed accounts.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Primary workspace navigation | The expanded navigation pane contains `Home` linking to `/` and `Accounts` linking to `/accounts`. A keyboard or pointer user can move between the calendar and account-management page, and exactly one link exposes the current route programmatically and visually without color alone. |
| AC-02: Honest account-card gallery | The page distinguishes loading, empty, populated, and unavailable states. Each persisted account is a separate named card, and no locally invented account is presented as persisted. |
| AC-03: Valid card creation | The separate `New account` card contains the labeled creation form. A valid unique name creates exactly one persisted account and the backend reread displays its card in deterministic order. |
| AC-04: Validation and concurrency | Blank, over-length, and case-insensitive duplicate names create nothing and produce accessible, recoverable validation feedback, including when another request creates the duplicate first. |
| AC-05: Accessible operation | The page has a descriptive heading, meaningful card structure, associated field label and error, keyboard-operable navigation and submission, visible focus, and announced load/save status. |
| AC-06: Responsive card layout | Persisted cards and the creation card reflow without nested cards, clipped names, overlapping controls, or page-level horizontal scrolling at supported desktop and narrow viewports. |
| AC-07: Bounded slice | No account rename, deletion, archival, type, opening balance, institution, account number, transaction relationship, transaction listing, transfer, or balance calculation is added. |

## Implementation Notes

1. Use a dedicated account model and repository; an account is not a string
   field embedded in a transaction.
2. Use an explicit PostgreSQL migration. Do not migrate automatically at API
   startup.
3. Enforce normalized uniqueness durably using established EF Core and
   PostgreSQL mechanisms; do not rely on a read-before-write race.
4. Add the `Home` and `Accounts` links to the existing navigation landmark,
   using Blazor's framework-supported route-aware link behavior.
5. Build the gallery with semantic articles, form controls, and CSS Grid. Add
  native account-selection links with Feature 013's implemented detail route.

No new runtime dependency is expected.

## Validation

- Domain and Application tests cover trimming, length, duplicate outcomes, and
  deterministic listing.
- PostgreSQL integration tests prove persistence and case-insensitive unique
  enforcement against the real provider and load the deterministic account
  fixtures in an isolated database.
- API tests cover empty/list/create and validation Problem Details.
- Client component tests cover exact link labels and destinations, current-route
  semantics, collapsed-pane behavior, card structure, truthful states,
  validation, creation, and authoritative reread.
- One hosted browser journey proves Home -> Accounts -> account creation ->
  visible account card -> Home, including keyboard operation, current-link
  presentation, and non-overlapping desktop and narrow gallery layouts.

Completed validation:

- Domain and Application focused tests pass for account invariants, duplicate
  outcomes, and deterministic ordering.
- API, checked OpenAPI, typed Client, component, and workspace navigation tests
  pass.
- The owned PostgreSQL 18 harness passes all three provider tests and removes
  its isolated container.
- The published Firefox journey passes the Home -> Accounts -> create -> Home
  flow and desktop/narrow layout checks.

## Definition of Done

The feature is complete: the primary navigation provides working `Home` and
`Accounts` links, the Accounts page presents persisted accounts and the
creation form as peer cards, a valid account appears after a backend reread,
invalid or duplicate names are rejected truthfully, focused tests pass, checked
OpenAPI is current, the explicit migration is present, and the solution builds.

## Deferred Work

- Feature 012 makes account ownership mandatory for transactions.
- Feature 013 makes an account card selectable and shows that account's full
  transaction history.
- Rename, deletion, archival, account type, opening/current balances,
  institutions, account numbers, reconciliation, and transfers remain outside
  this feature.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-04 | Start with account name only. | The user chose the smallest identity needed for transaction ownership; type and balance would introduce separate rules. |
| 2026-09-04 | Keep account creation separate from transaction changes. | It provides one testable catalog outcome and gives Feature 012 an authoritative selection source. |
| 2026-09-04 | Add `Home` and `Accounts` as the first primary navigation destinations. | Both destinations lead to implemented workspaces and provide a direct round trip between calendar and account management. |
| 2026-09-04 | Present persisted accounts and creation as peer cards. | The user requested a scannable account gallery with a dedicated place to add another account. |
| 2026-09-04 | Define three deterministic development/test accounts. | Two populated accounts prove isolation and one empty account proves the account and history empty states without inventing production data. |
| 2026-09-05 | Keep Feature 011 account cards noninteractive. | A native link to `/accounts/{accountId}` would be a dead destination until Feature 013 implements the detail route; the card becomes selectable in that slice. |

## Dependencies

- Existing hosted Client/API, PostgreSQL boundary, checked OpenAPI workflow,
  and test projects provide the implementation and validation homes.
