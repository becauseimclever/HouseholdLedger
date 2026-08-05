# Feature 001 Closure Audit

## Audit Scope

**Date:** 2026-08-04

**Source of truth:**
[Feature 001: Application Scaffolding](../features/001-application-scaffolding.md)

**Previous audit:**
[2026-08-03 Feature 001 Definition of Done Audit](2026-08-03-feature-001-definition-of-done.md)

**Verdict:** Incomplete. This read-only audit does not make a feature-completion
claim.

This audit reconciles current read-only evidence with retained 2026-08-03
evidence. The only uncommitted product change observed was
`src/HouseholdLedger.Client/Pages/CalendarPage.razor.cs`; it is not owned by
this audit and was not modified. The current worktree cannot complete locked
restore for Client because `Microsoft.NET.ILLink.Tasks` resolves to `10.0.10`
while the Client lock file requires `[10.0.9, )`. Evidence that depends on a
current Client build, Client tests, or fresh Client publish is therefore not
retained as current proof.

## Approved AC-11 Decision

The user expressly approved the AC-11 revision on 2026-08-04. Empty Domain and
Application test projects are acceptable while the scaffold owns no Domain or
Application behavior. The revised criterion prohibits placeholder or vanity
assertions and requires future behavior to be tested at the lowest appropriate
layer. This approval resolves the prior wording decision only; it does not
waive any test, build, dependency, environment, or ownership evidence.

## Evidence Basis

### Current 2026-08-04 Evidence

- `dotnet --version` returned `10.0.302`.
- `dotnet sln HouseholdLedger.slnx list` listed six production and seven test
  projects.
- `dotnet test HouseholdLedger.slnx --no-restore --nologo --logger
  'console;verbosity=minimal'` exited `1`. Client `browser-wasm` locked restore
  failed with `NU1004` for `Microsoft.NET.ILLink.Tasks` `10.0.9` versus
  `10.0.10`. The command showed API Contracts `3/3` passing, API Integration
  `13/13` passing, Infrastructure `1` passing and `1` PostgreSQL test skipped,
  and zero discoverable tests in each Domain and Application test project. The
  three E2E failures were missing-artifact-contract failures: required explicit
  fresh publish paths were not supplied.
- A source/package search found no `AutoFixture`, `AutoMapper`, or `MediatR`
  references in `src/` or `tests/`.
- `git diff --check` and its Windows-aware form both exited `2`, identifying
  actual trailing whitespace in the uncommitted Client source file. This is not
  the earlier CRLF-only result.
- No local executable `markdownlint`, `markdown-link-check`, `cspell`, or
  `vale` command was available. Editor diagnostics for the feature document and
  prior audit reported no errors before this audit was added.

### Retained 2026-08-03 Evidence

The prior audit records exact successful Release build, locked restore, format,
OpenAPI comparison, independent API/Client publishes, browser E2E, and scoped
test results. It also records Podman `5.8.3`, WSL `2.3.26.0`, unavailable
administrator elevation for the required WSL `2.7.11` upgrade, an unpulled
PostgreSQL image, and no real-provider transcript. Retained evidence remains
useful only where the current worktree does not invalidate it.

## Acceptance Criteria Matrix

| Criterion | Verdict | Exact evidence and required closure work |
| --- | --- | --- |
| AC-01 | Not Met | SDK `10.0.302` is current, but current validation fails Client locked restore with `NU1004`; the former clean locked restore/build result is superseded for this worktree. Utility Fallback must reconcile and review the Client lock file, then rerun locked restore and zero-warning Release build. |
| AC-02 | Met | Current solution listing contains exactly six production and seven test projects. The 2026-08-03 provenance review found no copied BudgetExperiment source; no current evidence contradicts it. |
| AC-03 | Met | Current project inventory plus retained passing automated architecture-boundary evidence support the approved graph. The observed change is inside Client code-behind, not project references or package manifests. |
| AC-04 | Met | Current API Contracts `3/3` and API Integration `13/13` passed. Retained parser/schema, runtime/checked OpenAPI, and independent HTTP smoke evidence remains unaffected by Client locked restore. |
| AC-05 | Not Met | The current Client build cannot complete locked restore, so independent Client build/publish cannot be claimed. Rerun independent Client publish/static-host validation after remediation. |
| AC-06 | Not Met | The current uncommitted Client code has not passed locked restore, Client tests, or a fresh browser run. Retained browser evidence describes the August 3 revision, not current Client source. |
| AC-07 | Not Verifiable | Retained structural tests passed, but the current Client test project cannot build under locked restore. Rerun the Razor pairing/no-`@code` check and component tests after lock-file repair. |
| AC-08 | Met | Current API Integration `13/13` passes MVC health, CORS, ProblemDetails, runtime OpenAPI, and composition coverage. No current API source change was observed. |
| AC-09 | Blocked | Retained evidence shows an empty EF boundary and in-process registration test, but no operating Podman engine, pulled PostgreSQL image, isolated real-provider run, or complete exception closure. WSL upgrade requires administrator elevation. |
| AC-10 | Partial | Direct-family, resolved-graph, and browser-runtime review remains documented. Complete manual review of all NuGet transitives, published runtimes, PostgreSQL image layers, and PostgreSQL runtime artifacts is still absent. Current lock mismatch adds a prerequisite to refreshed inventory. |
| AC-11 | Met | User approval on 2026-08-04 accepts zero Domain/Application tests while no behavior exists. Current solution discovery lists all seven projects; invocation reached both empty projects and source/package search found no prohibited helpers. This does not represent a passing Client test run. |
| AC-12 | Not Met | The documented bare solution test command is intentionally incomplete, but current Client locked restore also prevents validating the fast Client layer. No clean-environment manual HTTPS startup and URL transcript exists. |
| AC-13 | Not Verifiable | User approvals and written wave records exist, but no independent complete orchestrator evidence proves exclusive ownership of every file, terminal, port, database, schema, container, browser profile, and generated output for every wave. |

