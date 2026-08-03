# Feature 001: Application Scaffolding

## Status

Status: Approved / All Six Waves Approved / Wave 1 Complete

- Approval authority: the user. The orchestrator cannot approve this document
  on the user's behalf.
- Approval record: the user approved the complete Feature 001 specification and
  implementation of all six waves on 2026-08-02.
- Implementation readiness: Wave 1 is completed and validated. Waves 2-6 are
  approved to proceed in dependency order without another user approval gate;
  all documented scope, exclusive ownership, resource isolation, validation,
  and Definition of Done requirements remain binding.
- Scope type: foundational application scaffold; no household-ledger feature
  behavior is included.
- Research date: 2026-08-02.

## Context and Problem

HouseholdLedger is a from-scratch successor to Budget Experiment. It needs a
small, current .NET and Blazor foundation that gives Kakeibo and the
calendar-centered household ledger clear architectural homes without importing
the reference application's accumulated architecture or feature set.

Kakeibo is a Japanese household-accounting practice centered on recording what
comes in, deciding what to save, understanding spending, and reflecting on how
to improve. HouseholdLedger treats time as part of that practice: days, weeks,
months, intentions, entries, and reflections must remain explicit domain
concepts rather than becoming UI-only date filters.

The built-in Blazor interface is one client of the product, not the product's
server-side boundary. Users and hosters must be able to replace it with a custom
web, mobile, desktop, or automation client without referencing API,
Infrastructure, Application, or Domain implementation assemblies. The core
application is therefore exposed through an ASP.NET Core MVC HTTP API with a
language-neutral, published contract.

The reference repository proves that .NET, Blazor, ASP.NET Core MVC, EF Core,
PostgreSQL, real-database tests, component tests, and browser tests can work
together. It also demonstrates the cost of broad shared projects, implicit
client/server coupling, mapping and mediator frameworks, fixture-generation
libraries, and package families added before a requirement needs them. The new
scaffold retains validated boundaries and test lessons while implementing them
afresh under stricter dependency rules.

## Desired Outcome

A new contributor can restore, build, test, and run an intentionally empty
HouseholdLedger system from documented commands. The system consists of an
independently compiled Blazor WebAssembly client and an ASP.NET Core MVC API.
The client reaches all server capabilities through HTTP contracts that any
frontend can implement. The API exposes a health endpoint and a persistence
boundary without implementing ledger, budgeting, import, authentication,
reporting, or reflection behavior.

The scaffold makes future ownership clear:

- Kakeibo and calendar rules belong to the domain and application core.
- HTTP transport and contract conformance belong to ASP.NET MVC controllers.
- Language-neutral API description is the compatibility boundary for all
  clients.
- PostgreSQL and EF Core belong to infrastructure.
- Razor markup, client state, accessibility, and HTTP consumption belong to the
  replaceable Blazor WebAssembly client.
- Tests are placed by the boundary and confidence they exercise.

## Goals

1. Establish a reproducible .NET 10 LTS and C# 14 baseline.
2. Use a standalone Blazor WebAssembly client with no server-side rendering or
  interactive server circuit.
3. Expose core application capabilities only through an ASP.NET Core MVC API.
4. Make the built-in client replaceable without references to server
  implementation assemblies or reliance on private server behavior.
5. Establish a language-neutral OpenAPI contract and a narrow .NET transport
  contract assembly that cannot leak domain or server types.
6. Establish inward dependency direction with a framework-independent domain.
7. Reserve clean domain and use-case homes for Kakeibo and calendar concepts.
8. Establish EF Core and PostgreSQL behind application-owned ports under the
  approved narrow PostgreSQL-license exception.
9. Add StyleCop Analyzers from the first build and enforce centralized code
  style with warnings treated as errors.
10. Admit only necessary, free, open-source dependencies whose licenses and
   commercial models satisfy the approved policy.
11. Establish strict Razor code-behind pairing for every page and component.
12. Establish a practical test pyramid using explicit builders and factories.
13. Make local setup and all scaffold verification repeatable from the CLI.
14. Preserve exclusive specialist ownership, resource isolation, wave ordering,
  approval records, and validation during implementation.

## Non-Goals

- Implementing transactions, accounts, categories, budgets, savings goals,
  recurring entries, imports, reports, reflections, or calendar behavior.
- Defining a complete Kakeibo model. This scaffold only reserves its ownership
  and prevents framework leakage into it.
- Porting source, tests, migrations, configuration, or architecture from Budget
  Experiment.
- Backward compatibility with Budget Experiment data, APIs, URLs, or behavior.
- Selecting or implementing authentication, authorization, tenancy, encryption,
  observability vendors, deployment targets, localization, or offline writes.
- Adding charting, CSV, AI, scheduling, resilience, or performance packages.
- Treating the built-in Blazor client, a .NET contract assembly, or generated
  .NET client as the only supported integration path.
- Server-side rendering, Interactive Server, Interactive Auto, SignalR UI
  circuits, or direct UI access to server services.
- Coupling API deployment to the built-in client. Same-origin static hosting may
  be added later only if API and frontend remain independently replaceable.
- Adding AutoFixture, AutoMapper, MediatR, a generic repository, or a repository
  per entity.
- Applying migrations automatically during normal application startup.
- Establishing coverage percentage targets before meaningful product behavior
  exists.

## Reference-Repository Lessons

### Retained

| Lesson | Successor decision |
| --- | --- |
| Keep business rules independent of UI and storage | A dependency-free Domain project and an Application project own rules, use cases, and ports. |
| Use ASP.NET MVC controllers for HTTP APIs | Controllers derive from `ControllerBase`, use `[ApiController]`, and stay transport-only. |
| Use PostgreSQL through EF Core and Npgsql | Provider code, mappings, migrations, and adapters live only in Infrastructure. |
| Test provider behavior against PostgreSQL | Infrastructure and API integration tests use disposable PostgreSQL, not EF InMemory. |
| Use bUnit for focused component behavior | Component tests form the UI layer below browser tests. |
| Keep browser tests to critical journeys | E2E remains the smallest layer and uses only an approved browser/tool chain. |
| Treat warnings, nullability, package auditing, and formatting as build concerns | Shared build settings apply consistently from the repository root. |
| Keep secrets out of tracked configuration | Local connection strings use user secrets or environment variables. |

### Rejected or Deferred

| Reference choice | Successor decision |
| --- | --- |
| Broad Domain, Application, Infrastructure, API, Client, Contracts, and Shared layering | Use six focused production projects; omit a general Shared project and enforce the narrow API Contracts boundary. |
| Server-rendered UI as the default | Use standalone Blazor WebAssembly and keep API deployment independent of the built-in client. |
| Shared enums and DTOs as broad cross-layer projects | Keep domain language in Domain and transport models beside the owning MVC boundary. |
| Large initial package set | Install only packages needed to build and verify the scaffold. |
| EF InMemory for API or persistence confidence | Use PostgreSQL for translated queries, constraints, transactions, and migrations. |
| Automatic migration execution at app startup | Keep migration creation and application explicit in developer and deployment workflows. |
| Early API versioning, Scalar, OpenTelemetry, Serilog, charting, AI, import, and performance packages | Defer each until an approved feature establishes the need and acceptance evidence. |

## Framework Recommendation

### Stable Versus LTS

As of 2026-08-02, .NET 10 is both the latest generally available .NET release
and an LTS release. Microsoft lists .NET 10 as active, released 2025-11-11, and
supported through 2028-11-14. The latest servicing release is 10.0.10, published
2026-07-14. .NET 11 Preview 6 exists, but preview releases are not the latest
stable framework and are not appropriate for this baseline.

