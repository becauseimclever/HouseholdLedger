# Feature 001 Definition of Done Audit

## Audit Result

**Date:** 2026-08-03

**Source of truth:**
[Feature 001: Application Scaffolding](../features/001-application-scaffolding.md)

**Verdict:** Incomplete

AC-01 through AC-08 are Met. AC-09 is Blocked; AC-10, AC-11, and AC-12
are Partial; AC-13 is Not Verifiable. Feature 001 and Wave 6 cannot be marked
complete.

This was a read-only implementation audit. Only documentation was changed.
BudgetExperiment was treated as read-only.

## Acceptance Criteria

| Criterion | Verdict | Evidence and gap |
| --- | --- | --- |
| AC-01 | Met | SDK 10.0.302 is selected. Locked restore and C# 14/StyleCop probes are recorded; the fresh Release solution build passed with zero warnings and errors. Central package and build files, lock files, and `dotnet list package --include-transitive` enumerate the resolved package graph. AC-10 separately assesses completeness of manual dependency review. |
| AC-02 | Met | The solution contains the approved six production and seven test projects. Project and source inspection found the intended successor structure; no BudgetExperiment product change is present. |
| AC-03 | Met | Automated dependency-boundary tests pass. Project inspection confirms Domain has no reference, Application references Domain, Infrastructure references Application and Domain, API references Application, Infrastructure, and API Contracts, and Client references only API Contracts. |
| AC-04 | Met | Three API contract tests pass, including OpenAPI.NET parser diagnostics and schema assertions. Thirteen API integration tests pass, including checked/runtime agreement. The separate-process HTTP smoke has no product project reference and verifies health and OpenAPI. |
| AC-05 | Met | Independent Release publishes are recorded. Client configuration and HTTP-adapter tests pass. The fresh-publish browser workflow serves Client static output separately and proves the configured cross-origin API request through Resource Timing and explicit health status. |
| AC-06 | Met | Twenty-two Client tests pass. Two fresh hardened browser runs passed all 3 cases at exact `1440x900` and `500x844` inner viewports, covering landmarks, calendar period, honest empty state, unavailable/error handling, not-found recovery, skip navigation, geometry, and text containment. |
| AC-07 | Met | Client structural tests pass and enforce one `.razor.cs` partner per Razor file and no inline `@code` blocks. Build and component tests pass. |
| AC-08 | Met | API integration tests pass for MVC health, response shape, ProblemDetails, configured CORS, runtime OpenAPI, and conditional Infrastructure registration. Source uses an `[ApiController]` controller rather than a Minimal API health mapping. |
| AC-09 | Blocked | EF Core and Npgsql boundaries and in-process registration pass. Podman 5.8.3 is installed, but its engine is unavailable because upgrading WSL from the evidenced 2.3.26 release to required 2.7.11 needs administrator elevation. The reviewed PostgreSQL 18 image has not been pulled or run. The real connectivity test is skipped, so no approved real-PostgreSQL transcript exists. Complete PostgreSQL/image closure review is also not evidenced. |
| AC-10 | Partial | Direct package families, the resolved NuGet graph, and the exact Firefox/geckodriver runtime are inventoried. Browser provenance, licenses, notices, versions, vulnerabilities, signatures where available, and executable hashes are verified. Complete manual transitive and published-runtime review is not evidenced. PostgreSQL image review exists, but there is no pull, runtime inventory, or run evidence. |
| AC-11 | Partial | AutoFixture, AutoMapper, and MediatR are absent. Implemented tests use explicit setup and resource-dependent layers use approved dependencies. The seven projects are present, but Domain and Application report no discoverable tests because those layers contain no behavior. Treating this as an acceptable behavior-light pyramid materially revises the literal approved wording; exact revised wording below requires user approval. |
| AC-12 | Partial | Setup, testing, troubleshooting, independent publish, and browser commands match current behavior. Automated fresh-publish evidence starts separate hosts and verifies Client and API URLs. No clean-environment manual HTTPS startup and URL-verification transcript is recorded. |
| AC-13 | Not Verifiable | The specification and reports describe exclusive ownership and isolated ports, profiles, processes, and outputs. The audit had no independently verifiable complete orchestrator record covering every wave, file, terminal, and external resource. Claims in reports alone are insufficient for a Met verdict. |

## AC-11 Approval Required

The current implementation deliberately avoids vanity Domain and Application
tests because those projects contain no behavior. Accepting that design is
reasonable for this scaffold, but it changes the approved meaning of a balanced
test pyramid. Replace AC-11 with the following exact wording only after explicit
user approval:

