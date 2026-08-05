# Feature 001 Final Definition of Done Audit

## Audit Scope

**Date:** 2026-08-04

**Source of truth:**
[Feature 001: Application Scaffolding](../features/001-application-scaffolding.md)

**Supersedes for final closure:**
[2026-08-04 Feature 001 Closure Audit](2026-08-04-feature-001-closure-audit.md)

**Verdict:** Incomplete. Feature 001 is not complete because AC-09 and AC-10
are `Partial`, and therefore not every Definition of Done item is `Met`.

This audit incorporates the orchestrator's authoritative ownership and
validation record for the six closure work packages. It verifies repository
documents, locked package inventories, current test-source input requirements,
and retained evidence without starting a server, browser, container, or other
external resource. It does not change production code, tests, generated output,
or BudgetExperiment.

## Evidence Basis

### Current Local Inspection

- `src/HouseholdLedger.Client/packages.lock.json` now resolves
  `Microsoft.NET.ILLink.Tasks` `10.0.10`; the `10.0.9` lock mismatch reported
  by the superseded closure audit is absent.
- All committed `packages.lock.json` files enumerate the NuGet closure. The
  inventory includes the direct package families and transitive packages such
  as Npgsql `10.0.3`, bUnit's AngleSharp family, Microsoft build/runtime
  assets, NUnit, and xUnit runner assets. Enumeration is not a complete manual
  license, notices, commercial-model, or vulnerability review.
- The current browser test source requires eight explicit inputs:
  `HOUSEHOLDLEDGER_API_ARTIFACT`, `HOUSEHOLDLEDGER_CLIENT_PUBLISH_DIR`,
  `HOUSEHOLDLEDGER_FIREFOX_BINARY`, `HOUSEHOLDLEDGER_GECKODRIVER`,
  `HOUSEHOLDLEDGER_E2E_PROFILE_ROOT`, `HOUSEHOLDLEDGER_E2E_OUTPUT_DIR`,
  `HOUSEHOLDLEDGER_E2E_API_PORT`, and
  `HOUSEHOLDLEDGER_E2E_CLIENT_PORT`. It validates normalized inputs and
  distinct available loopback API and Client ports.
- The feature-specific API, Client, and browser-validation artifact directories
  are empty after cleanup. Older `artifacts/publish` output remains, but it is
  dated 2026-08-02 and is not used as proof of the 2026-08-04 fresh publishes.
- The current worktree includes changes outside this audit's writable scope:
  the Client calendar code-behind and the E2E browser test. This audit did not
  modify them. Documentation changes are separately scoped under `docs/`.

### Retained Closure Evidence Supplied by Owners

| Work package | Exact retained evidence | Audit use |
| --- | --- | --- |
| Blazor UI repair | `CalendarPage.razor.cs` was the only owned file; dedicated terminal only; no ports or processes. It retained the TimeProvider calendar shell, removed duplicate mock-ledger code and trailing whitespace, and passed diagnostics plus `git -c core.whitespace=cr-at-eol diff --check -- src/HouseholdLedger.Client/Pages/CalendarPage.razor.cs`. | Validates the current Client revision's scope and whitespace hygiene. |
| Utility fallback | Client lock file required no edit. Locked Client restore, Release Client build, Release Client publish, and Release solution build all passed with zero warnings and errors. | Resolves the prior restore/build blocker for AC-01. |
| Test architecture | Test-only ownership; explicit API `51271`, Client `51272`, profile/output allocation beneath `artifacts/feature-001-validation`; all resources removed. Client `22/22`, API Contracts `3/3`, API Integration `13/13`, HTTP smoke `1/1`, and two direct-W3C desktop/mobile browser runs `2/2` each passed. | Validates Client behavior, Razor checks, browser workflow, and test-pyramid evidence. |
| API HTTPS | API-only ownership; port `51281`; temporary output beneath `artifacts/feature-001-api-https`; trusted development certificate; locked restore, build, and publish passed. A published HTTPS host returned `200 {"status":"available"}` and `200 /openapi/v1.json` with OpenAPI `3.1.1` and the health path; process and port were released. | Validates independent API publishing and HTTPS runtime behavior. |
| Client HTTPS | Client-only ownership; port `51282`; temporary output beneath `artifacts/feature-001-client-https`; trusted development certificate; locked restore, build, and publish passed. Fresh static publish was 144 files and 15,814,766 bytes; a temporary TLS static host returned normal-certificate `200` root HTML, then was removed and its port released. The WebAssembly SDK `WasmAppHost` ignored explicit URL settings, so the static host, not `dotnet run --urls`, is the fixed-port publish proof. | Validates independently hostable static Client output and corrects the development-host interpretation. |
| PostgreSQL | Infrastructure and Infrastructure Integration Tests were the only allocation; Docker `29.6.2`; no network; container `householdledger-feature001-postgres`, port `55431`, database `householdledger_feature001`, and unique in-container credentials were removed. Image identity was `docker.io/library/postgres:18` at `postgres@sha256:a9abf4275f9e99bff8e6aed712b3b7dfec9cac1341bba01c1ffdfce9ff9fc34a`; runtime was PostgreSQL `18.3 Debian`. Focused real test `1/1`, full Infrastructure tests `2/2`, and API InfrastructureCompositionTests `6/6` passed. No production database was inspected or mutated. | Validates the real-provider functional gate and narrow runtime identity, but not a complete image-layer or notice closure. |