## Definition of Done Matrix

| Definition of Done item | Verdict | Evidence or remaining work |
| --- | --- | --- |
| User approved the specification, decisions, and six waves | Met | Feature decision history records approval on 2026-08-02. |
| AC-01 through AC-13 are Met | Not Met | AC-01, AC-05, AC-06, and AC-12 are Not Met; AC-07 and AC-13 are Not Verifiable; AC-09 is Blocked; AC-10 is Partial. |
| Approved stable SDK resolves | Met | Current `dotnet --version`: `10.0.302`. |
| Approved EF tool restores | Met | No tool manifest or migration exists; no EF tool is required. |
| Deterministic locked restore succeeds | Not Met | Current Client locked restore reports `NU1004` for an ILLink tasks version mismatch. |
| Zero-warning solution build succeeds | Not Met | Client locked restore prevents a fresh solution build conclusion. |
| StyleCop is centralized and enforced | Not Verifiable | Retained evidence supports it, but a current whole-solution build cannot complete. |
| API and Client build and publish independently | Not Met | API retained evidence is intact; Client needs lock repair and a fresh independent publish. |
| OpenAPI, contracts, controller, and HTTP probe agree | Met | Current contract/integration tests and retained independent HTTP evidence agree. |
| Resource-free unit, contract, and component tests pass | Not Met | Contract tests pass and zero Domain/Application tests are approved, but Client cannot currently restore/build. |
| Real-PostgreSQL Infrastructure and API integration pass | Blocked | Podman/WSL/image/transcript prerequisite remains unavailable. |
| Approved browser evidence passes twice | Not Verifiable | Two August 3 runs passed, but do not validate the current Client revision. |
| Automated architecture checks pass | Met | Retained passing architecture evidence is not contradicted by the observed code-behind-only change. |
| Automated Razor checks pass | Not Verifiable | Rerun after Client lock-file remediation. |
| Prohibited helpers are absent | Met | Current source/package search found no AutoFixture, AutoMapper, or MediatR references. |
| Every dependency and artifact has complete dated review | Partial | Complete transitive, published-runtime, and PostgreSQL image/runtime review remains absent. |
| Every license is allowlisted or exactly excepted | Partial | Direct and browser records exist; incomplete closure review prevents a complete conclusion. |
| Wave 3 exception and full closure pass policy | Partial | Exact exception is recorded; bUnit/AngleSharp and published-output closure remains incomplete. |
| PostgreSQL exception remains narrow and fully reviewed | Partial | Narrow use is documented; real server/image/runtime closure remains incomplete. |
| Vulnerability audit has no blocking finding | Not Verifiable | Retained audit evidence exists, but current locked restore prevents refreshed validation. |
| Format verification passes | Not Verifiable | Retained success exists; do not claim current success while the worktree has a failing build gate. |
| Changed Markdown checks pass or unavailable checks are recorded | Met | Post-edit editor diagnostics report no errors; local links resolve; CRLF-aware documentation diff check passes. No repository Markdown formatter, linter, spelling, or grammar command is available, and that limitation is recorded. |
| Fresh-publish cross-origin browser workflow passes | Not Verifiable | Retained August 3 result is not current for the uncommitted Client source. |
| Secret and generated-artifact scans are clean | Not Verifiable | Retained focused scan exists; no current full scan was run in this documentation audit. |
| Every wave has approval, ownership, and completion evidence | Not Met | AC-13 lacks complete independently verifiable ownership/resource evidence. |
| `git diff --check` succeeds | Not Met | Current command exits `2` because of actual trailing whitespace in uncommitted Client source. |
| Final audit finds no stale or unmet item | Not Met | This audit identifies current stale validation and unresolved items. |
| BudgetExperiment and parent implementation remain unchanged | Met | Current worktree status identifies only a HouseholdLedger Client path; this audit did not modify BudgetExperiment or parent implementation. |

