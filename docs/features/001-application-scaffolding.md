# Feature 001: Application Scaffolding

## Status

Status: Paused for active implementation and acceptance validation; superseded
for that active work by Feature 002 / not complete / not abandoned

- Approval authority: the user. The orchestrator cannot approve this document
  on the user's behalf.
- Approval record: the user approved the complete Feature 001 specification and
  implementation of all six waves on 2026-08-02. On 2026-08-04, the user
  approved a fresh hosting direction: the API directly references and hosts the
  built-in Blazor WebAssembly Client through standard .NET framework support;
  MVC/OpenAPI remains available for alternate HTTP/OpenAPI clients; and a
  visible rendered shell is the required hosted-WASM outcome. This supersedes
  the immediately prior external-artifact-composition simplification.
- Implementation readiness: this specification revision is approved, but it
  does not authorize implementation or establish completion. Historical
  evidence may be consulted only as context; it does not prove the simplified
  acceptance criteria below.
- Pause decision: on 2026-08-04, the user paused Feature 001 so Feature 002
  can isolate and prove one deliberately minimal hosted Blazor WebAssembly
  runtime outcome. Feature 001 remains approved but incomplete and may be
  resumed only through a later user decision. No prior failed, partial, or
  historical Feature 001 evidence is acceptance evidence for Feature 002.
- Scope type: foundational application scaffold; no household-ledger feature
  behavior is included.
- Research date: 2026-08-02; user decisions updated 2026-08-04.

## Governing Simplification Revision

This section is the governing Feature 001 hosting and validation contract. It
records the user's fresh approved direction of 2026-08-04 and supersedes the
immediately prior external-static-artifact-composition simplification. It also
supersedes conflicting earlier Feature 001 text requiring custom Client payload
manifests, per-file hashes, asset inventories, static-file allow-lists,
composition scripts, package-composition provenance, retry or retention policy,
or two-host and multi-viewport browser validation. None of those controls is a
Feature 001 requirement unless a future approved feature establishes it.

### Minimal Deployed Workflow

1. The API directly references `HouseholdLedger.Client` using the standard
  SDK/framework hosted Blazor WebAssembly support and enables its built-in
  hosting/static-asset behavior.
2. Launch one API HTTPS process and open its root URL in a browser.
3. The browser loads WebAssembly and displays the visible Client shell.

The API remains available to alternate clients: `/api/v1` and OpenAPI are
non-UI paths on the same host. A separate local Client host is not required for
Feature 001 acceptance.

### Minimum Implementation Contract

- `HouseholdLedger.Api` directly references `HouseholdLedger.Client`; this
  intentional reference enables the framework's hosted Blazor WebAssembly
  behavior and static-asset handling.
- `HouseholdLedger.Client` may reference `HouseholdLedger.Api.Contracts`, but
  must not reference API implementation, Application, Infrastructure, or
  Domain. The API-to-Client reference does not reverse this rule.
- The API uses built-in, supported hosted Blazor WebAssembly/static-asset
  behavior. MVC `/api/v1` and OpenAPI remain available for alternate
  HTTP/OpenAPI clients.
- No custom composition script, manual static artifact staging, manifest,
  inventory, hash, custom static-file provider, or pre/post-processing is part
  of this Feature 001 contract. Existing implementation surfaces for those
  mechanisms are obsolete and must be removed by their owning specialists.
- No server-side Blazor, SSR, prerendering, Interactive Server, Interactive
  Auto, or SignalR UI circuit is introduced.

### Visible Scaffold State

The Client must display a document title and one accessible main heading, such
as `Household Ledger`. Placeholder content is sufficient. It must not imply
that ledger entries, financial data, or Kakeibo workflows exist.

### Simplified Acceptance Criteria

| Criterion | Observable outcome | Completion evidence |
| --- | --- | --- |
| AC-01: Baseline API build | With the documented SDK and restored checkout, the API project builds without warnings or errors. | `dotnet build` result for the API project. |
| AC-02: Intentional hosted Client boundary | The API directly references the Client as the standard hosted-WASM relationship; the Client has no reference back to API implementation, Application, Infrastructure, or Domain. | Project-reference inspection and API build result. |
| AC-03: One-host API workflow | One API process and URL serve the built-in WASM application at the root while MVC `/api/v1/...` and OpenAPI remain direct external HTTP/OpenAPI surfaces. | One browser proof, plus at most a minimal direct health/OpenAPI check. |
| AC-04: Visible WebAssembly shell | A browser loading the known API HTTPS URL renders the Client title and one main heading. | The one authoritative browser proof below. |

### Simplified Definition of Done

The feature is done only when a read-only audit marks AC-01 through AC-04
`Met`, confirms the intentional API-to-Client reference and one-way Client
boundary, confirms the honest visible shell, and finds no undocumented scope
change or contradicted criterion.

There is exactly one authoritative browser validation: build the API project,
launch the API on a known HTTPS URL, open that URL in a browser, and assert that
the visible shell rendered. A minimal direct check of health and OpenAPI may
support the API boundary, but it does not create an additional Feature 001 gate.

**Failure routing:** A loading failure is fixed by the owner of the controlling
component (API hosted-WASM startup, Client shell, or browser harness), then the
one-URL proof is rerun.

### Planned Simplification Work

This documentation phase does not direct file deletion or authorize
implementation. The following observed responsibilities are planned for
simplification:

| Surface | Planned owner | Responsibility to remove or reduce |
| --- | --- | --- |
| `src/HouseholdLedger.Api/HouseholdLedger.Api.csproj` and `src/HouseholdLedger.Api/Program.cs` | ASP.NET API | Add the intentional Client project reference and configure only built-in hosted-WASM/static-asset behavior. |
| `src/HouseholdLedger.Client/` | Blazor UI, if needed | Adjust only the basic title/main-heading shell if the hosted output needs it. |
| `scripts/Compose-HostedClientPackage.ps1` and related custom artifact surfaces | Utility Fallback | Remove obsolete composition, staging, manifest, inventory, hash, provider, and pre/post-processing behavior. |
| Assigned browser-proof files | Test Architecture | Remove former multi-host, CORS, inventory, and excessive browser assertions; retain one API-URL browser proof. |

Normal path safety when copying files, HTTPS, and the API boundary remain
appropriate minimal correctness and security concerns.

### Dependency-Ordered Implementation Plan

This plan records approved scope only. The orchestrator must assign exclusive
writable files and resources before each phase.