The Firefox executable hash was
`79f01d224fe7f31795f2d4edcb31f497c96e11e9d0770704ed8495861f70d1c1`;
the geckodriver hash was
`e95b4eac7960ffcd5acbfd92bb7d49d48f99c1d01a20ddd297fef8c80821020d`.
The retained desktop screenshot hash was
`7791e5733c1248c97952796b75cb92162c7b49ef20e2f83a970101602d9dce83`,
and the mobile screenshot hash was
`9db45a5b9f886f6058a52a0c1f4bd9cb025c88bd0dfe8ea416ec438c43dbfa12`.
The screenshot files themselves were correctly removed during cleanup, so this
audit verifies their recorded hashes rather than re-hashing files.

## Supply-Chain Review

### Verifiable Findings

| Closure | Evidence reviewed | Result |
| --- | --- | --- |
| Direct NuGet packages | `Directory.Packages.props`, all lock files, and the dated direct-family review in [Dependency Governance](../development/dependency-governance.md). | Direct versions, package-source policy, lock-file use, and explicit StyleCop and bUnit/AngleSharp.Css prerelease exceptions are documented. |
| Transitive NuGet packages | Current locked-file enumeration. | All resolved package names, versions, dependency edges, and content hashes are locally enumerable. The enumeration does not supply every package's license text, notices, publisher provenance, commercial-model finding, or individual vulnerability review. |
| Browser runtime | [Browser E2E Dependency Review](../development/browser-e2e-dependency-review.md), exact retained executable hashes, and direct-W3C execution results. | Firefox 153.0.1 EME-free and geckodriver 0.37.1 have the approved narrow runtime-only exception, recorded provenance, installed notices, version, hash, and vulnerability-review evidence. The direct-W3C harness adds no package. |
| PostgreSQL image and runtime | The retained immutable image digest, Docker 29.6.2, PostgreSQL 18.3 Debian runtime result, official-image documentation, Docker-library source license, PostgreSQL license page, and the real-provider cleanup report. | The official image source and PostgreSQL server license are supportable; the Docker-library wrapper source is MIT and PostgreSQL itself is PostgreSQL-licensed under the existing narrow exception. The supplied record does not retain a complete per-layer inventory, Debian package/SBOM, notices, vulnerability scan result, or commercial-model review for every bundled component. |
| Published API and Client | Retained independent publish commands, API health/OpenAPI transcript, and Client static-publish count/size plus TLS static-host transcript. | The final publish outputs were functionally verified but removed after cleanup. The local older publish output is not equivalent to the final output. No retained file-by-file runtime inventory, notices report, or vulnerability scan exists for the final publish directories. |

