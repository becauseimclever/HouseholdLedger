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
| [024](024-plan-and-review-a-month-with-intention.md) | Complete | Reconcile monthly intention and compare it with deliberately confirmed income and recorded expenses. |
| [025](025-align-account-subnavigation.md) | Complete | Align expanded account navigation with its parent destination. |
| [026](026-minimize-empty-day-inspector.md) | Complete (refined) | Focus empty days on recording actions without empty income detail. |
| [027](027-focus-account-detail-gallery.md) | Complete | Show only the selected account card on account detail routes. |
| [028](028-top-align-collapsed-workspace-navigation.md) | Complete | Keep collapsed workspace-navigation destinations at the top of the rail. |
| [030](030-normalize-calendar-journey-line-endings.md) | Complete | Restore formatting-gate compliance for the calendar browser journey source. |
| [031](031-normalize-generated-migration-encoding.md) | Complete | Restore formatting-gate compliance for generated migration source encodings. |
| [032](032-complete-selected-date-inspector-test-fixture.md) | Complete | Restore the selected-date inspector component behavior proof. |
| [033](033-correct-confirmed-income.md) | Complete | Correct or remove receipts and retry confirmation without duplicate records. |
| [034](034-reflect-on-a-month.md) | Complete | Save what worked and one intention for next month. |
| [035](035-reach-monthly-intention-easily.md) | Complete | Make monthly planning reachable with predictable navigation and protected drafts. |
| [036](036-describe-an-expense.md) | Complete | Preserve optional expense context for later review. |
| [037](037-understand-recorded-monthly-activity.md) | Complete | Distinguish recorded actuals, signed cashflow, expectations, and savings intent. |

Historical audits remain in [`docs/audit`](../../audit/). Archived documents are
not active implementation gates and should not be edited to rewrite historical
evidence. Corrections to current direction belong in the active roadmap or a
new feature.