1. **ASP.NET API:** add the API-to-Client project reference and configure
  standard hosted Blazor WebAssembly startup/static-asset behavior; run an API
  build and the minimal direct API check.
2. **Blazor UI, if needed:** adjust the component shell only when necessary to
  render a title and main heading under the API host.
3. **Utility Fallback:** remove obsolete composition scripts and custom
  artifacts as shared tooling, after exclusive ownership is assigned.
4. **Test Architecture:** add and run one browser proof against one API HTTPS
  URL; do not recreate a multi-host or static-host matrix.
5. **Research and Documentation:** audit the simplified criteria without
  modifying implementation.

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

A new contributor can restore, build, test, publish, and run an intentionally
empty HouseholdLedger system from documented commands. The system consists of a
standard .NET hosted Blazor WebAssembly Client and an ASP.NET Core MVC API. The
API directly references the Client and is the one local and deployed HTTPS
process: visiting its root starts the built-in WASM application. The Client
reaches server capabilities through HTTP contracts that any frontend can
implement. The API continues to expose its health and OpenAPI endpoints and a
persistence boundary without implementing ledger,
budgeting, import, authentication, reporting, or reflection behavior.

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
2. Use a standard API-hosted Blazor WebAssembly Client with no server-side
  rendering or interactive server circuit for the first-class browser
  experience.
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
15. Keep MVC `/api/v1` and checked OpenAPI independently consumable by custom
  clients while the API host serves the built-in Client at its root.

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
- Requiring separately deployable API and Client assemblies, custom artifact
  composition, or extra hosting scripts for the built-in Client.
- Retaining a two-host browser workflow as the normal local or deployed
  experience. A narrowly scoped custom-client API compatibility smoke remains
  permitted.
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
| Server-rendered UI as the default | Use Blazor WebAssembly hosted by the API through the standard framework support. The API directly references Client; no server-rendered UI circuit is introduced. |
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

Use standard .NET hosted Blazor WebAssembly support: the API directly references
the Client project and enables the framework's built-in hosting/static-asset
behavior. The first-class local and deployed browser workflow starts the API
host and visits its root. The Client uses a same-origin API base address in this
built-in deployment.

Frontend replaceability remains an HTTP/OpenAPI concern, not an assembly
deployment constraint. Custom web, mobile, desktop, and automation clients can
consume the documented API without an implementation assembly. The hosted
Client must not reference API implementation, Application, Infrastructure, or
Domain. Do not add a custom artifact composition step, static-file provider,
fallback implementation, manifest, inventory, hash, or pre/post-processing.

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
Infrastructure, API Contracts, and Client. Its Client reference is intentional:
it enables the standard hosted Blazor WebAssembly/static-asset behavior. It owns
MVC controllers, HTTP mapping, middleware, CORS, OpenAPI generation,
configuration, and startup. MVC `/api/v1/...` and OpenAPI remain externally
consumable HTTP/OpenAPI surfaces; this does not require separate API and Client
deployment units.

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

`HouseholdLedger.Client` is a Blazor WebAssembly project hosted by the API. It
may reference API Contracts only. It does not reference API implementation,
Domain, Application, or Infrastructure. It owns Razor components, presentation
state, accessibility, and API-client adapters.

The client uses a same-origin API base address in the API-hosted deployment. It
uses `HttpClient` and built-in JSON support unless a proven requirement justifies
another dependency. Its first page is a restrained, accessible calendar-centered
shell that does not simulate nonexistent financial data.

## Dependency Direction and Specialist Ownership

```text
Domain <- Application <- Infrastructure
             ^                ^
             |                |
             +-------------- API -> API Contracts <- Client
                             |
                             +-> Client
                             ^
                             |
                  custom clients via HTTP/OpenAPI
```

An arrow points to a referenced project. Domain references nothing. Application
references Domain. Infrastructure references Application and Domain. API
references Application, Infrastructure, API Contracts, and Client. Client may
reference only API Contracts. Custom clients need no .NET project reference.

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
| HTTP system smoke | EndToEndTests | A separate process proves the published API through HTTP without product project references. |
| Browser end to end, smallest | EndToEndTests | A package-free direct-W3C client proves the API-hosted published Client and MVC/OpenAPI boundary at desktop and mobile viewports. |

Do not use AutoFixture. Tests use readable explicit builders, object mothers
only where appropriate, or focused factory methods that expose meaningful
defaults. Do not hide setup behind reflection-driven specimen generation.

PostgreSQL tests use reviewed Podman provisioning with deterministic isolation
and cleanup. Podman 5.8.3 is installed, but the engine cannot start until an
administrator upgrades WSL from the evidenced 2.3.26 release to required
2.7.11. Testcontainers is excluded. Browser E2E uses manually provisioned
Firefox 153.0.1 Windows x64
en-US EME-free and geckodriver 0.37.1 Windows x64 under the narrow test-runtime
exception recorded below. Their artifact provenance, hashes, signatures where
available, installed licenses, versions, and vulnerability status were verified
before the successful browser runs.
`Selenium.WebDriver`, Selenium Manager, telemetry, and runtime downloaders are
excluded; the tests must use direct W3C WebDriver HTTP/JSON through built-in
.NET APIs. Microsoft.Playwright remains blocked.

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
their upstream projects must be free and open source. Optional vendor-provided
paid support for otherwise identical free and open-source software does not
disqualify it. Exclude a library or project that offers paid product tiers,
features, editions, hosted commercial counterparts, or dual commercial
licensing, even when the specific package or community edition is free and uses
an allowlisted license. Donations, grants, sponsorship, and optional support
alone do not constitute a commercial tier.

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

### Resolved Baseline as of 2026-08-02

The rows marked introduced reflect current direct project references or an
identified transitive dependency. Introduction is not proof that the complete
transitive/runtime closure satisfies policy. The dated manual direct-family
review and its limits are in
[`dependency-governance.md`](../development/dependency-governance.md).

