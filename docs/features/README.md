# Feature Index

This index is the current roadmap entry point. Feature documents retain product
decisions and acceptance boundaries; historical audits remain under
`docs/audit` and do not override a newer feature status.

## Active Roadmap

| Feature | Status | Outcome |
| --- | --- | --- |
| [025](025-align-account-subnavigation.md) | Proposed | Align expanded account navigation with its parent destination. |
| [028](028-top-align-collapsed-workspace-navigation.md) | Proposed | Keep collapsed workspace-navigation destinations at the top of the rail. |
| [029](029-establish-performance-benchmark-suite.md) | Proposed | Measure and protect production performance and memory efficiency with repeatable benchmarks. |
| [030](030-normalize-calendar-journey-line-endings.md) | Proposed | Restore formatting-gate compliance for the calendar browser journey source. |
| [031](031-normalize-generated-migration-encoding.md) | Proposed | Restore formatting-gate compliance for generated migration source encodings. |
| [032](032-complete-selected-date-inspector-test-fixture.md) | Proposed | Restore the selected-date inspector component behavior proof. |

[Completed Feature 024](archive/024-plan-and-review-a-month-with-intention.md)
centers a deliberate monthly plan and review. Schedules may help
anticipate income, but only user-confirmed receipts count as actuals. Display
currency remains an intentional presentation culture and indicator: changing
it never converts ledger values, and the Settings flow requires acknowledgement
of that behavior before saving.

## Archive

[Archived Features](archive/README.md) contains the closed or complete
specifications for Features 001 through 024, 026-027, and 033-037. Archived
documents are retained as historical records without placeholder files.

## Ideology Audit: Remaining Roadmap (2026-10-04)

The core loop is **intend, record what actually happened, and reflect**.
Calendar and account UI should make that loop easier, not compete with it.
Schedules remain optional expectation aids; no proposal may automatically
confirm income, imply savings, or replace a user's budgeting decisions.

The remaining proposals are supporting usability and maintenance work, not
missing Kakeibo practices. They should not become prerequisites for using the
already implemented monthly planning and reflection flow.

| Feature | Fit with the core | Recommendation and evidence |
| --- | --- | --- |
| [025](025-align-account-subnavigation.md) | Indirect: easier access to expense context. | Keep as low-priority polish. Current account subnavigation still has an extra indent. Preserve visible parent/child grouping even when aligning edges. |
| [028](028-top-align-collapsed-workspace-navigation.md) | Indirect: predictable access to Calendar and Plan and review. | Keep as a small usability slice ahead of decorative polish. Collapsed desktop navigation still centers vertically; narrow-layout rules differ and need their own check. |
| [029](029-establish-performance-benchmark-suite.md) | Conditional: responsiveness helps manual recording and review. | Defer the broad suite until a measured problem or release need justifies it. Start with one observed bottleneck; do not require baselines and profiling for every feature. Replace obsolete automatic receipt-materialization scope with deliberate confirmation. |
| [030](030-normalize-calendar-journey-line-endings.md) | Enabling maintenance, not product functionality. | Keep in a maintenance batch. Read-only inspection still finds LF rather than the configured CRLF in this file; prove with a focused formatter check before changing it. |
| [031](031-normalize-generated-migration-encoding.md) | Enabling reliable schema maintenance. | Keep in the same maintenance batch, preserving migration semantics. Inspection finds UTF-8 BOMs; identify actual formatter failures before normalization. Successful database migrations do not prove encoding compliance. |
| [032](032-complete-selected-date-inspector-test-fixture.md) | Enabling confidence in calendar-centered entry. | Reassess the stale failure report rather than blindly repairing it. The selected-date reveal test and Client suite have passed and the clock is registered, but it is still `TimeProvider.System`; a deterministic clock is needed to meet the proposal's full acceptance scope. |

Suggested order: resolve focused maintenance evidence (030-032), improve
navigation predictability (028), then consider alignment polish (025).
Keep 029 deferred and demand-driven. This is prioritization guidance, not
authorization to implement or close the remaining proposals.

The audit used current specifications, navigation CSS, component-test fixture
source, and read-only encoding checks. It did not run a new formatter, browser,
or test suite, and it does not claim all acceptance criteria are verified.

## Next Feature Number

Use **Feature 038** for the next approved outcome. New features should state one
user-observable result, the minimum cross-layer responsibilities needed for it,
bounded acceptance criteria, and proportionate validation. They do not require
implementation waves, specialist assignment ceremony, or a separate final audit
unless the risk or the maintainer specifically calls for one.
