# Feature 001 Hosting Scope-Change Audit

## Scope

**Date:** 2026-08-04

**Source of truth:**
[Feature 001: Application Scaffolding](../features/001-application-scaffolding.md)

**Decision source:** User-approved hosting-scope revision on 2026-08-04.

**Verdict:** Approved specification revision; implementation and replacement
validation are not complete.

The user decided that the built-in standalone Blazor WebAssembly Client is
served from inside the API host. Visiting the API host must present the Client,
while MVC `/api/v1` and checked OpenAPI remain independently consumable by
alternate UIs. This audit records the scope effect only. It does not inspect,
modify, or approve production code, tests, runtime configuration, generated
artifacts, or BudgetExperiment.

## Revised Contract

- The Client remains a standalone WebAssembly project that builds and publishes
  independently and references only API Contracts.
- The API has no CLR project reference to Client and does not require Client
  startup to build, test its HTTP boundary, or serve custom HTTP/OpenAPI
  clients.
- A declared, versioned Client publish artifact is composed into the API
  deployment package and served at the API root.
- MVC `/api/v1` and `/openapi/**` route ahead of static-file and SPA fallback
  handling. Unknown API or OpenAPI paths retain API/OpenAPI not-found semantics;
  they never return Client HTML.
- No server-side Blazor, prerendering, Interactive Server, Interactive Auto, or
  SignalR UI circuit is introduced.

## Evidence Impact

| Evidence area | Status after revision | Reason and required replacement |
| --- | --- | --- |
| API project graph and independently consumable MVC/OpenAPI boundary | Unaffected unless changed by implementation | Existing facts may be reused under the feature validation matrix. |
| Independent Client publish | Historical, insufficient alone | Revalidate only if the Client publish input or metadata changes; retain artifact identity for API composition. |
| Independent API HTTPS health/OpenAPI proof | Historical, insufficient alone | Revalidate API-hosted package behavior and fallback exclusions. |
| Two-host browser, CORS, and static-host checks | Invalidated for affected criteria | Replace with one API-hosted desktop/mobile browser workflow and one scoped custom-client HTTP/OpenAPI compatibility smoke. |
| AC-05, AC-06, AC-08, and AC-12 | Not Met pending replacement evidence | The new package, root-hosted UI, fallback, and single-host workflow are unimplemented/unverified. |
| AC-01 through AC-04, AC-07, AC-09 through AC-11, and AC-13 | No automatic verdict change | Reuse only under the feature document's stated invalidation rules; existing supply-chain and ownership gaps remain unresolved. |

The final 2026-08-04 Definition of Done audit remains a valid record of the
prior scope's evidence and gaps. It cannot certify the revised hosting behavior.

## Required Follow-Up

Implementation follows the approved dependency order in the feature document:

1. ASP.NET API establishes routing and fallback contract tests.
2. Blazor UI produces the versioned, independently publishable Client artifact.
3. A deployment-composition owner integrates the artifact into the API package
   without a Client CLR reference.
4. Test Architecture runs the primary single-host browser workflow and the
   narrowly scoped custom-client API compatibility smoke.
5. Research and Documentation audits retained evidence against the revised
   acceptance criteria and Definition of Done.

Every phase needs exact exclusive files and dedicated resources from the
orchestrator. The validation matrix is the authority for primary commands,
owners, evidence retention, invalidators, bounded retries, failure
classification, and cleanup. No two-host manual workflow may be repeated as a
primary proof.

## Documentation Validation

This record requires Markdown diagnostics, local-link validation, and
CRLF-aware diff hygiene before closure. No repository Markdown formatter,
linter, link checker, spelling checker, or grammar checker has been identified
in the repository; availability and exact results are recorded in the
documentation completion report rather than inferred here.