| Package or tool | Resolved / declared license | Reconciliation |
| --- | --- | --- |
| .NET SDK | 10.0.302 / MIT source baseline | Selected by `global.json`; current CLI evidence resolves 10.0.302. |
| Microsoft ASP.NET Core direct packages | 10.0.10 / MIT | Introduced for standalone WebAssembly, runtime OpenAPI, and API integration tests; optional paid support does not disqualify otherwise identical FOSS. |
| Microsoft EF Core direct packages | 10.0.10 / MIT | Introduced and exactly aligned; complete closure review remains open. |
| `Microsoft.OpenApi` | 2.11.0 / MIT | Introduced for checked OpenAPI parsing and comparison. |
| `StyleCop.Analyzers` | 1.2.0-beta.556 / MIT | Introduced under the deliberate prerelease exception; Wave 1 analyzer probes passed. |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 / PostgreSQL | Introduced under the narrow standing license exception; real server/image and complete closure evidence remain open. |
| Resolved `Npgsql` driver | 10.0.3 / PostgreSQL | Transitive under the same narrow exception; no startup database access is performed. |
| PostgreSQL server image | `docker.io/library/postgres:18` at `postgres@sha256:a9abf4275f9e99bff8e6aed712b3b7dfec9cac1341bba01c1ffdfce9ff9fc34a` / PostgreSQL and bundled components | Isolated Docker 29.6.2 validation ran PostgreSQL 18.3 Debian and cleaned its container, database, port, and credentials. Complete image-layer, bundled-component, notices, and vulnerability closure remains open. |
| `Microsoft.NET.Test.Sdk` | 18.8.1 / MIT | Introduced in all test projects. |
| `NUnit` / `NUnit3TestAdapter` | 4.6.1 / 4.6.0 / MIT | Introduced for contract, component, integration, and system tests. |
| `xunit.runner.visualstudio` | 3.1.5 / Apache-2.0 | Introduced in the currently empty Domain and Application unit-test projects. |
| `bunit` | 2.8.6 / MIT | Introduced under the one-time Wave 3 exception; full closure review remains open. |
| `AngleSharp.Css` | 1.0.0-beta.224 / MIT | Resolved transitive dependency under the same one-time Wave 3 prerelease exception; full closure review remains open. |
| `coverlet.collector` | Not introduced | Deferred pending complete review; this feature has no coverage threshold. |
| `Testcontainers.PostgreSql` | 4.13.0 / MIT | Excluded: official project links to the commercial Testcontainers Cloud service. |
| Firefox/geckodriver test runtime | Firefox 153.0.1 EME-free / geckodriver 0.37.1 | Narrow test-runtime-only exception; exact user-local artifacts and installed notices verified, with two complete browser runs passing. |
| `Microsoft.Playwright` | 1.61.0 / MIT package | Blocked; it is outside the approved Firefox/geckodriver chain and has unresolved runtime and commercial-product concerns. |

AutoFixture, AutoMapper, and MediatR are intentionally absent. No candidate with
`Provisional`, `Blocked`, `Deferred`, or `Excluded` status may be silently
installed.

## Local Developer Workflow

### Prerequisites

- .NET SDK 10.0.302 or a latest-patch SDK in its feature band selected by
  `global.json`.
- Podman, whose installation was approved on 2026-08-03, for reviewed isolated
  PostgreSQL integration tests; Docker Desktop and Testcontainers are not used.
- PowerShell 7 or a shell capable of running equivalent `dotnet` commands.
- Manually provisioned Firefox 153.0.1 Windows x64 en-US EME-free and
  geckodriver 0.37.1 Windows x64, verified under the approved test-runtime-only
  exception, for browser E2E tests.
- No globally installed `dotnet-ef`; restore it from the local tool manifest.

### Expected Commands

Run from `HouseholdLedger/` after implementation:

```powershell
dotnet --info
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test tests/HouseholdLedger.Api.Contracts.Tests --no-build
dotnet test tests/HouseholdLedger.Client.ComponentTests --no-build
dotnet test tests/HouseholdLedger.Infrastructure.IntegrationTests
dotnet test tests/HouseholdLedger.Api.IntegrationTests
dotnet run --project src/HouseholdLedger.Api
```

The one Feature 001 browser proof starts the API and loads its one HTTPS root
URL. It does not require a separately published Client, a static host, a second
port, or a custom artifact environment variable. Use the focused test/browser
workflow supplied by Test Architecture after it is implemented.

There is currently no local .NET tool manifest, and no migration exists. Do not
run `dotnet tool restore` or install `dotnet-ef` for this scaffold. Committed
NuGet lock files support `--locked-mode`.

Compare runtime OpenAPI with the checked artifact without mutating it, or update
the artifact only after reviewing an intentional contract change:

```powershell
pwsh scripts/openapi/Sync-OpenApi.ps1
pwsh scripts/openapi/Sync-OpenApi.ps1 -Update
pwsh scripts/openapi/Sync-OpenApi.ps1
```

The first and third commands compare only. The `-Update` command atomically
replaces the artifact. Automated tests never update it.

The primary local workflow builds and starts one HTTPS API host. Visiting its
root loads the built-in Client; `/api/v1/health` and `/openapi/v1.json` remain
direct HTTP endpoints on that same host. Same-origin deployment does not remove
the API's CORS policy for approved alternate origins, but the built-in Client
must not need cross-origin CORS to function.

Document database secret commands only after the provisioning and configuration
method is approved. Document exact local URLs only after launch settings
allocate and verify them.

### API-Hosted Client Integration Contract

The implementation phase must establish the following deliberately simple
contract:

1. The API directly references the Client project using standard SDK/framework
   hosted Blazor WebAssembly support and enables built-in hosting/static-asset
   behavior.
2. Starting the API is the first-class workflow. Its root starts WebAssembly
   and renders the Client title and one main heading.
3. MVC `/api/v1/...` and OpenAPI remain directly available to external
   HTTP/OpenAPI clients. This compatibility does not require an API build or
   deployment without its Client reference.
4. The Client references neither API implementation nor Application,
   Infrastructure, or Domain.
5. No custom composition script, manual staging, manifest, inventory, hash,
   custom static-file provider, or pre/post-processing is required.

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
test projects. The API directly references Client as an intentional hosted-WASM
relationship. Client references no API implementation, Application,
Infrastructure, or Domain project; API Contracts references no HouseholdLedger
project; no Budget Experiment source is copied.

Evidence: solution project listing, directory listing, and source provenance
review.

### AC-03: Enforced dependency direction

Domain has no references; Application references only Domain; Infrastructure
references Application and Domain; API references Application, Infrastructure,
API Contracts, and Client; Client may reference only API Contracts. No cycle
exists, and transport, Razor, EF, or provider types do not leak across their
boundaries.

Evidence: project-reference inspection, package-reference inspection, build,
and architecture-boundary test or equivalent automated check.

### AC-04: Replaceable API/client contract