**Decision:** target `net10.0`, use C# 14, and pin SDK `10.0.302` in
`global.json` with latest-patch roll-forward limited to its feature band. SDK
`10.0.302` is the newest SDK shown on the official .NET 10 download
page; `10.0.110` is also current for its older feature band. The pin controls
build reproducibility, while the application remains on the .NET 10 LTS runtime
line.

All Microsoft runtime-aligned packages must use the same latest stable 10.0.x
patch, initially 10.0.10. Supported operation requires staying current with
servicing patches; the scaffold must not freeze 10.0.10 for the life of .NET 10.

Do not adopt .NET 11 before its general-availability release and a separately
approved upgrade. Latest preview is not equivalent to latest stable.

### Blazor Hosting Recommendation

Use the .NET 10 `blazorwasm` template to create an independently compiled
standalone Blazor WebAssembly client. Run the ASP.NET Core MVC API as a separate
project and process during development. Configure the client API base address,
API CORS policy, and local HTTPS origins explicitly.

This is preferred over a Blazor Web App hosted arrangement because frontend
replaceability is a controlling requirement. A Blazor Web App with WebAssembly
rendering still makes its server project the normal run and deployment entry
point. A standalone client makes the replaceability test direct: it can build,
publish, and run without API implementation projects, while the API can build,
publish, and serve custom clients without the built-in Client assembly.

An optional same-origin deployment may later copy published client static
assets behind the API host or another static server. That packaging convenience
must not create a project reference from Client to API implementation, make the
API depend on Client startup, hide the OpenAPI contract, or prevent a hoster from
omitting and replacing the built-in UI. It is deferred from this scaffold.

## Proposed Repository Structure

```text
HouseholdLedger/
|-- HouseholdLedger.slnx
|-- Directory.Build.props
|-- Directory.Packages.props
|-- global.json
|-- .editorconfig
|-- stylecop.json
|-- README.md
|-- docs/
|   |-- architecture/
|   |-- development/
|   `-- features/
|       `-- 001-application-scaffolding.md
|-- src/
|   |-- HouseholdLedger.Domain/
|   |-- HouseholdLedger.Application/
|   |-- HouseholdLedger.Infrastructure/
|   |-- HouseholdLedger.Api.Contracts/
|   |-- HouseholdLedger.Api/
|   `-- HouseholdLedger.Client/
`-- tests/
    |-- HouseholdLedger.Domain.UnitTests/
    |-- HouseholdLedger.Application.UnitTests/
  |-- HouseholdLedger.Api.Contracts.Tests/
  |-- HouseholdLedger.Client.ComponentTests/
    |-- HouseholdLedger.Infrastructure.IntegrationTests/
  |-- HouseholdLedger.Api.IntegrationTests/
    `-- HouseholdLedger.EndToEndTests/
```

The approved shape contains six production projects and seven test projects.
Use the modern XML solution format as `HouseholdLedger.slnx`, supported by the
selected .NET SDK. Do not also create a classic `.sln`.

### Domain

`HouseholdLedger.Domain` owns entities, value objects, aggregates, policies,
invariants, calculations, domain events, and domain services. It has no project
references and no references to ASP.NET Core, Blazor, EF Core, Npgsql,
serialization, or deployment packages.

Create empty namespace folders only when they communicate an immediate boundary;
do not generate placeholder types. Future feature slices should group code by
domain capability rather than by generic technical type.

The first domain architecture note must reserve language for:

- household ledger entries associated with explicit calendar dates;
- accounting periods and boundaries such as day, week, and month;
- money and currency correctness;
- Kakeibo intentions, spending classifications, and reflections;
- time-zone and locale decisions where date meaning depends on them.

This reservation is not permission to finalize those models in the scaffold.

### Application

`HouseholdLedger.Application` references Domain. It owns use cases, commands and
queries, orchestration, application policies, and ports consumed by those use
cases. Persistence and external-service interfaces are defined here when an
approved use case needs them.

Application remains independent of ASP.NET Core, Blazor, EF Core, Npgsql, and
provider-specific models. It does not expose `IQueryable`, `DbContext`, HTTP
types, or Razor types.

Use cases are invoked directly through explicit application interfaces or
concrete services registered with built-in dependency injection. Do not add a
mediator library merely to move a method call behind indirection.

### Infrastructure

`HouseholdLedger.Infrastructure` references Application and Domain. It owns:

- `DbContext` and design-time context creation;
- EF Core entity configurations and PostgreSQL-specific mappings;
- migrations and migration metadata;
- implementations of application-owned persistence ports;
- provider registration extensions;
- explicit transaction and concurrency behavior.

Official records declare Npgsql and PostgreSQL under the `PostgreSQL` license,
which remains outside the general allowlist. The user has approved a narrow
standing exception only for the PostgreSQL licenses required by PostgreSQL
server, Npgsql, and `Npgsql.EntityFrameworkCore.PostgreSQL`. This exception does
not admit unrelated software under the same license or waive provenance,
free/open-source, transitive/runtime, vulnerability, or no-paid-option review.

The scaffold may include an empty `HouseholdLedgerDbContext`, but the initial
migration is deferred until a real model exists. It must not invent ledger
tables. Verify the empty context and provider connectivity with a connection
smoke test.

Connection strings are supplied at the API composition root. Infrastructure
must not read user secrets directly or contain credentials.

### API Contracts

`HouseholdLedger.Api.Contracts` is the only production project shared with the
built-in client. It references no other HouseholdLedger project and contains
only public transport DTOs, enums, pagination/error metadata, and serialization
attributes proven necessary by an API contract. It contains no domain entities,
application interfaces, controllers, EF types, business rules, DI registration,
or server implementation helpers.

The assembly is a convenience for .NET clients, not the canonical cross-language
contract. The canonical contract is checked, published OpenAPI generated from
controller behavior. Custom frontends may consume HTTP and OpenAPI without
loading any HouseholdLedger assembly. Contract tests detect drift between
controller responses, API Contracts types, and the OpenAPI artifact.

### API

`HouseholdLedger.Api` is the server composition root. It references Application,
Infrastructure, and API Contracts, but not Client. It owns MVC controllers,
HTTP mapping, middleware, CORS, OpenAPI generation, configuration, and startup.
It must build, test, publish, and run when Client is absent from the invocation.

Suggested ownership:

```text
HouseholdLedger.Api/
|-- Controllers/
|-- Configuration/
`-- Program.cs
```

Controllers map explicitly between contract and application types. Do not use
AutoMapper. Repeated mapping may earn a small local mapper only when its benefit
is demonstrated and it remains explicit and dependency-free.

### Client

`HouseholdLedger.Client` is a standalone Blazor WebAssembly project. It
references API Contracts only. It does not reference Domain, Application,
Infrastructure, or API. It owns Razor components, presentation state,
accessibility, API-client adapters, and client startup.

The client receives its API base URL from environment-specific static
configuration. It uses `HttpClient` and built-in JSON support unless a proven
requirement justifies another dependency. It publishes as independently
hostable static assets. Its first page is a restrained, accessible
calendar-centered shell that does not simulate nonexistent financial data.

## Dependency Direction and Specialist Ownership

```text
Domain <- Application <- Infrastructure
             ^                ^
             |                |
             +-------------- API -> API Contracts <- Client
                                ^
                                |
                     custom clients via HTTP/OpenAPI
```

An arrow points to a referenced project. Domain references nothing. Application
references Domain. Infrastructure references Application and Domain. API
references Application, Infrastructure, and API Contracts. Client references
only API Contracts. Custom clients need no .NET project reference.

