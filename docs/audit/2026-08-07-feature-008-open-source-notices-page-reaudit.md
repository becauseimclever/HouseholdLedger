# Feature 008: Open-Source Notices Page Final Re-Audit

**Date:** 2026-08-07

**Feature:** [Feature 008: Open-Source Notices Page](../features/008-open-source-notices-page.md)

**Verdict:** **Complete.** All Feature 008 acceptance criteria and Definition
of Done items are **Met**.

This is a read-only, narrow re-audit after the prior final audit found only
AC-05 unmet. The new, source-controlled [Third-Party Notice
Inventory](../development/third-party-notice-inventory.md) supplies the
previously missing retained reconciliation and handoff record. No build, test,
browser, or publish command was run for this re-audit; it evaluates current
sources and retained evidence.

## Evidence Boundary

The manifest is the canonical in-app notice input. The inventory records the
2026-08-07 fresh Client and hosted Release publish reconciliation, associated
`.deps.json` and physical-output inspection, and retained `PublishOutputs`
records. It identifies exactly this five-package non-platform third-party
notice closure:

| Component | Version | Publication form |
| --- | ---: | --- |
| `Blazicons.Lucide` | 3.0.8 | Runtime assembly |
| `Blazicons` | 4.0.21 | Both |
| `BlazorComponentUtilities` | 1.8.0 | Runtime assembly |
| `Npgsql` | 10.0.3 | Runtime assembly |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | Runtime assembly |

The retained Client closure record and the Feature008 hosted closure and E2E
confirmation records support that scope. The hosted closure record lists the
five assemblies, and the inventory records that the Blazicons scoped/minified
CSS assets establish its `Both` classification. This is a retained notice
inventory, not an SBOM or a general dependency-governance closure.

## Acceptance Criteria

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01: Dedicated reachable page | **Met** | [OpenSourceNoticesPage.razor](../../src/HouseholdLedger.Client/Pages/OpenSourceNoticesPage.razor) defines exactly `/open-source-notices`, one `main`, and one `h1`. [MainLayout.razor](../../src/HouseholdLedger.Client/Layout/MainLayout.razor) has one post-grid auxiliary `NavLink`, outside the empty `nav`; [Routes.razor](../../src/HouseholdLedger.Client/Routes.razor) retains root routing and `FocusOnNavigate` to `h1`. Focused component and retained browser evidence verify the link, navigation, route, and preserved calendar root. |
| AC-02: Complete published attribution | **Met** | [OpenSourceNoticesManifest.cs](../../src/HouseholdLedger.Client/OpenSourceNotices/OpenSourceNoticesManifest.cs) contains exactly the inventory's five components, exact versions, forms, notices, copyright statements, complete MIT, ISC, and PostgreSQL license text, and supplementary native links. The inventory confirms the five-package closure is limited to current published non-platform third-party material, not restored-but-unshipped packages. The component test asserts the exact closure and rendered notice text. |
| AC-03: Publication-form truthfulness | **Met** | The manifest's forms agree with the retained fresh Client and hosted publish inventory: `Blazicons` is both a WebAssembly assembly and static CSS assets; the other four listed packages are runtime assemblies. The inventory bases those conclusions on publish output, `.deps.json`, static-web-asset, and physical-output inspection rather than a package reference or lock file alone. Focused rendering assertions cover every visible label. |
| AC-04: Accessible no-frills notice reading | **Met** | The page uses native `main`, section, heading, list, definition-list, and anchor semantics. Notice text is selectable `pre` content with `white-space: pre-wrap`, no fixed height, overlay, or scroll trap; page and auxiliary-link styles provide visible keyboard focus. Retained 1440 x 900 browser evidence reports focused `h1`, visible entries and notices, native accessible external links, and no horizontal overflow. |
| AC-05: Attribution update boundary | **Met** | The [inventory](../development/third-party-notice-inventory.md) is the exact retained source-controlled record that the prior audit found absent. It names the Documentation and Attribution owner; identifies the canonical manifest; records the Client and hosted final-output evidence locations; defines the five-package closure and in-app `/open-source-notices` delivery; distinguishes locks from publication evidence; and requires fresh Client/hosted publish, `.deps.json`, physical-output, manifest, and [Dependency Governance](../development/dependency-governance.md) reconciliation whenever package, closure, static asset, runtime assembly, trimming, or notice changes. It also records the Feature 003 handoff and limitations. |
| AC-06: Bounded scope | **Met** | The reviewed Feature 008 surface is the static manifest, one notices page and styles, one auxiliary/legal link and styles, focused tests, the inventory, and audits. It adds no package, API, persistence, settings, automated supply-chain system, product/data behavior, or broader navigation. The route remains an auxiliary footer destination outside the deliberately empty primary navigation. |