The API publishes a valid OpenAPI document describing `/api/v1/health`, errors,
content types, and schemas. A client implemented from HTTP/OpenAPI can call the
API without any server implementation assembly. API Contracts contains only
transport concerns, and contract drift checks pass.

Evidence: generated OpenAPI artifact, schema validation, contract tests, and a
small implementation-independent HTTP probe.

### AC-05: Standard hosted WebAssembly workflow

The API directly references Client and uses only built-in hosted Blazor
WebAssembly/static-asset behavior. Starting one API process and loading its root
URL starts WASM and renders the basic Client shell. The Client calls only
documented routes and does not reference API implementation, Application,
Infrastructure, or Domain. No custom composition script, staging, manifest,
inventory, hash, static-file provider, or pre/post-processing remains required.

Evidence: project graph, API build, one-URL browser proof, and source review of
the removed custom artifact surfaces.

### AC-06: Honest accessible calendar shell

The Client displays a responsive calendar-centered shell whose empty state
identifies the period without invented entries, totals, savings, or reflections.
Loading, API-unavailable, not-found, and error states are accessible. Template
demo content is absent.

Evidence: component tests and, after the approved browser artifacts are
provisioned and verified, successful desktop and mobile browser evidence.

### AC-07: Universal Razor code-behind

Every `.razor` file has a matching `.razor.cs` partial class, no Razor file has
an `@code` block, and lifecycle methods, handlers, parameters, injection, and
presentation logic reside in code-behind.

Evidence: automated pairing/content check plus successful component tests and
build.

### AC-08: MVC API pipeline

`GET /api/v1/health` is served by an `[ApiController]` MVC controller derived from
`ControllerBase`, returns a successful machine-readable response, and discloses
no sensitive configuration. MVC routing, problem details, and OpenAPI work
through the real host alongside the built-in hosted Client.

Evidence: minimal direct health/OpenAPI check; source review confirms no Minimal
API mapping for this endpoint.

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

### AC-11: Explicit implementation patterns and proportionate tests

AutoFixture, AutoMapper, and MediatR are absent. Tests use explicit builders or
factories where needed; mapping is explicit; use cases use built-in DI and direct
interfaces. All seven test projects are present and invocable. For this
behavior-light scaffold, Domain and Application test projects may contain zero
tests until those layers own behavior; placeholder or vanity assertions must not
be added to satisfy a numerical pyramid. Implemented behavior is tested at the
lowest appropriate layer, and browser E2E remains the smallest behavior layer.
Resource-dependent tests run only with approved dependencies and do not use
excluded substitutes.

Evidence: dependency and source search, focused source review, test
classification by behavior and layer, commands invoking all seven projects, and
exact results for every implemented layer.

### AC-12: Local workflow is accurate

A contributor following the README/development documentation can restore tools
and packages, build the API project, start one API HTTPS host, load the shell at
its root, and make minimal direct health and OpenAPI checks without undocumented
setup. Resource-dependent commands do not rely on excluded products.

Evidence: API build result, single-host browser evidence, and minimal direct
HTTP verification of health and OpenAPI.

### AC-13: Specialist and resource ownership

Implementation history shows no overlapping writable file ownership, terminal,
port, database, schema, container, browser profile, or generated-output use.
Each wave reports its files, resources, and exact validation results before the
next wave begins.

Evidence: orchestrator wave records and specialist completion reports.

### Current Acceptance Status

The final 2026-08-04 read-only audit is recorded in
[2026-08-04 Feature 001 Final Definition of Done Audit](../audit/2026-08-04-feature-001-final-definition-of-done-audit.md).

- **AC-01 through AC-04 - Requires fresh validation.** The fresh 2026-08-04
  hosted-Blazor decision changes the first-class project relationship and
  workflow. Prior external-composition evidence is historical only.
- **AC-05 through AC-08 - Requires fresh validation where hosting behavior or
  evidence depended on external composition.** No prior static-host, fallback,
  multi-host, or independent-publish evidence proves this contract.
- **AC-09 - Partial.** Isolated real PostgreSQL tests and cleanup passed, but the
  complete image/runtime component review required by the criterion is not
  retained.
- **AC-10 - Partial.** Direct, lock-file, browser, digest/runtime, and publish
  evidence is recorded; the complete manual transitive, image-layer/runtime, and
  final published-output closure remains absent.
- **AC-11 - Met.** The user approved the revised behavior-light wording, and all
  implemented layers have passing evidence without placeholder tests.
- **AC-12 - Previously Partial / invalidated for workflow proof.** The former
  two-host workflow is no longer an accepted primary proof. A clean-environment
  single-host transcript is required.
- **AC-13 - Partial.** The orchestrator supplied authoritative exclusive closure
  allocations and cleanup, while independently retained full resource detail for
  earlier Waves 1-2 remains incomplete.

## Hosting-Revision Implementation Phases and Approval Record

The following phases are approved for specification planning only. They do not
authorize implementation, transfer existing file ownership, or establish that
any acceptance criterion is complete. The orchestrator must assign the exact
exclusive files and dedicated resources before each phase starts.

| Phase | Dependency and owner | Exclusive implementation surface | Dedicated resources | Required primary validation |
| --- | --- | --- | --- | --- |
| H1 | First: API specialist | Assigned API project/startup files | Dedicated terminal only | Build API; run it; make at most minimal direct health/OpenAPI checks. |
| H2 | After H1: Blazor UI, only if the shell needs adjustment | Assigned Client component-shell files | Dedicated terminal only | Confirm title and main heading render through the API host. |
| H3 | After H1 and H2: Utility Fallback | Assigned shared scripts and custom artifact surfaces | Dedicated terminal only | Remove obsolete composition/staging scripts and artifacts; no replacement processing. |
| H4 | After H3: Test Architecture | Assigned browser-proof files | Dedicated terminal, one HTTPS port, and browser profile | One browser proof against the API root URL. |
| H5 | After H4: Research and Documentation | Assigned documentation only | Dedicated terminal only; no runtime resources | Read-only criterion-to-evidence audit and available documentation checks. |

The expected dependency order is H1, H2 if needed, H3, H4, then H5. No phase
may use a shared terminal, port, browser profile, database, schema, container,
test data, process, or output directory without an explicit exclusive allocation
and cleanup record.

## Authoritative Validation Matrix and Guardrails

This matrix is authoritative for the hosting-revision implementation phase. An
owner records one primary command or retained artifact for each criterion; a
passing result is reused unless an explicit invalidator applies. Supplementary
checks may diagnose a failure but do not replace the designated primary proof.