| Surface | Owning specialist | Boundary |
| --- | --- | --- |
| Domain models and rules; application use cases and ports | Domain and Business Logic | No framework, transport, or provider dependencies. |
| MVC controllers, HTTP mapping, OpenAPI, CORS, middleware, and startup | ASP.NET API | No database access or business-rule implementation. |
| Public DTO assembly and language-neutral API contract | ASP.NET API | No implementation or domain leakage; compatibility review required. |
| WebAssembly components, client state, styling, accessibility, and HTTP adapters | Blazor UI | No server implementation references or business-rule ownership. |
| EF Core, PostgreSQL, mappings, migrations, adapters | Persistence and Integrations | Implements Application ports; no UI or transport ownership. |
| Component, integration, contract, and E2E harnesses and tests | Test Architecture | Production defects return to the production owner. |
| Feature document, final audit, and engineering documentation | Research and Documentation | No production or test implementation. |
| Shared project files, package manifests, solution, CI, and unowned tooling | Utility Fallback | Sequential ownership of shared resources only. |

`Program.cs`, project files, `.editorconfig`, `stylecop.json`, `Directory.Build.props`,
`Directory.Packages.props`, `global.json`, and the solution are shared resources.
The orchestrator must assign each to exactly one specialist at a time. No
parallel wave may grant overlapping writes.

## ASP.NET MVC Controller Boundary

- Register controllers with `AddControllers` and map them with `MapControllers`.
- API controllers derive from `ControllerBase`, use `[ApiController]`, and use
  explicit versioned routes below `/api/v1`.
- Controllers validate transport shape, map transport models, invoke one
  application use case, honor cancellation, and map outcomes to HTTP responses.
- Controllers do not query `DbContext`, contain Kakeibo calculations, or expose
  EF entities.
- Use framework `ProblemDetails` and `ValidationProblemDetails` for error shape.
- The scaffold includes `GET /api/v1/health`
  implemented through a controller. It proves routing without inventing domain
  behavior.
- Generate and check an OpenAPI document from controller metadata. Do not add a
  third-party interactive API UI in this scaffold.
- Treat additive and breaking contract changes deliberately. The initial URL
  major is `v1`; breaking changes require a new major route and an approved
  compatibility decision. Nonbreaking changes update the v1 OpenAPI artifact.

Microsoft recommends `ControllerBase` rather than `Controller` for web APIs and
documents `[ApiController]` behavior including attribute routing, automatic 400
responses, binding inference, and problem details.

## Blazor and Code-Behind Rules

- Use standalone Blazor WebAssembly; do not configure Interactive Server,
  Interactive Auto, server prerendering, or a SignalR UI circuit.
- Every `.razor` page and component has a same-directory `.razor.cs` partial
  class with the same component name, including `App`, `Routes`, layouts,
  navigation, error/not-found surfaces, pages, and shared components.
- Razor files contain markup, directives, component composition, and declarative
  binding only. They contain no `@code` blocks.
- Event handlers, lifecycle methods, injected dependencies, parameters, and
  presentation logic live in code-behind.
- Scoped styles use matching `.razor.css` files when component-specific styling
  is needed.
- The client calls only documented API routes through a narrow HTTP adapter.
- The shell has meaningful landmarks, a single page heading, keyboard-operable
  navigation, visible focus, sufficient contrast, a useful document title, and
  understandable loading, API-unavailable, not-found, error, and empty states.
- Calendar semantics must not be reduced to a visual grid. Accessible names and
  text expose the displayed period and empty state without requiring color or
  spatial inference.
- Template demo pages, sample weather data, and counter behavior are removed.

A verification script or test must fail when a matching `.razor.cs` file is
absent, an `@code` block is present, or Client references a forbidden project or
namespace.

## PostgreSQL and EF Core Boundaries

PostgreSQL is the approved relational provider through EF Core. Official package
records declare `Npgsql.EntityFrameworkCore.PostgreSQL` and Npgsql under
`PostgreSQL`, and the database itself uses the PostgreSQL License. Similarity to
MIT does not make that SPDX identifier MIT. The user-approved standing exception
is limited to the PostgreSQL licenses required for PostgreSQL server, Npgsql,
and `Npgsql.EntityFrameworkCore.PostgreSQL`; it does not change the general
allowlist or approve unrelated PostgreSQL-licensed software.

Under that exception:

- EF Core and Npgsql packages exist only in Infrastructure, its integration
  tests, and design-time tooling where required.
- API supplies validated configuration to Infrastructure through a registration
  extension. Domain and Application do not know connection strings or providers.
- Migrations live in Infrastructure and are generated explicitly with
  `dotnet ef`; application startup does not silently mutate schemas.
- Integration tests use an approved, isolated real PostgreSQL resource with
  deterministic cleanup. They do not use Testcontainers.
- Do not use `Microsoft.EntityFrameworkCore.InMemory` for persistence or API
  integration confidence. Do not substitute SQLite for PostgreSQL query tests.
- Persistence adapters return domain/application types, not EF entities or
  `IQueryable`.
- Date, time-zone, money precision, constraints, concurrency, and transaction
  choices are feature decisions and must be proven against PostgreSQL when
  introduced.

Microsoft's EF Core guidance recommends coverage against the production database
system, warns that provider behavior differs, and strongly discourages EF
InMemory as a database fake. This technical lesson does not override dependency
policy.

## Test Pyramid

| Layer | Project | Initial scaffold evidence |
| --- | --- | --- |
| Unit, broadest | Domain.UnitTests and Application.UnitTests | Projects discover and run; no vanity tests or placeholder business assertions. |
| Contract | Api.Contracts.Tests | Serialization and OpenAPI drift checks prove a language-neutral boundary. |
| Component | Client.ComponentTests | The WebAssembly shell renders honest states without a live API. |
| Infrastructure integration | Infrastructure.IntegrationTests | Runs after an approved PostgreSQL provisioning method exists. |
| API integration | Api.IntegrationTests | `WebApplicationFactory` proves startup, MVC health, OpenAPI, CORS, and replacement configuration. |
| End to end, smallest | EndToEndTests | An approved browser tool loads the independently served client against the API. |

Do not use AutoFixture. Tests use readable explicit builders, object mothers
only where appropriate, or focused factory methods that expose meaningful
defaults. Do not hide setup behind reflection-driven specimen generation.

PostgreSQL tests use reviewed Podman provisioning with deterministic isolation
and cleanup; Testcontainers is excluded. Browser mechanisms remain blocked
until the complete automation, browser artifact, and license inventory passes
policy. After that full review, the user approves only the narrow browser-runtime
license exception necessary for E2E execution. Microsoft.Playwright remains
provisional until that evidence exists: its NuGet package is MIT, but downloaded
browser binaries carry separate licenses. Direct NuGet metadata is insufficient.

The Test Architecture specialist owns the final pyramid review and all
component/integration/E2E test implementation. Domain, Application, API, UI,
and Persistence specialists retain their own production behavior and unit-test
ownership as defined by their specialist instructions.

## Package and Version Policy

### Dependency-Minimization Rule

Prefer the .NET and ASP.NET Core shared frameworks, BCL, built-in dependency
injection, `HttpClient`, `System.Text.Json`, explicit mapping, direct use-case
interfaces, and explicit test builders/factories. Do not add AutoFixture,
AutoMapper, or MediatR. This is not a ban on all third-party packages: a necessary
package may be approved when its value, maintenance cost, provenance, complete
license chain, and commercial model satisfy this policy.

### License Allowlist

Only these canonical SPDX identifiers are allowed without a new user decision:

- AGPL: `AGPL-1.0-only`, `AGPL-1.0-or-later`, `AGPL-3.0-only`, and
  `AGPL-3.0-or-later`.
- GPL: `GPL-1.0-only`, `GPL-1.0-or-later`, `GPL-2.0-only`,
  `GPL-2.0-or-later`, `GPL-3.0-only`, and `GPL-3.0-or-later`.