## Definition of Done

| Item | Verdict | Evidence |
| --- | --- | --- |
| 1. User approval authorizes implementation | **Met** | The approved feature records the 2026-08-07 approval, and the audit request confirms approval through completion. This is authorization only, not validation evidence. |
| 2. Read-only audit marks AC-01 through AC-06 Met | **Met** | This re-audit marks every approved criterion Met using current code and retained evidence. |
| 3. Manifest matches fresh Client and hosted final-output inventories | **Met** | The inventory records the fresh Client and hosted reconciliation and exact evidence locations; its five-package closure matches the current source-controlled manifest. |
| 4. Precise forms and required notices, including applicable Lucide/Feather, Blazicons, and utility material | **Met** | The manifest renders exact versions/forms and in-app Kyle Herzog MIT, Ed Charbeneau MIT, Lucide ISC, and conservative Feather MIT notices, plus Npgsql PostgreSQL notices. The inventory retains the conservative no-exact-icon-mapping limitation without omitting either applicable Lucide or Feather notice. |
| 5. Focused Client tests, hosted browser proof, and fresh inventory comparison pass | **Met** | Retained evidence reviewed by the prior audit reports warning-free Client build, focused component tests 4/4 passed, and Feature 008 hosted browser proof 1/1 passed; the current test sources still exercise the documented route, content, readability, focus, and root-return behavior. The inventory now retains the fresh-publish comparison. These commands were not re-executed. |
| 6. Sole link and route preserve primary navigation, calendar behavior, data, packages, configuration, and information architecture | **Met** | Current layout, router, component tests, retained browser journey, and scoped feature review establish one auxiliary link, no primary-navigation destination, root-calendar preservation, and no Feature 008 product-data, package, configuration, or broader-navigation change. |
| 7. Feature 003 evidence is handed to governance owner without a Feature 003 completion claim | **Met** | The inventory explicitly hands its inventory and shipped-notice evidence to the Feature 003 dependency-governance owner for that feature's required narrow re-audit. It expressly says that this does not close Feature 003, AC-06, or its Definition of Done. This satisfies Feature 008's handoff boundary; it does not claim that the separate Feature 003 re-audit has completed. |

## Inventory Scope and Limits

The updated record avoids an inventory overclaim. It explicitly states its
published non-platform third-party scope and five-package closure, identifies
the manifest as the canonical in-app input and `/open-source-notices` as the
notice-delivery route, and separates source-of-truth notice content from the
supporting published-output records. It requires reconciliation rather than
assuming locks prove delivery. Microsoft platform/framework assets are policy
exclusions, and HouseholdLedger assemblies are proprietary product code; both
are explicitly excluded from this third-party notice manifest without claiming
they are exempt from future review.

## Feature 003 Boundary

Feature 008 can audit-pass without a new Feature 003 vulnerability result.
Its approved scope expressly excludes vulnerability scanning and Feature 003
completion. The completed Feature 008 work contributes the retained Client and
hosted publish inventory plus an in-app, final-output-tested delivery mechanism
for applicable Blazicons/Lucide, conservative Feather, and
BlazorComponentUtilities notices.

The separate Feature 003 risk remains: its Lucide review records no successful,
inspectable target-framework vulnerability result enumerating the closure. The
inventory does not claim to supply that evidence, nor integrity, provenance, or
commercial-model closure beyond the existing review. The Feature 003
dependency-governance owner must complete its remaining evidence and arrange
the separate narrow re-audit before Feature 003 can claim governance closure.

## Ownership and Validation

**Strict ownership:** this re-audit creates only this report under `docs/audit/`.
It does not modify product code, tests, inventories, configuration, artifacts,
the Feature 008 specification, or `BudgetExperiment`.

Markdown diagnostics report no errors. This report has 100 CRLF line endings and
zero bare LF endings. Git's scoped `core.whitespace=cr-at-eol` no-index diff
check reports no whitespace diagnostics; its exit code 1 is expected because it
compares this new report with `NUL`. No build, test, browser, publish, or
vulnerability-scan command is part of this re-audit.