| Requirement | Primary proof | Owner | Retained location / identifier |
| --- | --- | --- | --- |
| AC-01 | `dotnet build` for the API project | API specialist | Command result. |
| AC-02 | Project-reference inspection plus API build | API specialist | Project graph or review note and command result. |
| AC-03 | Run one API process and load its root URL | Test Architecture | One browser-proof result. |
| AC-04 | Browser asserts the visible title and main heading | Test Architecture | One browser-proof result. |
| DoD: external API compatibility | Minimal direct health and OpenAPI checks | API specialist | Direct-check result. |
| DoD: obsolete custom processing removed | Source review after H3 | Utility Fallback | Removal report. |
| DoD: final completion conclusion | Read-only criterion-to-evidence audit | Research and Documentation | Dated final audit in `docs/audit/`. |

Validation stays deliberately small: build the API project; inspect the
intentional project direction; run the API; load its root URL once in a browser;
and make at most minimal direct health/OpenAPI checks. The browser workflow is
the primary UI proof. Do not repeat former two-host, static-host, manifest, or
multi-viewport matrices.

After a primary proof passes, do not rerun it unless its owner records an
invalidator: a change to its owned surface or declared dependency, changed
artifact inputs, changed runtime/tool version, changed resource configuration,
or a documented nondeterminism investigation. Failures are classified as
`production`, `test`, `environment`, or `evidence`; only the owning specialist
may repair the classified surface. One rerun is permitted after a documented
repair. A second failure of the same primary proof requires escalation to the
orchestrator with the classification, retained logs, attempted repair, and
resource-cleanup status before any third attempt.

Owners retain command lines, exit codes, and concise result summaries. Temporary
servers, HTTPS ports, and browser profiles are removed after evidence capture.
A future audit marks historical external-composition evidence as `invalidated`;
it must never silently treat a previous pass as proof for this hosted surface.

## Implementation Waves and Approval Record

The user approved this feature document and implementation of all six waves on
2026-08-02. Waves 1 and 2 are completed and validated. Waves 3-5 are partially
implemented and Wave 6 is audited but incomplete as recorded below. Work may
proceed without another user approval gate, but only in the documented
dependency order. Before each wave, the orchestrator must still record its
outcome, participating specialists, exact writable files, isolated resources,
dependencies, risks, and validation. Approval does not establish implementation
completion or satisfy any acceptance criterion or Definition of Done item by
itself.

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

Approval: explicitly approved by the user on 2026-08-02; completed and
validated on 2026-08-02.

Owners: ASP.NET API for API Contracts, controller, CORS, OpenAPI, and API startup;
Utility Fallback owns shared project files sequentially when assigned.

Define the narrow health contract, `/api/v1` convention, MVC health controller,
problem details, CORS, and generated OpenAPI artifact. Add contract drift checks.
Do not invent business DTOs or use cases.

Gate evidence: API integration and contract checks, OpenAPI validation,
implementation-independent HTTP probe, and API publish without Client.

Completion evidence: the API integration and API Contracts projects pass. They
cover `/api/v1/health`, framework ProblemDetails, configured CORS, runtime and
checked OpenAPI agreement, and dependency boundaries. The default OpenAPI
script invocation compared runtime output without changing the checked
artifact. The separate-process HTTP system smoke passed without a product
project reference, and the API published independently. Infrastructure
registration is conditional on a nonblank, syntactically usable connection
string; configured startup registers Npgsql without connecting to a database or
accessing a schema. OpenAPI.NET parser diagnostics and schema assertions now
pass, so AC-04 is Met.

### Wave 3: Independent WebAssembly Client Shell

Approval: explicitly approved by the user on 2026-08-02; implementation evidence
is present, but the wave is not complete because its complete dependency-closure
review remains open.

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

Current evidence: the standalone WebAssembly Client reads configurable public
`Api:BaseUrl`, references only API Contracts, and publishes independently. All
22 Client tests pass and are classified as 12 unit, 8 bUnit component, and 2
structural tests. The structural checks enforce project boundaries and universal
Razor code-behind. The exact bUnit/AngleSharp.Css prerelease exception is
preserved. The direct-family manual review found no bUnit or AngleSharp paid
product tier, but complete transitive notices and published-output review remain
open. Optional vendor-provided paid support does not disqualify otherwise
identical FOSS under the 2026-08-03 user decision. Therefore the gate is not
fully met.

### Wave 4: PostgreSQL Persistence Boundary

Approval: explicitly approved by the user on 2026-08-02; partially implemented
and blocked at the real-PostgreSQL gate.

Owner: Persistence and Integrations. The PostgreSQL license decision is
resolved by the narrow standing exception, and reviewed Podman provisioning is
the approved test method. Podman 5.8.3 is installed, but its engine is blocked
until an administrator upgrades WSL to 2.7.11.
This wave starts only after the named PostgreSQL dependencies pass the remaining
policy reviews.

Implement the empty EF boundary without fake entities, an initial migration,
automatic startup migration, Testcontainers, EF InMemory, or SQLite
substitution. Defer the initial migration until a real model exists.

Gate evidence: standing-exception decision record, complete dependency reviews,
proof that no unrelated PostgreSQL-licensed software entered through the
exception, boundary checks, reviewed Podman provisioning, and real-provider
test results. Without completed reviews and successful provisioning evidence,
this wave and feature completion remain blocked.

Current evidence: the empty `HouseholdLedgerDbContext`, Npgsql registration, and
owned Podman harness exist without entities or migrations. One in-process
registration test passes. The Infrastructure NUnit references were repaired
with `PrivateAssets="all"`. Podman 5.8.3 is installed, but its engine is
unavailable because upgrading WSL to required version 2.7.11 needs administrator
elevation. The real PostgreSQL connectivity test skips, and no isolated
connection string is supplied. Installation is not execution evidence. The
reviewed PostgreSQL image has not been pulled or run, and no real-provider
transcript exists, so this wave is not complete.

### Wave 5: Remaining Test Pyramid and Developer Documentation

Approval: explicitly approved by the user on 2026-08-02; browser evidence is
complete, while the broader clean-workflow and final-review gates remain open.

Owners: Test Architecture for component/integration/E2E tests and harnesses;
Research and Documentation for assigned development/architecture documentation.

