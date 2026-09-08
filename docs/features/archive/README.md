# Archived Features

These specifications are no longer active roadmap work. They remain available
as historical product, acceptance, and decision records.

| Feature | Final disposition | Outcome |
| --- | --- | --- |
| [001](001-application-scaffolding.md) | Closed and superseded | Established the repository and architecture baseline before smaller features replaced its unfinished process contract. |
| [002](002-minimal-hosted-sample-shell.md) | Complete | Proved the standard API-hosted Blazor WebAssembly application path. |
| [003](003-workspace-navigation-and-ui-foundation.md) | Complete | Added the accessible desktop workspace and pane controls. |
| [004](004-calendar-item-selection-and-inspector-detail-contract.md) | Complete | Added transient Today, Week, and Month date selection. |
| [005](005-record-a-transaction-for-selected-day.md) | Complete | Added selected-day transaction creation, persistence, and authoritative rereads. |
| [006](006-workspace-pane-preference-persistence.md) | Superseded | Combined its transaction proposal into Feature 005 and deferred pane preferences. |
| [007](007-correct-or-remove-selected-day-transaction.md) | Complete | Added selected-day transaction correction and confirmed permanent removal. |
| [008](008-open-source-notices-page.md) | Complete | Added published open-source attribution in the application. |
| [009](009-themeable-design-system-and-workbench-dark-theme.md) | Complete | Added the custom semantic-token design system and themeable three-pane `Workbench Dark` workspace. |
| [010](010-calendar-daily-expense-amounts.md) | Complete | Added exact backend-calculated daily and month-to-date expense totals to the calendar. |
| [011](011-add-a-named-account.md) | Complete | Added durable named accounts and account creation. |
| [012](012-require-an-account-for-every-transaction.md) | Complete | Required every transaction to belong to a persisted account. |
| [013](013-view-an-accounts-transactions.md) | Complete | Added complete per-account transaction history. |
| [014](014-expand-accounts-in-workspace-navigation.md) | Complete | Added persisted account destinations to workspace navigation. |
| [015](015-filter-and-search-account-transactions.md) | Complete | Added backend-owned account transaction filters and search. |
| [016](016-configure-global-display-currency.md) | Complete | Added a durable display culture and currency indicator with explicit no-conversion acknowledgement. |
| [017](017-use-a-generic-money-display.md) | Complete | Added the durable generic `¤` money display option. |
| [018](018-present-navigation-as-workspace-tiles.md) | Complete | Presented application destinations as workspace navigation tiles. |
| [019](019-collapse-navigation-from-its-pane.md) | Complete | Added pane-owned navigation collapse and a reachable rail. |
| [020](020-select-a-workbench-light-theme.md) | Complete | Added a durable Workbench Light theme choice. |
| [021](021-present-a-themed-application-footer-and-links.md) | Complete | Added the themed application footer and intentional link roles. |
| [022](022-collapse-and-reveal-the-inspector-pane.md) | Complete | Added inspector collapse and selected-date automatic reveal. |
| [023](023-navigate-from-the-collapsed-icon-rail.md) | Complete | Preserved complete destination navigation in the collapsed rail. |

Historical audits remain in [`docs/audit`](../../audit/). Archived documents are
not active implementation gates and should not be edited to rewrite historical
evidence. Corrections to current direction belong in the active roadmap or a
new feature.
