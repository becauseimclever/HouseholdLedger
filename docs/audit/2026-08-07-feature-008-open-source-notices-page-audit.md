# Feature 008: Open-Source Notices Page Final Audit

**Date:** 2026-08-07

**Feature:** [Feature 008: Open-Source Notices Page](../features/008-open-source-notices-page.md)

**Verdict:** **Not complete.** AC-05 is **Not Met**, so the Definition of Done
is not yet satisfied.

The user explicitly approved Feature 008 through completion. This is a
read-only audit of the approved scope. It does not claim full repository
dependency governance, a vulnerability clearance, or completion of Feature
003. The minimal remaining Feature 008 action is for the Documentation and
Attribution owner to create the approved source-controlled inventory record at
`docs/development/third-party-notice-inventory.md`, identify the retained
Feature008 Client and hosted output evidence there, and record the handoff to
the Feature 003 dependency-governance owner. Research and Documentation must
then re-audit Feature 008.

## Audit Scope and Evidence Boundary

Reviewed current source includes the Client manifest, notices route, layout,
router, styles, central packages and applicable locks; the focused component
and browser tests; the Feature 003 Lucide dependency-governance review; and
the retained Release publish-output records. The manifest's five entries match
the source-controlled final-output closure:

| Component | Version | Verified publication form |
| --- | ---: | --- |
| `Blazicons.Lucide` | 3.0.8 | Runtime assembly |
| `Blazicons` | 4.0.21 | Both |
| `BlazorComponentUtilities` | 1.8.0 | Runtime assembly |
| `Npgsql` | 10.0.3 | Runtime assembly |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | Runtime assembly |

The retained hosted records
`src/HouseholdLedger.Api/obj/Release/net10.0/PublishOutputs.af997bfc1b.txt`
(Feature008 closure) and `PublishOutputs.abf3d1c172.txt` (Feature008 E2E)
list all five assemblies. The retained Client Release output records identify
the three Client-side assemblies, while the API static-web-asset publish
record identifies the `Blazicons` scoped/minified CSS asset. This supports the
manifest's `Both` classification for `Blazicons`, and its runtime-assembly
classification for the other entries. These generated records are retained
local evidence, not a general-purpose, source-controlled SBOM or a complete
package-governance record.

## Acceptance Criteria

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01: Dedicated reachable page | **Met** | [OpenSourceNoticesPage.razor](../../src/HouseholdLedger.Client/Pages/OpenSourceNoticesPage.razor) declares exactly `/open-source-notices`, one `main`, and one `h1`. [MainLayout.razor](../../src/HouseholdLedger.Client/Layout/MainLayout.razor) places one native `NavLink` after the workspace grid, outside the empty `nav`; [Routes.razor](../../src/HouseholdLedger.Client/Routes.razor) retains root routing and `FocusOnNavigate` to `h1`. The focused component test verifies one footer link, its active route state, following-grid position, and no navigation links/buttons. The retained browser result is reported as 1/1 passed and its source returns to `/` and verifies the calendar heading and empty primary navigation. |
| AC-02: Complete published attribution | **Met** | [OpenSourceNoticesManifest.cs](../../src/HouseholdLedger.Client/OpenSourceNotices/OpenSourceNoticesManifest.cs) renders exactly the five verified packages at their exact versions, with project/source URLs, copyright statements, and direct full MIT, ISC, and PostgreSQL license text. It includes no restored-but-unshipped package. The scope statement accurately limits the page to confirmed published components. The component test asserts exactly this five-entry closure and every rendered license text/link. |
| AC-03: Publication-form truthfulness | **Met** | The five manifest forms match the retained Client and hosted Release output records described above. The `Blazicons` entry states both its WebAssembly assembly and CSS assets, while each other entry states a runtime assembly only. The component test asserts each visible label. |
| AC-04: Accessible no-frills notice reading | **Met** | The page uses native `main`, section, heading, list, definition-list, and anchor semantics. Required text is rendered in selectable `pre` elements with `white-space: pre-wrap`, no fixed height, and no overlay. [OpenSourceNoticesPage.razor.css](../../src/HouseholdLedger.Client/Pages/OpenSourceNoticesPage.razor.css) supplies visible focus outlines for outbound links; [MainLayout.razor.css](../../src/HouseholdLedger.Client/Layout/MainLayout.razor.css) supplies one for the auxiliary link. The retained 1440x900 browser evidence reports focused `h1` after navigation, visible entries and notice text, native links, no horizontal overflow, and visible keyboard focus. |
| AC-05: Attribution update boundary | **Not Met** | The approved feature document requires reconciliation to fresh output inventory, names the Documentation and Attribution owner, and assigns `docs/development/third-party-notice-inventory.md` as the source-controlled retained-inventory record. That file is absent. The manifest's 2026-08-07 summary and ignored generated `obj` output lists substantiate the present reconciliation, and [Dependency Governance](../development/dependency-governance.md) requires the procedure, but neither records the retained-evidence location and responsible-owner result in the required source-controlled document. |
| AC-06: Bounded scope | **Met** | The Feature 008 work consists of the manifest, notice route/styles, one auxiliary footer link/styles, two focused test files, and this audit. [Directory.Packages.props](../../Directory.Packages.props), Client and Infrastructure project files, and current locks show no Feature 008 dependency, package, or configuration change. No API, persistence, settings, ledger/Kakeibo, broad-navigation, responsive-workspace, or automated supply-chain implementation was added. Current modified Calendar files and general testing/document changes are separate user work and were not counted as Feature 008. |

