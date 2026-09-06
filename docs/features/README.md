# Feature Index

This index is the current roadmap entry point. Feature documents retain product
decisions and acceptance boundaries; historical audits remain under
`docs/audit` and do not override a newer feature status.

## Active Roadmap

| Feature | Status | Outcome |
| --- | --- | --- |
| [010](010-calendar-daily-expense-amounts.md) | Complete | Show backend-calculated all-account daily expense amounts on the month calendar. |
| [011](011-add-a-named-account.md) | Proposed | Add Home/Accounts navigation and an account-card gallery with a creation card. |
| [012](012-require-an-account-for-every-transaction.md) | Complete | Require a persisted account in every selected-day transaction create and edit workflow. |
| [013](013-view-an-accounts-transactions.md) | Proposed | Select an account card and view its complete transaction history. |

The features are ordered so each delivers one observable result. Feature 010
can complete independently; Feature 012 depends on Feature 011 and preserves
Feature 010's all-account summary meaning; Feature 013 depends on mandatory
transaction ownership from Feature 012.

## Archive

[Archived Features](archive/README.md) contains the complete specifications for
Features 001 through 009. Archived documents are retained as
historical records without placeholder files or link-maintenance requirements.

## Next Feature Number

Use **Feature 014** for the next approved outcome. New features should state one
user-observable result, the minimum cross-layer responsibilities needed for it,
bounded acceptance criteria, and proportionate validation. They do not require
implementation waves, specialist assignment ceremony, or a separate final audit
unless the risk or the maintainer specifically calls for one.
