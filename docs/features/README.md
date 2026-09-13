# Feature Index

This index is the current roadmap entry point. Feature documents retain product
decisions and acceptance boundaries; historical audits remain under
`docs/audit` and do not override a newer feature status.

## Active Roadmap

| Feature | Status | Outcome |
| --- | --- | --- |
| [024](024-track-recurring-pay-period-income.md) | Proposed | Track recurring pay-period income and allocate each dated receipt exactly across household accounts. |

Feature 024 starts the receive step of the Kakeibo cycle while extending the
calendar-centered record with durable income receipts. Display currency remains an intentional
presentation culture and indicator: changing it never converts ledger values,
and the Settings flow requires acknowledgement of that behavior before saving.

## Archive

[Archived Features](archive/README.md) contains the closed or complete
specifications for Features 001 through 023. Archived
documents are retained as historical records without placeholder files.

## Next Feature Number

Use **Feature 025** for the next approved outcome. New features should state one
user-observable result, the minimum cross-layer responsibilities needed for it,
bounded acceptance criteria, and proportionate validation. They do not require
implementation waves, specialist assignment ceremony, or a separate final audit
unless the risk or the maintainer specifically calls for one.