> **AC-11: Explicit implementation patterns and proportionate tests**
>
> AutoFixture, AutoMapper, and MediatR are absent. Tests use explicit builders or
> factories where setup is needed; mapping is explicit; use cases use built-in DI
> and direct interfaces. All seven test projects are present and invocable. For
> this behavior-light scaffold, Domain and Application test projects may contain
> zero tests until those layers own behavior; placeholder or vanity assertions
> must not be added to satisfy a numerical pyramid. Implemented behavior is tested
> at the lowest appropriate layer, and browser E2E remains the smallest behavior
> layer. Resource-dependent tests run only with approved dependencies and do not
> use excluded substitutes.
>
> Evidence: dependency and source search, focused source review, test
> classification by behavior and layer, commands invoking all seven projects,
> and exact results for every implemented layer.

Until the user approves this wording, AC-11 remains Partial. Existing approval
of all six implementation waves does not approve this acceptance change.

## Supply-Chain Reconciliation

- **NuGet:** Direct and transitive packages are locked and enumerable. Manual
  direct-family review exists. Complete transitive notices and complete
  published API, Client WebAssembly, and test-runtime closure review are not
  evidenced.
- **Browser:** Firefox 153.0.1 EME-free and geckodriver 0.37.1 provenance,
  installed licenses, versions, vulnerabilities, signatures where available,
  and artifacts are verified. Tests enforce Firefox SHA-256
  `79f01d224fe7f31795f2d4edcb31f497c96e11e9d0770704ed8495861f70d1c1`
  and geckodriver SHA-256
  `e95b4eac7960ffcd5acbfd92bb7d49d48f99c1d01a20ddd297fef8c80821020d`
  before launch.
- **Browser execution:** Two fresh runs after hardening passed 3/3. Checks cover
  post-navigation page errors and unhandled rejections, explicit API health
  status, critical same-origin Resource Timing entries, and static-host response
  status.
- **Browser residual:** Direct W3C WebDriver Classic provides no pre-navigation
  script injection and no Firefox console-log retrieval. Errors before the
  post-navigation listener is installed are not observable through this driver.
- **PostgreSQL:** The `docker.io/library/postgres:18` image has a review record,
  but it has not been pulled or run because the Podman engine is blocked. No
  claim of complete image or published-runtime review is supported.

## Definition of Done