The official Docker Hub `postgres` page identifies the maintained Docker
Official Image and its Dockerfile/repository links. The PostgreSQL project
states that the database is free and open source under the PostgreSQL License.
NuGet identifies `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, its source
repository, and its PostgreSQL license. These upstream facts support the narrow
exception; they do not replace the missing complete closure evidence.

### Remaining Supply-Chain Gap

AC-10 requires every direct, transitive, tool, runtime, downloaded, database,
browser, and image dependency to have a dated manual provenance, license,
commercial-model, and vulnerability review. The current record does not retain
that complete review for every locked NuGet transitive, final published runtime
file, or PostgreSQL image layer and bundled Debian component. No local artifact
or supplied transcript fills that gap. Therefore neither a successful restore
nor the functional PostgreSQL run can support a complete supply-chain verdict.

**Required owner and action:** Research and Documentation must receive an
immutable final package/image/publish inventory (including image layer digests
or SBOM, notices, and scan reports), review each unresolved component against
the approved policy and current upstream sources, and record dated results.
The image/publish producer must retain those inventories before cleanup. The
user must decide any new exception or prohibited commercial-model finding.

## Acceptance Criteria Matrix

| Criterion | Verdict | Evidence and limitation |
| --- | --- | --- |
| AC-01 | Met | Current Client lock resolution plus retained locked restore, independent Release build/publish, and zero-warning Release solution build resolve the prior `NU1004` blocker. |
| AC-02 | Met | The approved six-production/seven-test-project inventory and prior provenance review remain unchanged. |
| AC-03 | Met | Retained automated architecture checks and project/package inspection support the approved dependency graph; closure changes were confined to Client code-behind, tests, or no edit. |
| AC-04 | Met | API Contracts `3/3`, API Integration `13/13`, HTTP smoke `1/1`, plus the independent API HTTPS OpenAPI transcript agree. |
| AC-05 | Met | Independent Client and API publishes passed. Client static output was served over trusted TLS; API HTTPS health and OpenAPI were served independently. The fixed-port Client proof is the temporary static host because WasmAppHost ignored explicit URLs. |
| AC-06 | Met | Client `22/22` and two direct-W3C desktop/mobile runs `2/2` against fresh publishes passed after the Client cleanup. |
| AC-07 | Met | The retained Client test result includes the Razor structural checks; the only current Client change was the owned code-behind cleanup. |
| AC-08 | Met | API Integration `13/13` and the independent API HTTPS transcript cover health, CORS, OpenAPI, and MVC-host behavior. |
| AC-09 | Partial | Real isolated PostgreSQL functional evidence is now complete: focused `1/1`, full Infrastructure `2/2`, and API composition `6/6` passed with cleanup and no production database contact. The required complete provenance, dependency-chain, FOSS, vulnerability, and commercial-model review for all image/runtime components is not retained. |
| AC-10 | Partial | Direct package, lock-file, browser-runtime, PostgreSQL digest/runtime, and final publish evidence are documented. A complete dated manual closure for all NuGet transitives, final published output files, and PostgreSQL layers/runtime components is absent. |
| AC-11 | Met | The user approved the revised behavior-light criterion. All seven projects were invoked; implemented layers passed; Domain/Application remain intentionally empty; prohibited helpers remain absent. |
| AC-12 | Partial | Restore/build/publish, API HTTPS, Client TLS static-host, and browser workflows are evidenced. The prior fixed-origin `dotnet run --urls` Client instruction was inaccurate because WasmAppHost ignores explicit URLs; guidance now records the limitation and the complete two-host manual workflow has not been retained as a single clean-environment transcript. |
| AC-13 | Partial | The orchestrator's authoritative closure allocations establish exclusive files/resources and cleanup for the six closure packages. Earlier Wave 1 and Wave 2 records identify ownership and validation but do not provide an independently retained complete terminal/resource ledger for every claimed resource. Under the stated evidence standard, the full all-wave claim remains only partially verifiable. |

## Definition of Done Matrix

| Definition of Done item | Verdict | Evidence or next action |
| --- | --- | --- |
| User approved specification, decisions, and six waves | Met | Approval and revised AC-11 decision are recorded in the feature decision history. |
| AC-01 through AC-13 are Met | Not Met | AC-09, AC-10, AC-12, and AC-13 are Partial. |
| Approved stable SDK resolves | Met | Retained validation used SDK 10.0.302. |
| No EF tool is required | Met | No tool manifest or migration exists; documentation rejects global `dotnet-ef`. |
| Deterministic locked restore succeeds | Met | Retained locked Client restore and zero-warning builds passed after confirming the current Client lock resolves 10.0.10. |
| Zero-warning solution build succeeds | Met | Retained Release whole-solution build passed with zero warnings/errors. |
| StyleCop is centralized and enforced | Met | Prior probe evidence remains supported by the current clean Release solution build. |
| API and Client build and publish independently | Met | Independent locked restore/build/publish evidence exists for both projects. |
| OpenAPI, contracts, controller, and HTTP probe agree | Met | API Contracts, API Integration, HTTP smoke, and HTTPS OpenAPI evidence agree. |
| Resource-free unit, contract, and component tests pass | Met | Client `22/22`, API Contracts `3/3`, API Integration `13/13`; approved empty Domain/Application projects have no behavior to test. |
| Real PostgreSQL Infrastructure and API integration pass | Met | Isolated focused real test `1/1`, full Infrastructure `2/2`, and API composition `6/6` passed and cleaned up. |
| Approved browser evidence passes twice | Met | Two direct-W3C desktop/mobile runs `2/2` passed with pinned runtime hashes. |
| Automated architecture and Razor checks pass | Met | Retained Client structural/architecture evidence and current closure scope support both checks. |
| Prohibited helpers are absent | Met | Retained source/package search and approved test architecture evidence support the absence. |
| Every dependency and artifact has complete dated review | Not Met | Complete transitive NuGet, image-layer/runtime, and final publish-output review is missing. Research and Documentation needs retained inventories and review records. |
| All licenses are allowlisted or exactly excepted | Not Met | Existing direct/browser and narrow PostgreSQL exceptions are documented, but complete closure review is absent. |
| Wave 3 exception and complete closure pass policy | Not Met | Exact bUnit/AngleSharp.Css exception is bounded, but their full transitive/published-output closure remains unreviewed. |
| PostgreSQL exception remains narrow and fully reviewed | Not Met | Scope and functional use are narrow; complete image/runtime component closure is not retained. |
| Vulnerability audit has no blocking finding | Partial | Locked restore audit is retained; individual current vulnerability evidence for every final image/publish component is absent. |
| Format verification passes | Met | Retained formatting validation and the owned Client CRLF-aware diff check passed. |
| Changed documentation checks pass or limitations are recorded | Met | This audit records unavailable repository Markdown formatter, linter, link, spelling, and grammar tools; current focused document validation follows below. |
| Fresh-publish cross-origin browser workflow passes | Met | Two direct-W3C desktop/mobile runs used fresh API/Client publishes and explicit isolated resources. |
| Secret and generated-artifact scans are clean | Not Verifiable | Cleanup reports remove allocated outputs and resources, but this audit has no fresh comprehensive secret/generated-artifact scan transcript. Test Architecture or Utility Fallback must run and retain one. |
| All waves have approval, exclusive ownership, and exact completion evidence | Not Met | Closure packages are authoritatively allocated; the all-resource evidence for Waves 1-2 is incomplete. Orchestrator must retain or supply those records. |
| Windows-aware HouseholdLedger diff hygiene passes | Met | The owned Client CRLF-aware check passed. Final documentation diff hygiene is validated after this audit edit. |
| Final audit finds no stale or unmet item | Not Met | This audit identifies supply-chain, workflow, older-wave ownership, and scan-evidence gaps. |
| BudgetExperiment and parent product implementation remain unchanged | Met | This documentation-only audit did not modify them; current status shows no BudgetExperiment change. |

## Completion Decision and Required Work

Feature 001 remains **incomplete**. Its implementation and functional
validation are substantially complete, including real PostgreSQL and browser
evidence, but the approved Definition of Done requires every item to be `Met`.
The outstanding work is evidence/documentation closure, not a license or runtime
finding that this audit can safely waive.

| Item | Classification | Concrete next action and owner |
| --- | --- | --- |
| Complete supply-chain closure | Partial / Not Met | Research and Documentation, with artifact-producing specialists, must preserve and review complete locked NuGet, final API/Client publish, and PostgreSQL image-layer/runtime inventories, notices, provenance, commercial model, and vulnerability outputs. |
| Reproducible manual local workflow | Partial | Blazor UI and ASP.NET API must provide a clean-environment, documented two-host HTTPS transcript that does not rely on WasmAppHost honoring `--urls`; Research and Documentation must verify the commands and URLs. |
| All-wave ownership record | Partial / Not Met | Orchestrator must supply the missing Wave 1-2 terminal/resource allocation and cleanup records, or explicitly narrow the AC-13 claim through user-approved scope change. |
| Comprehensive secret/generated-output scan | Not Verifiable | Utility Fallback or Test Architecture must run the owned scan and retain its exact scope/result. |

No undocumented product-core deviation or accessibility regression is evidenced
by the supplied closure results. The notable documentation mismatch was the
WasmAppHost fixed-URL assumption; it is recorded here and corrected in the
development guidance.

## Validation Record

The documentation owner completed focused validation on 2026-08-04:

- VS Code Markdown diagnostics reported no errors for the final audit, feature
  record, and changed development guides.
- Local Markdown links resolved for the final audit and for every changed tracked
  documentation file.
- `git -c core.whitespace=cr-at-eol diff --check -- docs` passed.
- No repository Markdown formatter, linter, link checker, spelling checker, or
  grammar checker command is available. Those tools are recorded as unavailable,
  not passed.

## Sources

- Local: committed `Directory.Packages.props`, `Directory.Build.props`, all
  `packages.lock.json` files, browser test source, feature specification, prior
  audits, and development dependency/browser records; inspected 2026-08-04.
- Retained local validation: the six closure work-package reports supplied by
  the orchestrator on 2026-08-04.
- Upstream, accessed 2026-08-04:
  [Docker Official Image for PostgreSQL](https://hub.docker.com/_/postgres),
  [docker-library/postgres license](https://github.com/docker-library/postgres/blob/master/LICENSE),
  [PostgreSQL license](https://www.postgresql.org/about/licence/), and
  [Npgsql EF Core provider 10.0.3](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3).
