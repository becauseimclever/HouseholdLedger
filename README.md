# HouseholdLedger

HouseholdLedger is a calendar-centered household ledger informed by Kakeibo.
Kakeibo is a Japanese approach to household accounting that combines recording
money with planning and reflection. The application aims to make those moments
clear without judging a household's choices or assuming its financial goals.

The repository currently contains an intentionally small application scaffold,
not a usable ledger. It establishes a standalone Blazor WebAssembly client, an
ASP.NET Core MVC API, a language-neutral OpenAPI contract, and a PostgreSQL
persistence boundary. Entries, budgets, categories, savings, and reflections
are not implemented yet.

## Technology

- .NET 10 and C# 14
- Standalone Blazor WebAssembly
- ASP.NET Core MVC and OpenAPI
- EF Core, Npgsql, and PostgreSQL at the Infrastructure boundary
- xUnit v3 and bUnit tests

The API references, publishes, and serves the Blazor WebAssembly Client as one
hosted application. Any replacement frontend can consume the HTTP/OpenAPI
contract without referencing server implementation assemblies.
The package-free direct-W3C browser path is approved and verified: pinned
Firefox and geckodriver hashes are enforced, and two fresh hardened runs passed
all three E2E cases. The real PostgreSQL test remains blocked because Podman
5.8.3 cannot start its engine until an administrator upgrades WSL to 2.7.11.

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
completion gates are recorded in
[Feature 001: Application Scaffolding](docs/features/001-application-scaffolding.md).
