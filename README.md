# HouseholdLedger

HouseholdLedger is a calendar-centered household ledger informed by Kakeibo.
Kakeibo is a Japanese approach to household accounting that combines recording
money with planning and reflection. The application aims to make those moments
clear without judging a household's choices or assuming its financial goals.

## Intentional Household Budgeting

Use the calendar to record expenses in four Kakeibo-inspired classifications:
Necessities, Optional, Culture, and Unexpected. Named accounts describe where
money is held, not what it is intended for.

Plan expected monthly income across intended savings and those classifications.
Confirm income only after receiving it, then compare the plan with recorded
actuals. Pay schedules offer editable expectations; they never record receipts,
move money, or change a plan automatically.

Correct or remove mistaken receipts, add optional expense descriptions, and
save what worked plus one intention for next month. Reflection does not require
a completed financial plan. Unsaved monthly intentions have navigation warnings,
and unchanged confirmation retries cannot create duplicate receipts.

The product is a ledger, not a bank-balance or transfer system. Intended savings
is a target, and an unallocated remainder is not evidence of savings achieved.
Changing the display currency changes formatting only, never amount values.

## Technology

- .NET 10 and C# 14
- Standalone Blazor WebAssembly
- ASP.NET Core MVC and OpenAPI
- EF Core, Npgsql, and PostgreSQL at the Infrastructure boundary
- xUnit v3 and bUnit tests

The API references, publishes, and serves the Blazor WebAssembly Client as one
hosted application. Any replacement frontend can consume the HTTP/OpenAPI
contract without referencing server implementation assemblies.
Real PostgreSQL integration tests and the package-free Firefox/W3C browser
journeys validate persistence and user workflows in isolated test databases.
Pinned Firefox and geckodriver hashes are enforced. Automated tests must never
use the persistent manual-development database.

## Start Contributing

1. Read [Development Setup](docs/development/setup.md).
2. Review the [Architecture Overview](docs/architecture/overview.md).
3. Run the appropriate layers in [Testing](docs/development/testing.md).
4. Follow the [Contribution Guide](docs/development/contributing.md).

Dependency changes use the proportionate checks in
[Dependency Governance](docs/development/dependency-governance.md). Browser E2E
approval, runtime review, and residual observability limit are recorded in the
[Browser E2E Dependency Review](docs/development/browser-e2e-dependency-review.md).

For known setup failures, see
[Troubleshooting](docs/development/troubleshooting.md). The approved scope and
current outcomes are recorded in the [Feature Index](docs/features/README.md).