- Apache: `Apache-1.0`, `Apache-1.1`, and `Apache-2.0`.
- MIT: `MIT` and `MIT-0`.

Deprecated SPDX aliases must be normalized to their current `-only` or
`-or-later` forms before evaluation. Other similarly named MIT variants are not
implicitly accepted. LGPL, BSD, PostgreSQL, MPL, ISC, MS-PL, public-domain
dedications, custom `LicenseRef` terms, and every other identifier are blocked
by default even when OSI-approved or permissive.

The user-approved narrow standing exception to this allowlist covers only the
PostgreSQL licenses required for PostgreSQL server, Npgsql, and
`Npgsql.EntityFrameworkCore.PostgreSQL`. It does not cover any unrelated
PostgreSQL-licensed dependency. Each excepted dependency must still pass the
same documented provenance, complete dependency-chain, free/open-source,
vulnerability, and no-paid-option review as every other dependency.

For compound expressions:

- Every required branch of an `AND` expression must be allowlisted.
- For `OR`, a specific allowlisted branch may be selected only when that choice
  is legally available, recorded in the dependency review, and does not conflict
  with the commercial-model rule.
- `WITH` exceptions, `LicenseRef` terms, missing expressions, ambiguous metadata,
  and contradictory package/source licenses require manual review and explicit
  user approval.
- Dual or multi-licensing does not rescue a project that also offers a paid,
  proprietary, commercial, or source-available product option under the rule
  below.

### Free and Open-Source Commercial-Model Rule

Dependencies, tools required to build or test, downloaded runtime assets, and
their upstream projects must be free and open source. Exclude a library or
project that offers a paid, proprietary, commercial edition, commercial tier,
hosted commercial counterpart, or dual commercial licensing, even when the
specific package or community edition is free and uses an allowlisted license.
Donations, grants, and sponsorship alone do not constitute a commercial tier.

Automation can inventory packages, parse declared SPDX expressions, detect
known vulnerabilities, and compare approved metadata. It cannot reliably prove
ownership, provenance, license compatibility, hidden runtime downloads,
trademark terms, or whether an upstream commercial model changed. Every direct
dependency and material transitive/runtime dependency therefore requires a
documented manual review of official package metadata, source repository,
license text, maintainer/publisher, release provenance, commercial offerings,
and downloaded artifacts.

### Build and Supply-Chain Enforcement

1. Use central package management in `Directory.Packages.props`; project files
   name packages without repeating versions.
2. Use stable packages except the explicit StyleCop and one-time Wave 3 bUnit
  exceptions below.
3. Keep Microsoft ASP.NET Core and EF Core packages aligned to the target
   framework servicing patch.
4. Keep all `Microsoft.EntityFrameworkCore.*` packages on exactly the same
   version. Add `Microsoft.EntityFrameworkCore.Relational` directly because
   Microsoft notes independently released providers may lag its patch.
5. Match the Npgsql provider major version to the EF Core major version and
   verify its declared compatibility before every upgrade.
6. Enable NuGet audit for direct and transitive packages and fail the build at
  the user-approved severity threshold.
7. Commit NuGet lock files and package source mapping; enforce locked restore.
8. Produce a direct/transitive dependency inventory and dated manual license,
  provenance, and commercial-model review record.
9. Block approval when a dependency is unreviewed, has a disallowed expression,
  unresolved provenance, or a prohibited commercial model.
10. Review metadata monthly, before release, and on every dependency update.
11. Treat automated classification as evidence for review, not final legal or
   commercial-model determination.
12. Remove packages whose approved purpose is removed.

### StyleCop Prerelease Exception

Use `StyleCop.Analyzers` `1.2.0-beta.556`. Official NuGet and upstream release
records show it is the latest available prerelease as of 2026-08-02; NuGet calls
it prerelease and declares `MIT`. The upstream release is dated 2023-12-20.

This is a deliberate, user-directed exception to the stable-package rule. It is
not permission for other prerelease dependencies. Centralize the version in
`Directory.Packages.props`; inject the analyzer into every C# production and test
project from `Directory.Build.props`; set `PrivateAssets="all"`; and restrict
assets to analyzer/build content so it does not flow into published runtime
output.

Commit root `stylecop.json` and `.editorconfig`. Link `stylecop.json` into every
C# project as an `AdditionalFiles` item. Configure rule behavior in
`stylecop.json`, diagnostic severities in `.editorconfig`, nullable analysis and
`TreatWarningsAsErrors` centrally, and narrow generated-code exclusions without
weakening handwritten code. `dotnet build` must execute analyzers and fail on any
enabled StyleCop diagnostic. Suppressions require a documented rule-specific
rationale; blanket or project-wide suppression is prohibited.

Because beta.556 predates C# 14 and its upstream repository shows C# 12-era test
coverage, Wave 1 must compile representative C# 14 syntax and inspect analyzer
diagnostics. An analyzer crash or material incompatibility blocks implementation;
do not silently downgrade StyleCop or C# or add broad suppressions.

### Wave 3 bUnit Prerelease Exception

On 2026-08-02, the user explicitly approved a one-time exception to the
stable-package rule for Wave 3 to permit `bunit` 2.8.6 and its required
transitive `AngleSharp.Css` 1.0.0-beta.224 dependency. This approval resolves
only the prerelease-policy blocker for that exact direct and transitive version
pair. Adoption remains subject to the existing complete license, provenance,
vulnerability, free/open-source, commercial-model, and no-paid-option checks.

The exception does not permit any other prerelease dependency, automatically
approve a future `bunit` or `AngleSharp.Css` version, waive review of any other
transitive or runtime dependency, or relax the package policy beyond this
one-time Wave 3 use. A version change or different prerelease dependency
requires a new explicit user decision.

### Candidate Baseline as of 2026-08-02

Unless a row records an explicit policy exception, these are candidates rather
than approved dependencies. An exception approves only the stated policy
deviation; versions, full transitive graphs, provenance, licenses, commercial
models, vulnerabilities, and runtime downloads must still be checked
immediately before implementation.

| Package or tool | Candidate / declared license | Reconciliation |
| --- | --- | --- |
| .NET SDK and ASP.NET Core shared framework | 10.0.302 / MIT source baseline | Provisional; verify distributed components and no required proprietary tooling. |
| .NET / ASP.NET Core runtime line | 10.0.10 / MIT source baseline | Provisional; stay on supported servicing patches. |
| `StyleCop.Analyzers` | 1.2.0-beta.556 / MIT | Deliberate prerelease exception; C# 14 probe and complete package review still required. |
| `Microsoft.EntityFrameworkCore` | 10.0.10 / MIT | Provisional after transitive and commercial-model review. |
| `Microsoft.EntityFrameworkCore.Relational` | 10.0.10 / MIT | Provisional; align all Microsoft EF packages exactly. |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.10 / MIT | Provisional; private assets only. |
| `dotnet-ef` | 10.0.10 / MIT | Provisional local tool; align with EF Core. |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 / PostgreSQL | Provisional under the narrow standing license exception; full dependency, FOSS, and no-paid-option review remains required. |
| `Npgsql` | Provider-compatible stable version / PostgreSQL | Provisional under the same narrow exception; version, provenance, transitive/runtime, FOSS, and no-paid-option review remain required. |
| PostgreSQL server | Supported release / PostgreSQL | Provisional under the narrow standing license exception; distribution, provenance, FOSS, and no-paid-option review remain required. |
| `xunit.v3` | 3.2.2 / Apache-2.0 | Provisional; full graph and project model require review. |
| `Microsoft.NET.Test.Sdk` | 18.8.1 / MIT | Provisional after full graph and project-model review. |
| `coverlet.collector` | 10.0.1 / MIT | Include only after complete dependency review, including transitive Mono.Cecil licensing; establish no coverage threshold. |
| `bunit` | 2.8.6 / MIT | One-time Wave 3 stable-policy exception approved; adoption remains gated on all existing reviews and is not approval for future versions. |
| `AngleSharp.Css` | 1.0.0-beta.224 / license review required | Required transitive dependency of the excepted `bunit` version; the same one-time Wave 3 exception resolves only its prerelease status, and all existing reviews remain required. |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.10 / MIT | Provisional after full review. |
| `Microsoft.AspNetCore.OpenApi` | 10.0.10 / MIT | Recommended for canonical contract generation; review build companions separately. |
| `Testcontainers.PostgreSql` | 4.13.0 / MIT | Excluded: official project links to the commercial Testcontainers Cloud service. |
| `Microsoft.Playwright` | 1.61.0 / MIT package | Blocked pending complete automation/browser artifact and license inventory; afterward only the approved narrow browser-runtime license exception may apply. |