The following Wave 5 evidence describes the pre-revision two-host design. It
remains historical evidence only and cannot satisfy the API-hosted Client
criteria introduced on 2026-08-04. The hosting-revision phases and validation
matrix above control replacement evidence.

Add only approved component, API, infrastructure, and E2E evidence. Browser E2E
uses only the approved manually provisioned Firefox 153.0.1 EME-free and
geckodriver 0.37.1 test-runtime chain after all post-provision checks pass. It
uses no Selenium package, Selenium Manager, telemetry, or runtime downloader;
direct W3C WebDriver calls use built-in .NET HTTP/JSON APIs. Document separate
API/Client startup, CORS, configuration, independent publishing, and Podman
database provisioning. Parallel work requires disjoint files and resources.

Gate evidence: each test layer's exact command and result, approved browser viewport
evidence, Markdown checks, and clean-workflow transcript.

#### Wave 5 Documentation Evidence

The assigned architecture and developer-documentation slice was completed on
2026-08-02. It records the current layer boundaries, independent API and Client
startup and publishing, explicit CORS and public Client configuration, test
commands by layer, the owned Podman PostgreSQL harness, universal Razor
code-behind conventions, dependency governance, contribution workflow, and
verified troubleshooting. The repository README now provides a Kakeibo-aware
onboarding path without implying that ledger behavior exists.

This documentation and test evidence does not complete Wave 5:

- Podman 5.8.3 is installed, but its engine is unavailable because the WSL
  2.7.11 upgrade requires administrator elevation. The PostgreSQL harness has
  not executed, the real-provider test reports skipped, and no real-provider
  transcript exists.
- `Microsoft.Playwright` 1.61.0 does not qualify under current policy. Its
  runtime includes an Apache-2.0 JavaScript driver, Node.js 24.16.0 and its
  third-party inventory, Chromium 149, Firefox 151, WebKit 26.5, FFmpeg, and a
  Windows dependency helper. Complete binary notices and classifications remain
  unresolved, browser downloads have no pinned cryptographic hash or signature
  verification, and Microsoft offers paid Azure Playwright Workspaces. The
  proposed browser-runtime license exception does not waive the independent
  no-paid-counterpart rule.
- The approved test-owned W3C WebDriver client uses only built-in .NET
  HTTP/JSON APIs. It contains no Selenium or Playwright package, invokes no
  manager, and downloads no runtime. Exact Firefox 153.0.1 EME-free and
  geckodriver 0.37.1 artifacts are pinned under the user's local application
  data and supplied by explicit normalized absolute environment variables.
- The NUnit EndToEndTests project has one separate-process HTTP system smoke
  plus desktop and mobile browser cases, with zero product project references.
  Two consecutive complete 3/3 fresh-publish runs passed in 8.7 seconds and
  8.1 seconds. The browser cases prove cross-origin Resource Timing and API
  CORS, exact `1440x900` and `500x844` inner viewports, title, landmarks,
  calendar period, honest empty state, not-found recovery, skip navigation,
  geometry, and text containment. Owned processes, ports, profiles, variables,
  and temporary publish outputs were cleaned.
- Four screenshots were retained under ignored `TestResults` output. Desktop
  images are 47,332 bytes with SHA-256
  `a63a564fff9a9811ea7d58c832ad0e57b3062e1ba2648e56277d46223356f882`;
  mobile images are 26,266 bytes with SHA-256
  `5ba6d48e6b703d50dfd512e95f9c2cf58ab8ef4eb2c37791428f6ef0fca28ddf`.
- Locked restore, a zero-warning solution build, formatting, and the available
  contract, component, integration, structural, system, and browser tests pass.
  The 43 cases classify as 12 unit, 8 component, 2 contract, 14 integration
  including one skipped external PostgreSQL case, 1 system, 2 browser E2E, and
  4 structural tests. The available result is 42 passed and one skipped. Unit
  tests are 27.9% of the suite and not yet a majority; empty Domain and
  Application tests are
  intentional because those layers contain no behavior.
- API and Client Release publishes pass independently. The OpenAPI compare
  script reports that runtime generation matches the checked artifact.

The detailed browser evidence is in
[`docs/development/browser-e2e-dependency-review.md`](../development/browser-e2e-dependency-review.md).
Changed Wave 5 Markdown files pass the editor's available Markdown diagnostics.
Repository Markdown link, command, structure, spelling, grammar, and diff checks
are recorded in the completion report for this documentation update; unavailable
checks are not treated as passed.

### Wave 6: Final Verification and Definition of Done Audit

Approval: explicitly approved by the user on 2026-08-02; audited on 2026-08-03
and not complete.

Owners: Utility Fallback for broad executable validation, then Research and
Documentation for a read-only criterion-to-evidence audit.

No implementation changes occur during the audit. Any `Not Met`, `Blocked`, or
`Not Verifiable` verdict returns to the owning specialist in a newly approved
repair wave when scope or resources change.

Gate evidence: complete command transcript and every criterion/Definition of
Done item marked `Met` with concrete evidence. The dated audit records Partial,
Blocked, and Not Verifiable items, so this gate is not met.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| API Contracts becomes a disguised Shared/domain assembly | Restrict it to transport shapes, enforce no project references, and keep OpenAPI canonical. |
| Built-in client becomes privileged despite API-first intent | Require independent builds, HTTP-only access, CORS, and an implementation-independent contract probe. |
| API-hosted static assets recreate server/client coupling | Integrate a declared Client publish artifact only; prohibit an API-to-Client CLR reference, Client startup dependency, server rendering, and UI circuit. |
| SPA fallback hides an API/OpenAPI failure | Route MVC and OpenAPI before fallback; integration tests prove unknown API/OpenAPI paths do not return Client HTML. |
| Historical two-host proof is treated as current | Mark affected prior evidence invalidated; use the authoritative matrix and retain only single-host browser proof plus one scoped custom-client API smoke. |
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
11. Optional vendor-provided paid support does not disqualify otherwise
  identical free/open-source software; paid product tiers, features, and
  editions remain disqualifying.
12. Firefox 153.0.1 Windows x64 en-US EME-free and geckodriver 0.37.1 Windows
  x64 have a narrow test-runtime-only non-allowlisted license exception,
  conditional on post-provision provenance, hash, signature, license, version,
  and vulnerability verification. No Selenium package, Selenium Manager,
  telemetry, or runtime downloader is allowed; direct W3C WebDriver uses
  built-in .NET HTTP/JSON APIs.
