# Feature 001 Simplification Scope Record

## Record

**Date:** 2026-08-04

**Source of truth:**
[Feature 001: Application Scaffolding](../features/001-application-scaffolding.md)

**Decision source:** Fresh user-approved architecture direction on 2026-08-04.

**Verdict:** The simplified specification is approved. This record does not
authorize implementation and does not claim a browser proof or Feature 001
completion.

## Scope Change

The immediately prior 2026-08-04 simplification retained external Client publish
artifacts and a composition step. This fresh user direction supersedes it. The
user decided that the API should reference the Client UI directly, enable
built-in Blazor hosting, and use no extra scripts or pre/post-processing.

After this decision, the minimum scaffold is the standard .NET hosted Blazor
WebAssembly arrangement: `HouseholdLedger.Api` directly references
`HouseholdLedger.Client`, enables its built-in hosting/static-asset behavior,
and runs as one API process at one URL. Loading that root URL starts WebAssembly
and displays a basic shell, title, and main heading. API `/api/v1/...` and
OpenAPI remain external HTTP/OpenAPI surfaces for alternate clients.

The Client must not reference API implementation, Application, Infrastructure,
or Domain. Alternate-client compatibility means the HTTP endpoints and OpenAPI
remain available; it does not require the API and Client assemblies to deploy
separately or prohibit the intentional API-to-Client reference. No SSR,
server-side Blazor, prerendering, or SignalR UI circuit is in scope.

## Acceptance and Definition of Done

The revised source specification defines four proportional criteria:

1. The API project builds with the documented SDK.
2. The API-to-Client reference is intentional, and the Client does not point
   back to server implementation projects.
3. One API process and root URL host the WASM shell while `/api/v1/...` and
   OpenAPI remain direct HTTP/OpenAPI surfaces.
4. A browser loading the known API HTTPS URL shows the Client document title and
   one main heading.

The sole authoritative browser validation is: build the API project, launch the
API at a known HTTPS URL, open that URL in a browser, and assert that the
visible shell rendered. Health and OpenAPI receive at most minimal direct
checks. A loading failure is repaired by the owner of the controlling component
(API hosted-WASM startup, Client shell, or browser harness), then that proof is
rerun.

## Planned Obsolete Implementation Work

The following are planned simplification surfaces. This record neither directs
file deletion nor modifies implementation.

| Observed surface | Owner | Work to remove or reduce |
| --- | --- | --- |
| `src/HouseholdLedger.Api/HouseholdLedger.Api.csproj` and `src/HouseholdLedger.Api/Program.cs` | API specialist | Add the Client project reference and configure built-in hosted-WASM/static-asset behavior. |
| `src/HouseholdLedger.Client/` | Blazor UI, only if needed | Adjust the component shell only when necessary for a basic title and main heading. |
| `scripts/Compose-HostedClientPackage.ps1` and related custom artifact surfaces | Utility Fallback | Remove obsolete scripts, staging, manifests, inventories, hashes, providers, and pre/post-processing. |
| Assigned browser-proof files | Test Architecture | Replace multi-host, CORS, inventory, and excessive assertions with one API-URL browser proof. |

## Dependency-Ordered Handoff

1. **API specialist:** add the intentional API-to-Client project reference and
   built-in hosting/startup configuration. Resources: assigned terminal only.
2. **Blazor UI, only if needed:** adjust the basic shell title and main heading.
   Resources: assigned terminal only.
3. **Utility Fallback:** remove obsolete composition scripts and artifacts as
   shared tooling. Resources: assigned terminal only.
4. **Test Architecture:** add and run one browser proof against one known API
   HTTPS URL. Resources: assigned terminal, one HTTPS port, and browser profile.
5. **Research and Documentation:** audit the four criteria and simplified
   Definition of Done after implementation owners provide evidence. Resources:
   assigned terminal only.

## Audit Limits

This documentation audit inspected the approved Feature 001 document and the
direct implementation surfaces named above. It did not start a server, browser,
database, container, or other runtime resource. It did not modify production
code, tests, scripts, project files, package configuration, or BudgetExperiment.

The prior [hosting scope-change audit](2026-08-04-feature-001-hosting-scope-change-audit.md)
and immediately prior external-composition simplification are historical
records. Neither overrides this fresh approved hosted-Blazor direction.