AutoFixture, AutoMapper, and MediatR are intentionally absent. No candidate with
`Provisional`, `Blocked`, `Deferred`, or `Excluded` status may be silently
installed.

## Local Developer Workflow

### Prerequisites

- .NET SDK 10.0.302 or a latest-patch SDK in its feature band selected by
  `global.json`.
- Reviewed Podman provisioning for isolated PostgreSQL integration tests;
  Docker Desktop and Testcontainers are not used.
- PowerShell 7 or a shell capable of running equivalent `dotnet` commands.
- An approved browser and automation mechanism only for E2E tests.
- No globally installed `dotnet-ef`; restore it from the local tool manifest.

### Expected Commands

Run from `HouseholdLedger/` after implementation:

```powershell
dotnet --info
dotnet tool restore
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test --no-build
dotnet test tests/HouseholdLedger.Api.Contracts.Tests --no-build
dotnet test tests/HouseholdLedger.Client.ComponentTests --no-build
dotnet test tests/HouseholdLedger.Infrastructure.IntegrationTests
dotnet test tests/HouseholdLedger.Api.IntegrationTests
dotnet test tests/HouseholdLedger.EndToEndTests
dotnet run --project src/HouseholdLedger.Api
dotnet run --project src/HouseholdLedger.Client
```

The implementation must commit NuGet lock files for `--locked-mode`.

The API and Client run on distinct documented HTTPS origins in development.
Client configuration points to the API origin, and API CORS permits only the
documented development client origin. Independent publication must also pass:

```powershell
dotnet publish src/HouseholdLedger.Api --no-restore
dotnet publish src/HouseholdLedger.Client --no-restore
```

Document database secret commands only after the provisioning and configuration
method is approved. Document exact local URLs only after launch settings
allocate and verify them.

## UX States in Scaffold Scope

- **Loading:** the shell communicates that the calendar is loading without
  showing fabricated financial data.
- **Empty:** the calendar area states that no ledger entries exist for the
  displayed period.
- **API unavailable:** the WebAssembly client presents a calm, keyboard and
  screen-reader understandable retry state without implying data loss.
- **Not found:** unknown routes produce an accessible not-found surface.
- **Error:** an unexpected UI failure produces a calm recovery surface without
  leaking technical or financial details.
- **Responsive:** the shell remains usable at narrow and wide viewport sizes;
  headings, navigation, and calendar framing do not overlap.

No task flow, entry form, financial total, or reflection prompt is included.
No disconnected-circuit UI exists because the client does not use Interactive
Server.

## Security and Privacy

- Commit no credentials, connection strings, household data, or realistic
  personal financial samples.
- Default logs and error responses must not expose configuration secrets or
  database details.
- Treat the browser as untrusted. Do not ship secrets, server implementation,
  privileged rules, or trusted validation solely in WebAssembly.
- Validate all API inputs and business rules server-side.
- Restrict CORS to approved origins; do not combine wildcard origins and
  credentials.
- Use HTTPS development configuration supplied by ASP.NET Core.
- Enable package vulnerability auditing for direct and transitive packages.
- The health endpoint reveals only service availability, not environment,
  dependency versions, connection strings, or database contents.
- Authentication and authorization remain unresolved product features; the
  scaffold must not imply that the empty application is production-ready.

## Acceptance Criteria

### AC-01: Reproducible supported baseline and analyzers

Given a clean checkout with the documented prerequisites, the selected SDK is a
stable .NET 10 SDK, every production and test project targets `net10.0`, restore
completes with no prerelease dependency except the documented StyleCop exception
and the exact one-time Wave 3 `bunit`/`AngleSharp.Css` exception, and the solution
builds with no warnings or errors. StyleCop 1.2.0-beta.556 runs for every C#
project from centralized configuration and fails the build on an enabled
violation.

Evidence: `dotnet --version`, `dotnet restore --locked-mode`, and
`dotnet build --no-restore` output; central package/build files; representative
C# 14 analyzer probe; deliberate failing-rule probe followed by a clean build;
and package inventory.

### AC-02: Curated project structure

The repository contains exactly the approved six production projects and seven
test projects. Client references only API Contracts; API does not reference
Client; API Contracts references no HouseholdLedger project; no Budget
Experiment source is copied.

Evidence: solution project listing, directory listing, and source provenance
review.

### AC-03: Enforced dependency direction

Domain has no references; Application references only Domain; Infrastructure
references Application and Domain; API references Application, Infrastructure,
and API Contracts; Client references only API Contracts. No cycle exists, and
transport, Razor, EF, or provider types do not leak across their boundaries.

Evidence: project-reference inspection, package-reference inspection, build,
and architecture-boundary test or equivalent automated check.

### AC-04: Replaceable API/client contract

The API publishes a valid OpenAPI document describing `/api/v1/health`, errors,
content types, and schemas. A client implemented from HTTP/OpenAPI can call the
API without any server implementation assembly. API Contracts contains only
transport concerns, and contract drift checks pass.

Evidence: generated OpenAPI artifact, schema validation, contract tests, and a
small implementation-independent HTTP probe.

### AC-05: Independent WebAssembly client

The standalone Client builds and publishes without building API implementation
projects, runs from static assets, reads a configurable API base URL, and calls
only documented routes. The API builds, publishes, and runs without Client.

Evidence: independent build/publish commands, project graph, static-host smoke
check, and network request inspection.

### AC-06: Honest accessible calendar shell

The Client displays a responsive calendar-centered shell whose empty state
identifies the period without invented entries, totals, savings, or reflections.
Loading, API-unavailable, not-found, and error states are accessible. Template
demo content is absent.

Evidence: component tests and, after browser dependencies are approved, desktop
and mobile browser evidence.

### AC-07: Universal Razor code-behind

Every `.razor` file has a matching `.razor.cs` partial class, no Razor file has
an `@code` block, and lifecycle methods, handlers, parameters, injection, and
presentation logic reside in code-behind.

Evidence: automated pairing/content check plus successful component tests and
build.

### AC-08: MVC API pipeline

`GET /api/v1/health` is served by an `[ApiController]` MVC controller derived from
`ControllerBase`, returns a successful machine-readable response, and discloses
no sensitive configuration. MVC routing, CORS, problem details, and OpenAPI work
through the real host.

Evidence: `WebApplicationFactory` integration test asserting route, status,
content type, and response shape; source review confirms no Minimal API mapping
for this endpoint.

### AC-09: Persistence policy and isolation

PostgreSQL is implemented through EF Core only after PostgreSQL server, Npgsql,
and `Npgsql.EntityFrameworkCore.PostgreSQL` each pass full provenance,
dependency-chain, free/open-source, vulnerability, and no-paid-option review
under the approved narrow standing license exception. EF Core, Npgsql,
`DbContext`, mappings, and migrations are confined to Infrastructure; API
supplies configuration; Domain and Application contain no provider types; no
unrelated PostgreSQL-licensed software is admitted by the exception.

