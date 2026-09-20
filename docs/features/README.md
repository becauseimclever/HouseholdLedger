# Feature Index

This index is the current roadmap entry point. Feature documents retain product
decisions and acceptance boundaries; historical audits remain under
`docs/audit` and do not override a newer feature status.

## Active Roadmap

| Feature | Status | Outcome |
| --- | --- | --- |
| [024](024-track-recurring-pay-period-income.md) | Proposed | Track recurring pay-period income and allocate each dated receipt exactly across household accounts. |
| [025](025-align-account-subnavigation.md) | Proposed | Align expanded account navigation with its parent destination. |
| [026](026-minimize-empty-day-inspector.md) | Proposed | Keep an otherwise empty day inspector focused on recording an expense. |
| [027](027-focus-account-detail-gallery.md) | Proposed | Show only the selected account card when viewing an account detail route. |
| [028](028-top-align-collapsed-workspace-navigation.md) | Proposed | Keep collapsed workspace-navigation destinations at the top of the rail. |
| [029](029-establish-performance-benchmark-suite.md) | Proposed | Measure and protect production performance and memory efficiency with repeatable benchmarks. |
| [030](030-normalize-calendar-journey-line-endings.md) | Proposed | Restore formatting-gate compliance for the calendar browser journey source. |
| [031](031-normalize-generated-migration-encoding.md) | Proposed | Restore formatting-gate compliance for generated migration source encodings. |
| [032](032-complete-selected-date-inspector-test-fixture.md) | Proposed | Restore the selected-date inspector component behavior proof. |

Feature 024 starts the receive step of the Kakeibo cycle while extending the
calendar-centered record with durable income receipts. Display currency remains an intentional
presentation culture and indicator: changing it never converts ledger values,
and the Settings flow requires acknowledgement of that behavior before saving.

## Archive

[Archived Features](archive/README.md) contains the closed or complete
specifications for Features 001 through 023. Archived
documents are retained as historical records without placeholder files.

## Next Feature Number

Use **Feature 033** for the next approved outcome. New features should state one
user-observable result, the minimum cross-layer responsibilities needed for it,
bounded acceptance criteria, and proportionate validation. They do not require
implementation waves, specialist assignment ceremony, or a separate final audit
unless the risk or the maintainer specifically calls for one.
