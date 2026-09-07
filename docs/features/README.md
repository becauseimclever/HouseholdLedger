# Feature Index

This index is the current roadmap entry point. Feature documents retain product
decisions and acceptance boundaries; historical audits remain under
`docs/audit` and do not override a newer feature status.

## Active Roadmap

| Feature | Status | Outcome |
| --- | --- | --- |
| [010](010-calendar-daily-expense-amounts.md) | Complete | Show backend-calculated all-account daily expense amounts on the month calendar. |
| [011](011-add-a-named-account.md) | Complete | Add Home/Accounts navigation and an account-card gallery with a creation card. |
| [012](012-require-an-account-for-every-transaction.md) | Complete | Require a persisted account in every selected-day transaction create and edit workflow. |
| [013](013-view-an-accounts-transactions.md) | Complete | Select an account card and view its complete transaction history. |
| [014](014-expand-accounts-in-workspace-navigation.md) | Complete | Expand Accounts in the left navigation and follow persisted account links directly to their transaction lists. |
| [015](015-filter-and-search-account-transactions.md) | Complete | Filter and search one account's transactions through addressable backend-owned criteria. |
| [016](016-configure-global-display-currency.md) | Complete | Persist a global named display currency from Settings and apply it to every monetary value. |
| [017](017-use-a-generic-money-display.md) | Complete | Choose a durable no-currency mode that displays all monetary values with the generic currency sign. |
| [018](018-present-navigation-as-workspace-tiles.md) | Complete | Present native destinations as themed navigation tiles and make the HouseholdLedger name a Home link. |
| [019](019-collapse-navigation-from-its-pane.md) | Complete | Replace the toolbar hamburger with a pane-owned arrow and a reachable collapsed navigation rail. |
| [020](020-select-a-workbench-light-theme.md) | Complete | Persistently switch the application between Workbench Dark and a VS Code-inspired Workbench Light theme. |
| [021](021-present-a-themed-application-footer-and-links.md) | Complete | Add copyright and a styled notices destination to the footer and give every shipped link an intentional themed role. |
| [022](022-collapse-and-reveal-the-inspector-pane.md) | Complete | Give the inspector a pane-owned collapse arrow and automatically reveal it when a calendar date is selected. |
| [023](023-navigate-from-the-collapsed-icon-rail.md) | Complete | Give every navigation destination an icon and preserve usable current-location navigation in the collapsed rail. |

The features are ordered so each delivers one observable result. Feature 014
reuses the account catalog and account-detail route; Feature 015 enhances that
existing transaction list. Feature 016 establishes global settings persistence
and named-currency formatting before Feature 017 adds the distinct generic
money-display option. Features 018 and 019 refine the navigation presentation
and collapse behavior. Feature 020 then uses Feature 016's settings lifecycle
and Feature 009's token contract to ship the first selectable second theme.
Feature 021 completes the shared chrome with a themed legal footer and
application-wide link presentation contract. Feature 022 mirrors the
navigation collapse pattern for the inspector while preserving selected-date
driven automatic reveal. Feature 023 extends the collapsed navigation rail
with icon links and persistent current-location context.

## Archive

[Archived Features](archive/README.md) contains the complete specifications for
Features 001 through 009. Archived documents are retained as
historical records without placeholder files or link-maintenance requirements.

## Next Feature Number

Use **Feature 024** for the next approved outcome. New features should state one
user-observable result, the minimum cross-layer responsibilities needed for it,
bounded acceptance criteria, and proportionate validation. They do not require
implementation waves, specialist assignment ceremony, or a separate final audit
unless the risk or the maintainer specifically calls for one.