Evidence: the standing-exception approval record, dependency reviews, boundary
checks, and an approved real-PostgreSQL test transcript.

### AC-10: Dependency governance

Every direct, transitive, tool, runtime, downloaded, database, browser, and image
dependency has an inventory entry and documented manual provenance, license,
and commercial-model review. Selected SPDX expressions satisfy the allowlist or
have an exact user exception. Prohibited commercial models and unreviewed
dependencies are absent; vulnerability automation reports its limits.

Evidence: inventories, official-source links, review records, NuGet audit,
restore lock evidence, and published-output inspection.

### AC-11: Explicit implementation patterns and balanced tests

AutoFixture, AutoMapper, and MediatR are absent. Tests use explicit builders or
factories where needed; mapping is explicit; use cases use built-in DI and direct
interfaces. All seven test projects are discoverable. Resource-dependent layers
run only with approved dependencies and do not use excluded substitutes.

Evidence: dependency/source search, focused source review, Test Architecture
classification, and exact test results by layer.

### AC-12: Local workflow is accurate

A contributor following the README/development documentation can restore tools
and packages, build, run fast tests, start API and Client on documented origins,
load the shell, call health, and independently publish both projects without
undocumented setup. Resource-dependent commands do not rely on excluded products.

Evidence: clean-environment command transcript and manual URL verification.

### AC-13: Specialist and resource ownership

Implementation history shows no overlapping writable file ownership, terminal,
port, database, schema, container, browser profile, or generated-output use.
Each wave reports its files, resources, and exact validation results before the
next wave begins.

Evidence: orchestrator wave records and specialist completion reports.

## Implementation Waves and Approval Record

The user approved this feature document and implementation of all six waves on
2026-08-02. Wave 1 is completed and validated. Waves 2-6 may proceed without
another user approval gate, but only in the documented dependency order. Before
each wave, the orchestrator must still record its outcome, participating
specialists, exact writable files, isolated resources, dependencies, risks, and
validation. Approval does not establish implementation completion or satisfy
any acceptance criterion or Definition of Done item by itself.

### Wave 1: Repository, Projects, and Enforced Build Policy

Approval: explicitly approved by the user on 2026-08-02; completed and
validated.

Owner: Utility Fallback, with all shared files under exclusive sequential
ownership.

Create the SDK pin, solution, central build/package policy, `.editorconfig`,
`stylecop.json`, six empty production projects, seven test projects, local tool
manifest if approved, ignores, and project references. Add centralized StyleCop
1.2.0-beta.556 and perform the C# 14 analyzer probe. Add no blocked provider,
browser, or test-container dependency.

Gate evidence: restore, graph, package inventory, license/commercial review,
StyleCop failing/clean probes, independent API/Client builds, and diff proving
Budget Experiment is unchanged.

### Wave 2: Contract-First API Skeleton

Approval: explicitly approved by the user on 2026-08-02; not yet marked
complete.

Owners: ASP.NET API for API Contracts, controller, CORS, OpenAPI, and API startup;
Utility Fallback owns shared project files sequentially when assigned.

Define the narrow health contract, `/api/v1` convention, MVC health controller,
problem details, CORS, and generated OpenAPI artifact. Add contract drift checks.
Do not invent business DTOs or use cases.

Gate evidence: API integration and contract checks, OpenAPI validation,
implementation-independent HTTP probe, and API publish without Client.

### Wave 3: Independent WebAssembly Client Shell

Approval: explicitly approved by the user on 2026-08-02; not yet marked
complete.

Owner: Blazor UI, after the API contract is frozen for this wave.

The user also explicitly approved on 2026-08-02 a one-time stable-package-policy
exception for this wave permitting only `bunit` 2.8.6 and its required
transitive `AngleSharp.Css` 1.0.0-beta.224 dependency. Both remain blocked from
adoption until all existing license, provenance, vulnerability,
free/open-source, commercial-model, and no-paid-option checks pass. This
exception does not mark Wave 3 complete or satisfy any implementation criterion.

Create the standalone Client, explicit HTTP adapter, API base-address
configuration, code-behind components, and accessible scaffold states. Client
must reference only API Contracts and run from static assets against the API.

Gate evidence: complete dependency reviews for `bunit`, `AngleSharp.Css`, and
their dependency closure; reference checks; component tests; independent Client
publish; network inspection; code-behind checks; and accessible state review.

### Wave 4: PostgreSQL Persistence Boundary

Approval: explicitly approved by the user on 2026-08-02; not yet marked
complete.

Owner: Persistence and Integrations. The PostgreSQL license decision is
resolved by the narrow standing exception, and reviewed Podman provisioning is
the approved test method. This wave starts only after the named PostgreSQL
dependencies pass the remaining policy reviews.

Implement the empty EF boundary without fake entities, an initial migration,
automatic startup migration, Testcontainers, EF InMemory, or SQLite
substitution. Defer the initial migration until a real model exists.

Gate evidence: standing-exception decision record, complete dependency reviews,
proof that no unrelated PostgreSQL-licensed software entered through the
exception, boundary checks, reviewed Podman provisioning, and real-provider
test results. Without completed reviews and successful provisioning evidence,
this wave and feature completion remain blocked.

### Wave 5: Remaining Test Pyramid and Developer Documentation

Approval: explicitly approved by the user on 2026-08-02; not yet marked
complete.

Owners: Test Architecture for component/integration/E2E tests and harnesses;
Research and Documentation for assigned development/architecture documentation.

Add only approved component, API, infrastructure, and E2E evidence. Browser E2E
work waits for the complete artifact/license inventory; only then may the narrow
browser-runtime license exception be used. Document separate API/Client startup,
CORS, configuration, independent publishing, and Podman database provisioning.
Parallel work requires disjoint files and resources.

Gate evidence: each test layer's exact command and result, approved browser viewport
evidence, Markdown checks, and clean-workflow transcript.

### Wave 6: Final Verification and Definition of Done Audit

Approval: explicitly approved by the user on 2026-08-02; not yet marked
complete.

Owners: Utility Fallback for broad executable validation, then Research and
Documentation for a read-only criterion-to-evidence audit.

No implementation changes occur during the audit. Any `Not Met`, `Blocked`, or
`Not Verifiable` verdict returns to the owning specialist in a newly approved
repair wave when scope or resources change.

Gate evidence: complete command transcript and every criterion/Definition of
Done item marked `Met` with concrete evidence.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| API Contracts becomes a disguised Shared/domain assembly | Restrict it to transport shapes, enforce no project references, and keep OpenAPI canonical. |
| Built-in client becomes privileged despite API-first intent | Require independent builds, HTTP-only access, CORS, and an implementation-independent contract probe. |
| Optional same-origin packaging recreates server/client coupling | Defer it; if added later, require replaceable deployment composition with no runtime or project dependency. |
| Contract changes break custom frontends | Use `/api/v1`, checked OpenAPI artifacts, drift tests, and explicit breaking-change approval. |
| WebAssembly exposes trusted logic or secrets | Keep validation and business rules server-side; treat client code and configuration as public. |
| Latest package versions drift after this research | Re-check official NuGet pages immediately before implementation and record any approved baseline change. |
| Narrow PostgreSQL exception is mistaken for a general license allowance | Name only PostgreSQL server, Npgsql, and the EF Core provider; reject unrelated PostgreSQL-licensed software and retain every FOSS/no-paid-option review. |
| Direct package license hides transitive/runtime terms | Inventory NuGet closure, tools, images, browsers, and downloaded assets; require manual review. |
| Commercial offerings change after adoption | Re-review official project and product sources monthly, on update, and before release. |
| Automated checks overclaim certainty | Treat them as triage; preserve dated manual evidence and unresolved findings. |
| StyleCop beta predates C# 14 | Run an early syntax/analyzer probe and block rather than silently suppress or downgrade. |
| The Wave 3 exception is read as broad prerelease approval | Pin it to `bunit` 2.8.6 and required transitive `AngleSharp.Css` 1.0.0-beta.224; require a new user decision for any other prerelease or version and retain every existing dependency check. |
| Empty EF model encourages fake entities or meaningless migrations | Permit no placeholder tables; defer the initial migration until a real model exists. |
| Database/browser requirements make the default loop slow | Keep unit/contract/component tests fast and make approved resource-dependent layers explicit. |
| Health endpoint is mistaken for production readiness | Keep response minimal and document that auth, deployment, observability, and operational health are out of scope. |
| Strict warnings on generated migrations create friction | Configure generated-code handling narrowly without weakening warnings for handwritten code. |
| `.slnx` is unsupported by a required user tool | Resolve tooling compatibility before Wave 1 and choose exactly one solution format. |