| Definition of Done item | Verdict | Evidence or remaining work |
| --- | --- | --- |
| User approved the specification, decisions, and six waves | Met | Decision history records explicit approval on 2026-08-02. |
| AC-01 through AC-13 are Met | Not Met | Five criteria are not Met. See the criterion matrix. |
| Approved stable SDK resolves | Met | Fresh `dotnet --version` returned `10.0.302`. |
| Approved EF tool restores | Met | Reconciled requirement: this scaffold has no tool manifest or migration and explicitly requires no local or global `dotnet-ef`. |
| Deterministic locked restore succeeds | Met | Latest recorded locked restore passed with only the approved prerelease exceptions and no blocking audit finding. |
| Zero-warning solution build succeeds | Met | Fresh Release `dotnet build --no-restore` passed with zero warnings and errors. |
| StyleCop is centralized and enforced | Met | Central configuration, prior C# 14/failing-rule probes, clean build, and absence from runtime output are recorded. |
| API and Client build and publish independently | Met | Independent Release publishes and project graph evidence pass. |
| OpenAPI, contracts, controller, and HTTP probe agree | Met | Parser/schema, runtime drift, integration, and separate-process smoke evidence pass. |
| Resource-free unit, contract, and component tests pass | Met | Fresh results: API Contracts 3/3 and Client 22/22. Domain/Application contain no behavior tests; AC-11 governs acceptance of that design. |
| Real-PostgreSQL Infrastructure and API integration pass | Blocked | Connectivity test remains skipped; engine, image pull/run, and transcript are absent. |
| Approved browser evidence passes twice | Met | Two hardened fresh-publish runs passed 3/3 at desktop and mobile viewports. |
| Automated architecture checks pass | Met | Boundary tests pass. |
| Automated Razor checks pass | Met | Pairing and no-`@code` tests pass. |
| Prohibited helpers are absent | Met | Package/source inspection and tests support the absence of AutoFixture, AutoMapper, and MediatR. |
| Every dependency and artifact has complete dated review | Partial | Browser chain is complete; transitive/published NuGet runtime and PostgreSQL image/runtime closure are incomplete. |
| Every license is allowlisted or exactly excepted | Partial | Direct families and browser exceptions are evidenced; incomplete closure prevents a complete conclusion. |
| Wave 3 exception and full closure pass policy | Partial | Exact versions and direct review are evidenced; complete bUnit/AngleSharp and published-output closure is not. |
| PostgreSQL exception remains narrow and fully reviewed | Partial | Narrow package use is evidenced; complete server/image and runtime review is not. |
| Vulnerability audit has no blocking finding | Met | Latest locked restore/build evidence reports no high or critical blocking finding; automation limits remain documented. |
| Format verification passes | Met | Fresh `dotnet format HouseholdLedger.slnx --verify-no-changes --no-restore` passed. |
| Changed Markdown checks pass or unavailable checks are recorded | Met | Editor Markdown diagnostics and local-link validation pass. No repository Markdown formatter, linter, or spelling/grammar command is configured; those checks are recorded as unavailable, not passed. |
| Fresh-publish cross-origin browser workflow passes | Met | Hardened runs used independent API and Client publishes and observed successful cross-origin health. |
| Secret and generated-artifact scans are clean | Met | Focused scan found only the explicit synthetic `test-only-secret` integration-test value and the PostgreSQL harness's runtime-generated GUID password. Status inspection found no tracked browser screenshots or temporary publish output. |
| Every wave has approval, ownership, and completion evidence | Not Verifiable | Approval exists; complete independently verifiable ownership and completion evidence does not. |
| `git diff --check` succeeds | Not Met | Literal default `git diff --check` exits 2 because it treats CRLF carriage returns as trailing whitespace. Direct text scanning found zero trailing-whitespace lines, and docs-scoped plus repository-wide checks pass with `git -c core.whitespace=cr-at-eol diff --check`. The literal command in the approved DoD is nevertheless not Met. |
| Final audit finds no stale or unmet item | Not Met | This audit found Partial, Blocked, Not Verifiable, and hygiene items. |
| BudgetExperiment and parent implementation remain unchanged | Met | Parent status reports only the HouseholdLedger working tree as modified; no BudgetExperiment path is changed. |

## Validation Evidence

Fresh commands run on 2026-08-03 without launching a browser or Podman engine:

- `dotnet --version`: `10.0.302`.
- `C:\Program Files\RedHat\Podman\podman.exe --version`: `5.8.3`.
- `wsl --version`: installed WSL `2.3.26.0`; the required 2.7.11 upgrade remains
  administrator-blocked.
- Release solution build: passed with zero warnings and errors.
- API Contracts: 3 passed.
- Client tests: 22 passed.
- API Integration: 13 passed.
- Infrastructure Integration: 1 passed, 1 PostgreSQL test skipped.
- Domain and Application projects: invoked; no tests available because no
  behavior exists in either project.
- OpenAPI compare: runtime output matched the checked artifact.
- `dotnet list HouseholdLedger.slnx package --include-transitive`: succeeded and
  enumerated the current resolved graph; enumeration is not manual closure
  review.
- Solution format verification: passed.
- Editor Markdown diagnostics: no errors.
- Local Markdown links: 0 broken across 10 files.
- Direct trailing-whitespace scan: 0 findings across 10 owned documentation
  files.
- Windows-aware docs-scoped and repository-wide diff checks: passed with
  `git -c core.whitespace=cr-at-eol diff --check`.
- Literal default `git diff --check`: exit 2 because CRLF carriage returns are
  classified as trailing whitespace.
- No repository Markdown formatter, linter, or spelling/grammar command is
  configured; those checks were unavailable and are not claimed as passed.

The browser was not rerun because two exact fresh hardened runs and retained
screenshots already provide the requested broad evidence. The Podman engine was
not invoked because its WSL prerequisite is known to be blocked.

## Required Work

1. Obtain administrator elevation, upgrade WSL to 2.7.11, start the Podman
   engine, pull the reviewed PostgreSQL image, and run the owned harness to a
   successful cleanup transcript.
2. Complete manual transitive, published-runtime, and PostgreSQL image/runtime
   provenance, license, notice, commercial-model, and vulnerability review.
3. Ask the user to approve or reject the exact AC-11 wording above.
4. Produce a clean-environment manual HTTPS API/Client startup and URL transcript.
5. Supply independently verifiable ownership/resource records for AC-13.
6. Resolve repository-wide diff hygiene findings through the owning specialists.

Wave 6 and Feature 001 remain incomplete until every criterion and Definition of
Done item is Met.