## Remaining Blockers

| Classification | Blocker | Resolution prerequisite |
| --- | --- | --- |
| Implementation | Client lock file resolves `Microsoft.NET.ILLink.Tasks` `10.0.10` while locking `[10.0.9, )`; current Client validation cannot build. | Utility Fallback must make a reviewed, deliberate lock-file reconciliation. |
| Implementation | Current uncommitted Client source has trailing whitespace, causing `git diff --check` to fail. | Blazor UI owns the source file and must remove whitespace and validate Client behavior. |
| Environment | Podman engine is unavailable until WSL reaches `2.7.11`; the upgrade requires administrator elevation. | User or authorized environment administrator must upgrade WSL and start the approved engine. |
| Evidence | No successful real-PostgreSQL provisioning/cleanup transcript or pulled-image/runtime inventory. | Persistence and Integrations plus Test Architecture require an operating approved engine and isolated test resource. |
| Evidence | Complete transitive, published-runtime, and PostgreSQL image/runtime provenance, license, notices, vulnerability, and commercial-model closure is absent. | Research and Documentation needs final dependency/image artifacts and output inventories. |
| Evidence | No clean-environment manual HTTPS startup and URL-verification transcript. | Blazor UI and ASP.NET API need an isolated two-process validation allocation after the build gate is fixed. |
| Evidence | No complete independently verifiable ownership/resource history. | Orchestrator must provide authoritative wave records for re-audit. |

## Proposed Phase 2 Plan

1. **Utility Fallback:** Reconcile the Client package lock file without changing package policy. Exclusive files: affected `packages.lock.json`, plus shared package files only if review proves necessary. Resources: dedicated terminal only. Validation: locked restore, zero-warning Release solution build, independent Client build/publish, and `git diff --check` after dependent owner work.
2. **Blazor UI:** Review the uncommitted Client code-behind against Feature 001's honest-shell and code-behind scope; remove trailing whitespace and avoid invented ledger behavior. Exclusive file: `src/HouseholdLedger.Client/Pages/CalendarPage.razor.cs`. Resources: dedicated terminal; later, allocated browser profile, ports, and output. Tests: Client component/structural tests, Razor pairing check, and fresh browser workflow after the package gate.
3. **Test Architecture:** Refresh resource-free tests and direct-W3C browser evidence against fresh API/Client publishes. Exclusive files: test projects and harnesses only if a test defect is found. Resources: isolated dynamic ports, browser profile, test output, and approved Firefox/geckodriver artifacts. Tests: focused Client, contract, API integration, HTTP smoke, then two browser runs.
4. **User or environment administrator, then Persistence and Integrations:** Upgrade WSL, start Podman, pull only the reviewed PostgreSQL image, and run the owned PostgreSQL harness to deterministic cleanup. Exclusive files: persistence/harness files only if a defect requires repair. Resources: approved WSL upgrade, Podman engine, isolated container/database/port/credentials, and cleanup transcript. Tests: real PostgreSQL Infrastructure and API integration tests.
5. **Research and Documentation:** Complete dependency closure review from finalized lock files, published outputs, pulled image digests/layers, and runtime inventories; update Feature 001 only with exact evidence. Exclusive files: separately assigned `docs/development/`, `docs/features/001-application-scaffolding.md`, and `docs/audit/`. Resources: no persistent runtime resource. Validation: source-link review, final artifact inventory, Markdown diagnostics, local-link check, and available documentation hygiene tools.
6. **Orchestrator:** Supply independent per-wave ownership/resource records, then assign a final read-only audit. Exclusive files: coordination records as applicable. Resources: authoritative terminal/port/container/browser/output allocation history. Validation: criterion-to-evidence matrix with no unresolved verdicts.

## Risks and Ownership Notes

The Client change is unowned by this audit and may be active user work. No
specialist should overwrite or revert it. The current build failure is a
feature-closure blocker regardless of whether the source change is intended.
PostgreSQL and browser operations were intentionally not run: this audit has no
allocated database, container, browser profile, port, test data, or persistent
process resource. No ownership conflict was encountered within the assigned
writable documentation paths.