## Resolved Approval Decisions

On 2026-08-02, the user approved the complete Feature 001 specification and
implementation of all six waves with these decisions:

1. Checked OpenAPI JSON is the canonical language-neutral contract. API
  Contracts is a noncanonical convenience assembly for .NET clients.
2. API routes use `/api/v1` major URL versioning. Additive changes remain in v1;
  breaking changes require a new URL major and compatibility decision.
3. The solution is `HouseholdLedger.slnx`.
4. `global.json` pins SDK 10.0.302 with latest-patch roll-forward in its feature
  band, and NuGet lock files are committed.
5. PostgreSQL tests use reviewed Podman provisioning. Testcontainers and Docker
  Desktop are not used.
6. Browser E2E may use a narrow browser-runtime license exception only after a
  complete automation, artifact, transitive/runtime dependency, provenance,
  license, and commercial-model inventory passes review.
7. Include `coverlet.collector` only after its complete dependency review. This
  feature establishes no coverage threshold.
8. Defer the initial migration until a real model exists.
9. Wave 1 is completed and validated. Waves 2-6 are approved without another
  user approval gate, while their dependency ordering, exclusive ownership,
  resource isolation, validation, and Definition of Done requirements remain
  binding.
10. Wave 3 has a one-time stable-package-policy exception for `bunit` 2.8.6
  and required transitive `AngleSharp.Css` 1.0.0-beta.224 only. The exception
  does not waive any existing dependency check, approve future versions or
  other prereleases, mark Wave 3 complete, or satisfy implementation criteria.

The existing narrow PostgreSQL/Npgsql license exception remains unchanged. No
open approval question remains for Feature 001 or Waves 1-6. Approval does not
mark Waves 2-6 complete or satisfy their implementation evidence requirements.

## Definition of Done

The feature is done only when all items below are evidenced and the final audit
marks every item `Met`.

- [x] The user explicitly approved this document, all recorded decisions, and
  implementation of all six waves on 2026-08-02; decision history records that
  approval.
- [ ] AC-01 through AC-13 are each `Met` with the stated evidence.
- [ ] `dotnet --version` resolves the approved stable .NET 10 SDK policy.
- [ ] `dotnet tool restore` succeeds and restores the approved EF tool version.
- [ ] Deterministic `dotnet restore` succeeds with no package downgrade,
  blocking vulnerability, or prerelease dependency except the documented
  StyleCop 1.2.0-beta.556 exception and the one-time Wave 3 exception for
  `bunit` 2.8.6 with required transitive `AngleSharp.Css`
  1.0.0-beta.224.
- [ ] `dotnet build --no-restore` succeeds with zero warnings and zero errors.
- [ ] StyleCop is centralized, runs for every C# project, passes the C# 14 probe,
  is absent from runtime output, and is enforced by the build.
- [ ] API and Client build and publish independently; API has no Client reference
  and Client has no server implementation reference.
- [ ] Checked OpenAPI, API Contracts, controller behavior, and an
  implementation-independent HTTP probe agree.
- [ ] Unit, contract, and component tests pass without database or browser
  prerequisites.
- [ ] Infrastructure and API integration tests pass against isolated real
  PostgreSQL resources using the excepted provider stack and an approved
  provisioning method.
- [ ] Browser evidence passes only with an approved automation and browser
  dependency chain; otherwise the feature is not done.
- [ ] Automated architecture checks prove the approved project-reference and
  package boundaries.
- [ ] Automated Razor checks prove one `.razor.cs` partner per `.razor` file and
  zero `@code` blocks.
- [ ] AutoFixture, AutoMapper, and MediatR are absent; explicit alternatives are
  verified in source and tests.
- [ ] Every direct, transitive, tool, runtime, database, browser, image, and
  downloaded artifact has dated provenance, license, and commercial-model review.
- [ ] All licenses are allowlisted or covered by an exact user exception;
  prohibited commercial models and unreviewed dependencies are absent.
- [ ] The Wave 3 exception is limited to `bunit` 2.8.6 and required transitive
  `AngleSharp.Css` 1.0.0-beta.224; both and their dependency closure have passed
  all existing license, provenance, vulnerability, free/open-source,
  commercial-model, and no-paid-option checks, and no other prerelease or
  version relies on this exception.
- [ ] The PostgreSQL standing exception covers only PostgreSQL server, Npgsql,
  and `Npgsql.EntityFrameworkCore.PostgreSQL`; each has passed complete FOSS and
  no-paid-option review, and no unrelated PostgreSQL-licensed software relies on
  the exception.
- [ ] A package vulnerability audit reports no finding at or above the approved
  failure threshold.
- [ ] `dotnet format --verify-no-changes` or the repository's approved equivalent
  succeeds.
- [ ] Repository Markdown formatter, linter, link checker, and spelling/grammar
  checks pass for all changed documentation, or unavailable checks are recorded
  explicitly rather than treated as passed.
- [ ] A clean workflow starts separate API and Client processes, loads the shell,
  receives a valid `GET /api/v1/health` response, and publishes both independently.
- [ ] Secrets and repository scans find no committed credential, connection
  string, realistic household data, or generated local artifact.
- [ ] All implementation waves have user approval records, exclusive file and
  resource ownership, and exact completion evidence.
- [ ] `git diff --check` succeeds for HouseholdLedger changes.
- [ ] The final read-only audit finds no undocumented scope change, product-core
  deviation, accessibility gap, stale command, or unmet item.
- [ ] Budget Experiment and the parent repository's product implementation remain
  unchanged.

## Decision History