13. Installing Podman for the approved PostgreSQL integration-test harness is
  approved. Installation and successful PostgreSQL tests remain required
  evidence.
14. The built-in standalone Blazor WebAssembly Client is served from the API
  host using a published static artifact. The API remains independently
  consumable through MVC `/api/v1` and OpenAPI for alternate UIs. The API has no
  Client CLR project reference; no server-side Blazor or SignalR UI circuit is
  introduced. The user approved this hosting-scope revision on 2026-08-04.

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
- [x] `dotnet --version` resolves the approved stable .NET 10 SDK policy.
- [x] No .NET tool manifest or migration exists, so no EF tool is required or
  restored for this scaffold; setup explicitly rejects global `dotnet-ef`.
- [x] Deterministic `dotnet restore` succeeds with no package downgrade,
  blocking vulnerability, or prerelease dependency except the documented
  StyleCop 1.2.0-beta.556 exception and the one-time Wave 3 exception for
  `bunit` 2.8.6 with required transitive `AngleSharp.Css`
  1.0.0-beta.224.
- [x] `dotnet build --no-restore` succeeds with zero warnings and zero errors.
- [x] StyleCop is centralized, runs for every C# project, passes the C# 14 probe,
  is absent from runtime output, and is enforced by the build.
- [ ] API directly references Client using built-in hosted Blazor WebAssembly
  support; Client has no API implementation, Application, Infrastructure, or
  Domain reference; no custom composition or static-artifact processing remains.
- [x] Checked OpenAPI, API Contracts, controller behavior, and an
  implementation-independent HTTP probe agree.
- [x] Unit, contract, and component tests pass without database or browser
  prerequisites.
- [ ] Infrastructure and API integration tests pass against isolated real
  PostgreSQL resources using the excepted provider stack and an approved
  provisioning method.
- [ ] Browser evidence uses the approved package-free direct-W3C automation and
  exact verified Firefox and geckodriver artifacts; the API-hosted Client root,
  API/OpenAPI precedence, and eligible SPA fallback pass at desktop and mobile
  viewports.
- [x] Automated architecture checks prove the approved project-reference and
  package boundaries.
- [x] Automated Razor checks prove one `.razor.cs` partner per `.razor` file and
  zero `@code` blocks.
- [x] AutoFixture, AutoMapper, and MediatR are absent; explicit alternatives are
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
- [x] A package vulnerability audit reports no finding at or above the approved
  failure threshold.
- [x] `dotnet format --verify-no-changes` or the repository's approved equivalent
  succeeds.
- [x] Repository Markdown formatter, linter, link checker, and spelling/grammar
  checks pass for all changed documentation, or unavailable checks are recorded
  explicitly rather than treated as passed.
- [ ] A fresh-publish browser workflow starts one API HTTPS host, loads the
  integrated Client at its root, observes same-host `GET /api/v1/health` and
  OpenAPI behavior, proves fallback exclusions, and retains one scoped
  custom-client HTTP/OpenAPI compatibility smoke.
- [x] Secrets and repository scans find no committed credential, connection
  string, realistic household data, or generated local artifact.
- [ ] All implementation waves have user approval records, exclusive file and
  resource ownership, and exact completion evidence.
- [ ] `git diff --check` succeeds for HouseholdLedger changes.
- [ ] The final read-only audit finds no undocumented scope change, product-core
  deviation, accessibility gap, stale command, or unmet item.
