# Feature Index

This index is the current roadmap entry point. Feature documents retain product
decisions and acceptance boundaries; historical audits remain under
`docs/audit` and do not override a newer feature status.

## Active Roadmap

| Feature | Status | Outcome |
| --- | --- | --- |
| [016](016-configure-global-display-currency.md) | Reopened | Correct named-currency presentation so USD ledger amounts are never relabeled as another denomination without conversion. |
| [017](017-use-a-generic-money-display.md) | Blocked by 016 | Retain the honest generic display while preventing restoration of misleading named-currency presentation. |

Feature 016 is active because currency symbols assert denomination: changing
USD amounts to EUR, CAD, GBP, or AUD symbols without conversion is not truthful
financial presentation. Feature 017 remains coupled to that correction because
its restore path selects Feature 016's named modes. The generic `¤` mode itself
remains an honest currency-neutral presentation.

## Archive

[Archived Features](archive/README.md) contains the closed or complete
specifications for Features 001 through 015 and 018 through 023. Archived
documents are retained as historical records without placeholder files.

## Next Feature Number

Use **Feature 024** for the next approved outcome. New features should state one
user-observable result, the minimum cross-layer responsibilities needed for it,
bounded acceptance criteria, and proportionate validation. They do not require
implementation waves, specialist assignment ceremony, or a separate final audit
unless the risk or the maintainer specifically calls for one.