| Date | Status | Decision |
| --- | --- | --- |
| 2026-08-02 | Explicit user decision | Target .NET 10 LTS; pin SDK 10.0.302 with latest-patch roll-forward in its feature band; reject .NET 11 previews for the stable baseline. |
| 2026-08-02 | Explicit user decision | Use standalone Blazor WebAssembly; expose the core through an ASP.NET MVC API; treat the built-in UI as one replaceable client. |
| 2026-08-02 | Explicit user decision | Require a contract boundary consumable without server implementation assemblies. |
| 2026-08-02 | Explicit user decision | Add the latest StyleCop prerelease from the beginning as a deliberate stable-policy exception. |
| 2026-08-02 | Verified candidate | The latest StyleCop prerelease is `StyleCop.Analyzers` 1.2.0-beta.556, declared MIT. |
| 2026-08-02 | Explicit user decision | Allow only specified AGPL, GPL, Apache, and MIT SPDX families; block all others by default. |
| 2026-08-02 | Explicit user decision | Exclude dependencies whose projects offer paid, proprietary, commercial, or commercial-tier alternatives. |
| 2026-08-02 | Explicit user decision | Avoid AutoFixture, AutoMapper, and MediatR; prefer explicit code and built-in functionality. |
| 2026-08-02 | Research finding | Npgsql and PostgreSQL use the `PostgreSQL` license, which remains outside the general allowlist. |
| 2026-08-02 | Explicit user decision | Use PostgreSQL with Entity Framework Core. Approve a narrow standing exception only for the PostgreSQL licenses required by PostgreSQL server, Npgsql, and `Npgsql.EntityFrameworkCore.PostgreSQL`; retain FOSS and no-paid-option review for every dependency and do not admit unrelated PostgreSQL-licensed software. |
| 2026-08-02 | Research finding / Excluded | Testcontainers.PostgreSql is MIT, but its ecosystem offers Testcontainers Cloud. |
| 2026-08-02 | Research finding / Blocked | Microsoft.Playwright is MIT, but downloaded browser artifacts require separate review. |
| 2026-08-02 | Explicit user decision | Use six production projects and seven test projects in `HouseholdLedger.slnx`; commit NuGet lock files. |
| 2026-08-02 | Explicit user decision | Make checked OpenAPI JSON canonical and API Contracts a noncanonical .NET convenience assembly; use `/api/v1`, additive v1 changes, and a new URL major for breaking changes. |
| 2026-08-02 | Explicit user decision | Use reviewed Podman provisioning for PostgreSQL tests; retain the existing narrow PostgreSQL/Npgsql license exception. |
| 2026-08-02 | Explicit user decision | Permit a narrow browser-runtime license exception only after complete E2E artifact and license inventory; include `coverlet.collector` only after complete dependency review and set no coverage threshold. |
| 2026-08-02 | Explicit user decision | Defer the initial migration until a real model exists. |
| 2026-08-02 | All six waves approved / Wave 1 complete | The user approved implementation of Waves 1-6. Wave 1 is completed and validated; Waves 2-6 require no further user approval gate but remain subject to all documented ordering, ownership, resource isolation, validation, acceptance criteria, and Definition of Done requirements. |
| 2026-08-02 | Explicit user decision / Wave 3 policy exception | Approve a one-time stable-package-policy exception permitting `bunit` 2.8.6 and its required transitive `AngleSharp.Css` 1.0.0-beta.224 dependency for Wave 3, subject to all existing license, provenance, vulnerability, free/open-source, commercial-model, and no-paid-option checks. The exception does not approve other prereleases or future versions, relax broader package policy, mark Wave 3 complete, or satisfy implementation criteria. |

## Sources

All web sources were accessed 2026-08-02.

### Microsoft Architecture and Platform

- [.NET and .NET Core Support Policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
  - .NET 10 is LTS, active, and supported through 2028-11-14; support
    requires current servicing patches; previews are not supported releases.
- [Download .NET 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
  - Latest runtime 10.0.10 and SDKs 10.0.302/10.0.110 on 2026-08-02;
    .NET 11 Preview 6 is preview-only.
- [ASP.NET Core Blazor hosting models](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-10.0)
  - Standalone WebAssembly executes in the browser, deploys as static files, and
    accesses server resources through APIs.
- [Tooling for ASP.NET Core Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/tooling?view=aspnetcore-10.0)
  - .NET 10 provides `blazorwasm` for standalone WebAssembly; the old hosted
    template option is unavailable in .NET 8 and later.
- [Create web APIs with ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0)
  - Controller APIs use `ControllerBase`; `[ApiController]` supplies API-focused
    routing, validation, binding, and problem-details behavior.
- [Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
  - `Microsoft.AspNetCore.Mvc.Testing` supplies `WebApplicationFactory` and
    `TestServer`; unit and integration tests should be separated.
- [Test Razor components in ASP.NET Core Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/test?view=aspnetcore-10.0)
  - Microsoft describes component unit tests and browser E2E tests, naming
    bUnit and Playwright as examples while noting bUnit is third-party.
- [EF Core database providers](https://learn.microsoft.com/en-us/ef/core/providers/)
  - Provider majors generally must match EF Core; Microsoft recommends a
    direct current `Relational` dependency when independent providers lag.
- [Choosing an EF Core testing strategy](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy)
  - Prefer coverage against the production database system; avoid EF InMemory
    and account for provider-specific query behavior.

### StyleCop

- [`StyleCop.Analyzers` 1.2.0-beta.556 on NuGet](https://www.nuget.org/packages/StyleCop.Analyzers/1.2.0-beta.556)
  - NuGet marks it prerelease, declares MIT, and dates it 2023-12-20.
- [StyleCop Analyzers 1.2.0-beta.556 release](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/releases/tag/1.2.0-beta.556)
- [StyleCop Analyzers configuration](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/1.2.0-beta.556/documentation/Configuration.md)
- [Official StyleCop NuGet version index](https://api.nuget.org/v3-flatcontainer/stylecop.analyzers/index.json)

### License and Commercial-Model Evidence

- [SPDX License List](https://spdx.org/licenses/)
- [SPDX license expressions](https://spdx.github.io/spdx-spec/v2.3/SPDX-license-expressions/)
  - Defines `AND`, `OR`, `WITH`, and current GNU `-only`/`-or-later` forms.
- [PostgreSQL License](https://www.postgresql.org/about/licence/)
  - PostgreSQL calls it free/open source and similar to BSD or MIT, but its SPDX
    identity remains `PostgreSQL`; it is covered only for the three named
    PostgreSQL stack dependencies by the narrow standing exception.
- [Testcontainers Cloud](https://testcontainers.com/cloud/)
  - Official page offers Cloud for Desktop/CI, a demo, and a free trial.

### Official NuGet Package Records

- [`Microsoft.EntityFrameworkCore` 10.0.10](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.10)
- [`Microsoft.EntityFrameworkCore.Relational` 10.0.10](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Relational/10.0.10)
- [`Microsoft.EntityFrameworkCore.Design` 10.0.10](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.10)
- [`dotnet-ef` 10.0.10](https://www.nuget.org/packages/dotnet-ef/10.0.10)
- [`Microsoft.AspNetCore.Mvc.Testing` 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/10.0.10)
- [`Microsoft.AspNetCore.OpenApi` 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.10)
- [`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3)
  - NuGet declares the `PostgreSQL` license, covered here only by the narrow
    standing exception and still subject to complete dependency review.
- [`xunit.v3` 3.2.2](https://www.nuget.org/packages/xunit.v3/3.2.2)
- [`Microsoft.NET.Test.Sdk` 18.8.1](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1)
- [`coverlet.collector` 10.0.1](https://www.nuget.org/packages/coverlet.collector/10.0.1)
- [`bunit` 2.8.6](https://www.nuget.org/packages/bunit/2.8.6)
  - NuGet declares MIT; official sources show sponsorship but no project
    commercial tier found in this review.
- [`Testcontainers.PostgreSql` 4.13.0](https://www.nuget.org/packages/Testcontainers.PostgreSql/4.13.0)
  - NuGet declares MIT, while the official project links to Testcontainers Cloud.
- [`Microsoft.Playwright` 1.61.0](https://www.nuget.org/packages/Microsoft.Playwright/1.61.0)
  - NuGet declares MIT; that does not determine downloaded browser licenses.

### Read-Only Reference Material

- `BudgetExperiment/BudgetExperiment.sln`
- `BudgetExperiment/Directory.Build.props`
- `BudgetExperiment/docs/ARCHITECTURE.md`
- `BudgetExperiment/docs/DEVELOPMENT.md`
- Budget Experiment project package and project-reference metadata inspected on
  2026-08-02.

Reference-repository observations are lessons, not authoritative requirements
for HouseholdLedger.