- [x] Budget Experiment and the parent repository's product implementation remain
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
| 2026-08-02 | Wave 5 documentation evidence / Blocked | Architecture and developer documentation completed. `Microsoft.Playwright` 1.61.0 does not qualify under current policy because its complete runtime/license chain is unresolved, browser archive installation lacks pinned cryptographic verification, and Microsoft offers paid Azure Playwright Workspaces. Podman and browser execution evidence remain outstanding; Wave 5 is not complete. |
| 2026-08-02 | Wave 2 complete | Thirteen API Integration project tests and three API Contracts project tests, nonmutating runtime/checked OpenAPI comparison, a separate-process implementation-independent HTTP probe, and independent API publish pass. Infrastructure registration is conditional and performs no startup database/schema access. |
| 2026-08-02 | Waves 3-5 partial / Blocked | Twenty-two classified Client tests and the then-required independent Client publish passed. This is historical evidence only after the 2026-08-04 standard-hosted-Blazor decision. The empty EF boundary and Podman harness are present, with one in-process pass and one real-PostgreSQL skip. The separate-process HTTP system smoke passes but is not browser evidence. Full dependency closure, PostgreSQL, browser, clean-workflow, and final-audit gates remain open. |
| 2026-08-02 | Wave 5 system-test repair / Blocked | EndToEndTests uses NUnit and contains one separate-process HTTP system test, not browser E2E. The test requires an explicit normalized absolute `HOUSEHOLDLEDGER_API_ARTIFACT`; two consecutive fresh-publish runs passed and cleaned their variable, process, and output. Infrastructure NUnit references now use `PrivateAssets="all"`. The suite has 41 cases: 40 pass and the real PostgreSQL case skips. AC-04, AC-05, AC-06, AC-10, and AC-11 remain Partial; AC-09 and Wave 5 remain Blocked; AC-07 and AC-08 are Met. |
| 2026-08-02 | Manual package review / Unresolved | Primary-source review covers all introduced direct package families and the exact AngleSharp.Css prerelease. No project-offered paid product was found outside the already blocked Playwright chain, but Microsoft's optional paid enterprise support requires user interpretation and complete transitive/runtime review remains open. Automation is not treated as business-model proof. |
| 2026-08-02 | Browser-chain research / Needs user decision | Firefox 153.0.1 EME-free with geckodriver 0.37.1 can provide the required browser evidence with manual pinned provisioning. `Selenium.WebDriver` 4.46.0 remains unapproved because its package embeds an incompletely classified Selenium Manager native/Rust closure; a package-free W3C WebDriver local end is the preferred alternative. Firefox directly offers paid Professional Support, so the user must interpret the no-paid-option rule and approve the exact runtime-only license exception before implementation. Browser DoD remains unmet. |
| 2026-08-03 | Explicit user decision / Commercial-model interpretation | Optional vendor-provided paid support does not disqualify otherwise identical free/open-source software. Paid product tiers, features, and editions remain disqualifying. |
| 2026-08-03 | Explicit user decision / Browser test-runtime exception | Approve Firefox 153.0.1 Windows x64 en-US EME-free and geckodriver 0.37.1 Windows x64 as a narrow test-runtime-only exception for their non-allowlisted licenses. Approval is conditional on exact post-provision artifact provenance, hash, signature where available, installed-license, version, and vulnerability verification. Use no Selenium package, Selenium Manager, telemetry, or runtime downloader; call W3C WebDriver directly through built-in .NET HTTP/JSON APIs. Browser acceptance criteria and Definition of Done remain unmet until verification and tests pass. |
| 2026-08-03 | Explicit user decision / PostgreSQL test runtime | Approve installing Podman to run the reviewed PostgreSQL integration-test harness. AC-09 and the PostgreSQL Definition of Done item remain blocked until installation, provisioning, and real-PostgreSQL tests succeed. |
| 2026-08-03 | Direct-W3C browser evidence / Met | Exact user-local Firefox 153.0.1 EME-free and geckodriver 0.37.1 artifacts passed post-provision checks. A package-free built-in .NET W3C client completed two consecutive 3/3 runs in 8.7 seconds and 8.1 seconds at exact `1440x900` and `500x844` inner viewports. AC-05, AC-06, and the browser Definition of Done item are met; PostgreSQL remains blocked by unavailable WSL elevation. |
| 2026-08-03 | Browser E2E hardening / Met | Tests enforce approved Firefox and geckodriver SHA-256 values before launch and check post-navigation page errors, explicit API health status, critical Resource Timing entries, and static-host responses. The direct-W3C residual is no pre-navigation injection or Firefox console-log retrieval. Two fresh hardened runs passed 3/3. |
| 2026-08-03 | Final audit / Incomplete | AC-01 through AC-08 are Met; AC-09 is Blocked; AC-10, AC-11, and AC-12 are Partial; AC-13 is Not Verifiable. AC-11 requires user approval of revised behavior-light-scaffold wording. Wave 6 and Feature 001 are not complete. |
| 2026-08-04 | Explicit user decision / AC-11 revision | Approve the revised AC-11 wording. Domain and Application test projects may contain zero tests while this scaffold owns no Domain or Application behavior; placeholder or vanity tests must not be added. This decision does not waive testing at the lowest appropriate layer when behavior is introduced. |
| 2026-08-04 | Provisional closure audit / Superseded | A Client lock-file mismatch and Client source whitespace issue blocked the earlier closure evidence. This finding is superseded by the later retained locked-restore, build, test, and source-hygiene evidence. |
| 2026-08-04 | Final Definition of Done audit / Incomplete | Locked restore, zero-warning builds, independent HTTPS API/Client publishes, real PostgreSQL tests, and direct-W3C browser validation now pass. AC-01 through AC-08 and AC-11 are Met. AC-09 and AC-10 remain Partial because the complete image/runtime, published-output, and transitive dependency policy closure is not retained. AC-12 is Partial because WasmAppHost ignores fixed URL settings and no clean-environment two-host manual transcript exists. AC-13 is Partial because closure allocations are authoritative but complete earlier-wave resource records are not independently retained. Feature 001 remains incomplete. |
| 2026-08-04 | Explicit user decision / Hosting scope revision (superseded) | This earlier same-day decision replaced the two-host primary workflow with API-hosted standalone Blazor WebAssembly static assets but retained independent Client publishing and no API-to-Client CLR reference. The later 2026-08-04 standard-hosted-Blazor decision supersedes those retained restrictions. Its two-host, CORS, and static-host evidence remains historical and invalidated for the current criteria. |
| 2026-08-04 | Explicit user decision / Simplification revision | Reduce Feature 001 to a minimum hosted-WASM scaffold: compose Client static output into the API package, launch one API HTTPS URL, and prove in a browser that WebAssembly renders a visible shell. Supersede Feature 001 requirements for custom payload manifests, hashes, inventories, static allow-lists, provenance, retry/retention policy, and excessive browser validation unless a concrete minimum requirement establishes a need. The governing simplification revision defines the authoritative acceptance criteria and Definition of Done. |
| 2026-08-04 | Explicit user decision / Standard hosted Blazor WebAssembly | Supersede the immediately prior external-artifact-composition simplification. API directly references Client and enables built-in hosted Blazor WebAssembly/static-asset behavior. No custom composition scripts, manual staging, manifests, inventories, hashes, custom static-file provider, or pre/post-processing is required. The first-class workflow is one API process and root URL; MVC `/api/v1/...` and OpenAPI remain external HTTP/OpenAPI surfaces. Client must not reference API implementation, Application, Infrastructure, or Domain. No SSR, server-side Blazor, prerendering, or SignalR UI circuit is authorized. |

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

- [`Microsoft.AspNetCore.Components.WebAssembly` 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly/10.0.10)
- [`Microsoft.EntityFrameworkCore` 10.0.10](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.10)
- [`Microsoft.EntityFrameworkCore.Relational` 10.0.10](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Relational/10.0.10)
- [`Microsoft.EntityFrameworkCore.Design` 10.0.10](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.10)
- [`Microsoft.AspNetCore.Mvc.Testing` 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/10.0.10)
- [`Microsoft.AspNetCore.OpenApi` 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.10)
- [`Microsoft.OpenApi` 2.11.0](https://www.nuget.org/packages/Microsoft.OpenApi/2.11.0)
- [`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3)
  - NuGet declares the `PostgreSQL` license, covered here only by the narrow
    standing exception and still subject to complete dependency review.
- [`Microsoft.NET.Test.Sdk` 18.8.1](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1)
- [`NUnit` 4.6.1](https://www.nuget.org/packages/NUnit/4.6.1)
- [`NUnit3TestAdapter` 4.6.0](https://www.nuget.org/packages/NUnit3TestAdapter/4.6.0)
- [`xunit.runner.visualstudio` 3.1.5](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5)
- [`bunit` 2.8.6](https://www.nuget.org/packages/bunit/2.8.6)
  - NuGet declares MIT; official sources show sponsorship but no project
    commercial tier found in this review.
- [`AngleSharp.Css` 1.0.0-beta.224](https://www.nuget.org/packages/AngleSharp.Css/1.0.0-beta.224)
- [`dotnet-ef` 10.0.10](https://www.nuget.org/packages/dotnet-ef/10.0.10)
  - Candidate record only; no tool manifest or package is introduced.
- [`coverlet.collector` 10.0.1](https://www.nuget.org/packages/coverlet.collector/10.0.1)
  - Deferred and not introduced.
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