## Definition of Done

| Item | Verdict | Evidence |
| --- | --- | --- |
| 1. User approval authorizes implementation | **Met** | The approved feature records the user's 2026-08-07 implementation approval; the audit request explicitly confirms approval through completion. This is authorization, not validation evidence. |
| 2. Read-only audit marks AC-01 through AC-06 Met | **Not Met** | This report maps all six criteria, but AC-05 is Not Met because the required source-controlled inventory record is absent. |
| 3. Manifest matches fresh Client and hosted final-output inventories | **Met** | The current five-entry manifest reconciles to the dedicated Feature008 Release Client/hosted output records: Client-side Blazicons family WebAssembly output, hosted Blazicons family and Npgsql assemblies, and hosted Blazicons static CSS asset. |
| 4. Precise forms and required Lucide/Feather, Blazicons, and utility notice text | **Met** | The manifest carries exact versions/forms; Kyle Herzog MIT notices; Ed Charbeneau MIT notice; Lucide ISC; and a conservative Feather MIT notice. It also carries the Npgsql PostgreSQL notices, all directly in-app with supplementary source links. |
| 5. Focused Client tests, hosted browser proof, and fresh inventory comparison pass | **Met** | Retained validation evidence reports a warning-free Client build, Feature 008 component tests 4/4 passed, and Feature 008 browser test 1/1 passed. The browser test source demonstrates it consumes explicit fresh API and Client publish paths, fixes 1440x900, uses isolated API/client ports 51391/51392, verifies the accessible journey and return to root, and disposes owned hosts/processes, profiles, output, and ports. No build, test, or browser command was run by this audit. |
| 6. Sole link/route preserve primary navigation, calendar behavior, data, packages, configuration, and information architecture | **Met** | The layout contains one post-grid auxiliary link; its `nav` remains without destinations. Route/browser assertions preserve the calendar root before and after the journey. Source and scoped worktree review identify no Feature 008 change to product data, package/configuration/lock files, or additional information architecture. |
| 7. Feature 003 evidence is handed to governance owner; no Feature 003 completion claim | **Not Verifiable** | This report identifies the Feature 008 inventory and shipped-notice evidence that must go to the Feature 003 governance owner, and it expressly retains the required narrow re-audit. No source-controlled inventory/handoff record exists, so the actual handoff cannot be verified. This report makes no completion claim for Feature 003. |

## Attribution and Governance Assessment

The page directly renders full required notices rather than outsourcing notice
delivery to URLs. Every listed package includes a source/project link and a
license/source link. The Lucide treatment is conservative: `PanelLeft` and
`PanelRight` are identified as the actual package use, the Lucide ISC text is
included, and the Feather MIT text is included because the exact icon mapping
is unavailable. The page accurately excludes proprietary HouseholdLedger
assemblies and Microsoft platform/framework assets rather than presenting them
as independent third-party notices.

Feature 008 resolves two specific Feature 003 Lucide-review blockers:

1. It provides a final Client/hosted publish inventory for the relevant
   Blazicons/Lucide closure.
2. It provides an in-app shipped-attribution mechanism, verified through the
   published-client browser journey, for Kyle Herzog, Ed Charbeneau, Lucide,
   and conservatively Feather material.

It does not resolve the Feature 003 review's missing inspectable,
target-framework vulnerability result. It also does not change that review's
required separate re-audit or transform its prior provenance, integrity, and
commercial-model evidence into a complete all-package review. The conservative
Feather treatment satisfies Feature 008 notice delivery without asserting an
exact icon-to-Feather mapping. Therefore Feature 003 remains incomplete until
its dependency-governance owner supplies the remaining evidence and Research
and Documentation performs its narrow re-audit.

## Testing Evidence and Limitations

The retained warning-free Client build and 4/4 component, 1/1 browser results
were reviewed as retained evidence; they were not re-executed. The test source
supports the claims made by those results, but tests cannot independently prove
legal completeness beyond their checked manifest data. The browser test proves
the current five-entry visible journey and selected accessibility/layout
properties at 1440x900, not every assistive-technology combination or a
mobile redesign.

The general [Testing](../development/testing.md) browser-workflow example still
uses its older generic ports and aggregate case counts. It is not the Feature
008 result source. The Feature 008 test itself requires and validates 51391 and
51392. This is a documentation-maintenance limitation, not a contradiction of
the feature-specific test contract or completion evidence.

## Ownership and Validation

This audit changed only this report under `docs/audit/`. It did not modify the
Feature 008 specification, implementation, tests, packages, locks,
configuration, artifacts, or BudgetExperiment. Product diagnostics for the
reviewed Client and focused test sources reported no errors. Final Markdown
diagnostics report no errors. The report has 122 CRLF endings and no bare LF
endings; Git's scoped `core.whitespace=cr-at-eol` diff check reports no
whitespace errors.
