# Feature Index

This index is the current roadmap entry point. Feature documents retain product
decisions and acceptance boundaries; historical audits remain under
`docs/audit` and do not override a newer feature status.

## Active Roadmap

| Feature | Status | Outcome |
| --- | --- | --- |
| [005](005-record-a-transaction-for-selected-day.md) | Implemented; external validation pending | Select a calendar date, record one USD expense, persist it, and reread that day. |
| [007](007-correct-or-remove-selected-day-transaction.md) | Planned | Correct or permanently remove a selected-day transaction. |
| [009](009-themeable-design-system-and-workbench-dark-theme.md) | Planned | Establish a custom visual and three-pane layout token system with the default `Workbench Dark` theme. |

Feature 005 still needs its configured real-PostgreSQL test and hosted browser
journey. Those environment checks do not block planning later independent
features, but they must pass before Feature 005 is marked complete.

## Archive

[Archived Features](archive/README.md) contains the complete specifications for
Features 001 through 004, 006, and 008. Archived documents are retained as
historical records without placeholder files or link-maintenance requirements.

## Next Feature Number

Use **Feature 009** for the next approved outcome. New features should state one
user-observable result, the minimum cross-layer responsibilities needed for it,
bounded acceptance criteria, and proportionate validation. They do not require
implementation waves, specialist assignment ceremony, or a separate final audit
unless the risk or the maintainer specifically calls for one.
