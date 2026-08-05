# Feature 002: Minimal Hosted Sample Shell

## Status

Status: Approved / Complete

- Approval authority: the user. The orchestrator cannot approve this document
  on the user's behalf.
- Approval record: the user explicitly approved implementation through
   completion on 2026-08-04.
- Completion record: the read-only Definition of Done audit dated 2026-08-04
   marks AC-01 through AC-05 and every Definition of Done item `Met`.
- Implementation status: complete. The approved thin slice uses the
   framework-supported hosted Blazor WebAssembly mechanism and has focused
   build, component, HTTP, and browser validation evidence.
- Relationship to Feature 001: Feature 001 is paused, not complete or
  abandoned. Feature 002 isolates one smaller runtime proof and does not
  inherit Feature 001 acceptance evidence, incomplete work, or completion
  claims.

## Context and Outcome

HouseholdLedger will later support a calendar-centered household ledger informed
by Kakeibo, a household-accounting practice that combines recording, planning,
and reflection. Before that product work proceeds, the user wants to verify a
single, intentionally plain application path: start one application URL and
see temporary sample content in a browser.

The outcome is a bare-bones hosted Blazor WebAssembly Client served from one
API process at a known HTTPS root URL. The browser displays neutral temporary
content, a document title, and an accessible main heading. The content must not
suggest that ledger, Kakeibo, account, budget, or other financial behavior
exists.

## Goals

1. Use the standard hosted Blazor WebAssembly direction: the API directly
   references the Client and uses built-in, supported .NET hosting and static
   web-asset behavior. The exact compatible SDK/template mechanism must be
   confirmed by the owning implementation specialist before code changes.
2. Start one API process at one known HTTPS root URL and render the Client at
   that root.
3. Present only a short neutral placeholder, such as `Temporary sample
   content`, with a document title and one accessible `main` heading.
4. Retain `GET /api/v1/health` and OpenAPI as minimal direct non-UI HTTP
   endpoints for alternate clients.
5. Preserve a one-way Client boundary: the Client must not directly reference
   Domain, Application, Infrastructure, or API implementation projects.

## Non-Goals and Explicitly Out of Scope

- Completing, resuming, or validating Feature 001.
- Ledger, Kakeibo, account, budget, category, calendar, persistence, or fake
  API behavior.
- Server-side Blazor, SSR, prerendering, or a SignalR UI circuit.
- CORS work, a second Client host, two-host workflows, browser matrices, or
  broad end-to-end coverage.
- Databases, migrations, containers, deployment packaging, supply-chain
  review, manifests, hashes, custom static providers, manual staging, or
  pre/post-processing scripts.
- Changes to project/package/solution configuration beyond the approved work
  needed to establish the intended API-to-Client relationship.

## Domain and UX Rules

- The temporary content is visibly a placeholder and is neutral: it contains
  no invented financial records, totals, account names, or Kakeibo guidance.
- The browser document has a descriptive temporary title.
- The root page has exactly one programmatically determinable main heading in
  its main content. A plain layout and normal browser navigation are enough;
  no product workflow is implied.
- MVC `/api/v1` and OpenAPI remain direct HTTP/OpenAPI surfaces. They are not
  simulated by the Client and do not require the Client to call them.

## Acceptance Criteria

| Criterion | Observable outcome | Completion evidence |
| --- | --- | --- |
| AC-01: Hosted project relationship | Project references show that the API intentionally references the Client for framework-supported hosted Blazor WebAssembly behavior, while the Client does not directly reference API implementation, Domain, Application, or Infrastructure. | Focused project-reference inspection and successful API build. |
| AC-02: Single-host root render | Starting one API process at a known HTTPS root URL lets a browser load the built-in Client and display only neutral temporary sample content. | The authoritative browser proof defined below. |
| AC-03: Accessible shell identity | The rendered root page exposes a descriptive document title and one accessible main heading. | The authoritative browser proof defined below. |
| AC-04: Minimal external API surfaces | Direct requests to `GET /api/v1/health` and the configured OpenAPI endpoint succeed without relying on the Client. | Focused HTTP checks against the same running API host. |
| AC-05: Focused validation | The affected API and Client build/test checks selected by Test Architecture pass without warnings or errors. | Retained focused command results; a browser test is added only if Test Architecture determines it is the simplest way to establish the authoritative proof. |

## Authoritative Proof and Validation Boundaries

There is one authoritative browser proof: start the API at one known HTTPS URL,
open its root URL in one browser, and verify the visible document title and main
heading with neutral temporary sample content. This is the only browser proof
required by Feature 002; it does not create a browser matrix or prove Feature
001.

The health and OpenAPI requests are supporting direct HTTP checks, not separate
browser workflows. Test Architecture chooses the narrowest focused build/test
commands and decides whether a simple automated browser assertion is necessary.
No server, browser, port, database, or container is used during this
documentation phase.

## Proposed Implementation Wave

After explicit user approval and exclusive file/resource assignments, use one
minimal sequential wave:

| Order | Specialist | Proposed responsibility and files | Resources | Validation and dependency |
| --- | --- | --- | --- | --- |
| 1 | ASP.NET API | Confirm the compatible current SDK/template mechanism; update `src/HouseholdLedger.Api/HouseholdLedger.Api.csproj` and `src/HouseholdLedger.Api/Program.cs` only as needed for the intentional API-to-Client hosted-WASM relationship and retained health/OpenAPI endpoints. | Dedicated terminal, one known HTTPS port. | API build, direct health/OpenAPI checks, and one-host startup. This controls the Client host. |
| 2 | Blazor UI | Update the minimal shell under `src/HouseholdLedger.Client/` only as needed for neutral placeholder content, document title, and accessible main heading. | Dedicated terminal; uses the API host assigned in step 1 only after the API owner releases it. | Client-focused build/component check where available, then the single root-URL browser proof. Depends on step 1. |
| 3 | Test Architecture | Add or select only the smallest focused automated check needed to make the one-host render proof repeatable, if a manual browser observation is not sufficient under the repository's test policy. | Dedicated terminal, one isolated HTTPS port, and one browser profile only if needed. | Focused build/test result and the one authoritative browser proof. Depends on steps 1-2. |

Research and Documentation audits the completed work against this specification
after owners supply their evidence. It does not implement or test the feature.

## Definition of Done

Feature 002 is done only when all of the following are true:

1. The user has explicitly approved this specification before implementation.
2. A read-only audit marks AC-01 through AC-05 `Met` using current, concrete
   implementation and validation evidence.
3. The audit confirms the one-way Client dependency boundary, the framework-
   supported hosted-WASM approach, and the absence of server-side Blazor, SSR,
   prerendering, and SignalR UI circuits.
4. The sole browser proof shows the one API HTTPS root URL rendering the neutral
   placeholder with its title and accessible main heading.
5. The audit finds no undocumented scope expansion, no financial or fake API
   content, and no claim that Feature 001 is complete.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-04 | Created as a draft that pauses active Feature 001 work and isolates a minimal hosted Blazor WebAssembly sample shell. | The user requested a single no-frills runtime proof before broader scaffold work continues. |
| 2026-08-04 | User approved implementation through completion. | The user approved this feature specification as the bounded hosted-shell thin slice. |
| 2026-08-04 | Read-only Definition of Done audit marked the feature complete. | Current implementation inspection and retained focused validation evidence satisfy AC-01 through AC-05 and every Definition of Done item. |

## Open Questions

All implementation and validation questions were resolved by the completed
specialist work and recorded in the 2026-08-04 Definition of Done audit.
