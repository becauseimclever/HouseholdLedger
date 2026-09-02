# Feature 008: Open-Source Notices Page

## Status

Status: Complete.

- Proposal date: 2026-08-06.
- User approval and implementation request: 2026-08-07.
- The final re-audit dated 2026-08-07 marks AC-01 through AC-06 and every
  Definition of Done item `Met`.
- This feature is independently observable legal and attribution information;
  it does not add ledger, Kakeibo, settings, or general navigation behavior.
- It contributes only the shipped-notice and final-output inventory evidence
  identified by the Feature 003 Lucide review. It is not a substitute for
  dependency admission review, vulnerability scanning, integrity verification,
  commercial-model review, or the Feature 003 re-audit.

## Context and Outcome

HouseholdLedger is a calendar-centered household ledger informed by Kakeibo, a
practice of recording, planning, and reflecting on household finances. A
notices page does not change that workflow. It makes the open-source material
included with the application available in a clear, factual place, without
asking a household to understand its dependency process.

The Feature 003 Lucide dependency-governance review found that no designated
shipped-attribution mechanism or final published-output inventory currently
exists. The review specifically requires notices for the applicable Lucide,
Feather-derived where applicable, Blazicons, and BlazorComponentUtilities
material, verified against the final Client and hosted output. This feature
creates the user-observable page that can carry those notices once the actual
published closure is known.

**Proposed outcome:** A user can follow a persistent, bottom-area `Open-source
notices` auxiliary/legal link to `/open-source-notices` and read the required
notices for the third-party components actually included in the published
application. The root calendar route and its primary navigation behavior stay
unchanged.

## Goals

1. Provide one dedicated, accessible in-app route for open-source notices.
2. Display complete, accurate notice information for the components included
   in the final published Client and hosted output at implementation time.
3. Clearly distinguish components shipped as static/bundled assets from those
   shipped as runtime assemblies; do not present restored-but-trimmed or
   otherwise unshipped packages as included.
4. Establish a small, source-controlled attribution update boundary so a
   dependency change cannot silently leave the page stale.
5. Use native links, landmarks, headings, and static content with a no-frills,
   accessible presentation.

## Non-Goals

- Vulnerability scanning, package integrity verification, license allowlisting,
  commercial-model review, package admission, or dependency-governance policy
  changes.
- Completing Feature 003 AC-06 or claiming Lucide governance closure without
  the separate required evidence and re-audit.
- New packages, a UI library, an automated supply-chain system, build-time
  inventory generator, API endpoint, database, migration, settings, or runtime
  configuration.
- Ledger entries, financial calculations, Kakeibo behavior, or any other
  product/business behavior.
- A broad navigation plan, additional destinations, a sitemap, or responsive
  workspace redesign.

## Route, UX, and Compliance Rules

- The page route is exactly `/open-source-notices`. It is not a calendar route,
  a settings route, or an API route.
- `MainLayout` places one native `Open-source notices` link in a small
  auxiliary/legal footer at the bottom of the workspace shell, after the
  workspace grid. The link is outside the primary `Navigation` landmark, so it
  does not turn Feature 003's intentionally empty navigation pane into a
  general destination list. No other links or destinations are introduced.
- The page has one descriptive `h1`, a `main` landmark, logical notice
  headings, native outbound links where a license or project URL is supplied,
  and visible keyboard focus. `FocusOnNavigate` continues to focus the `h1`.
  The current route is understandable without color, icon recognition, hover,
  or JavaScript-only behavior.
- Content is factual and neutral: it identifies the component, exact included
  version, publisher/copyright holder where required, license, and verbatim
  required attribution and license/notice text. For MIT and ISC material, this
  means the complete applicable license text is surfaced in-app with the
  required copyright statement. Source and license URLs are supplementary;
  they do not replace text that must be carried with the published application.
  A visible scope statement explains that the page covers components included
  in this published application, not every package ever restored during
  development.
- The page must state each component's publication form as `Static/bundled
  asset`, `Runtime assembly`, or `Both` only when supported by the fresh final
  publish inventory. It must not infer the form from a `PackageReference` or
  lock file alone.
- Styling remains restrained and readable. Notices preserve required text and
  links; long license text wraps, can be selected, and does not rely on a
  scroll trap, modal, or clipped fixed-height container.

## Attribution Source of Truth and Update Boundary

The approved first slice uses a **source-controlled, human-curated manifest**
rendered as static Client content. It does not introduce a generated-at-build
inventory or a new dependency-analysis tool. The manifest is the canonical
page input; the supporting dependency-governance record remains the canonical
evidence of admission decisions.

Before the manifest is authored or changed, its attribution owner runs the
existing fresh Client and hosted publish workflow and records a publish
inventory. The inventory determines the exact included closure and whether
each component is static/bundled, a runtime assembly, both, or absent. The
manifest then contains only included components, their publication form, and
the verbatim attribution and license/notice text that applies to each published
component. It must explicitly distinguish included third-party components from
general or proprietary dependencies that are not subject to third-party notice
delivery. The implementation must not pre-fill it with a Lucide-only list or
assume that all locked packages ship. It must include or surface required text
directly in the Client rather than rely on outbound URLs; for MIT and ISC
material, include the complete applicable license text and copyright statement.

Any change to a direct package, resolved closure, static web asset, published
runtime assembly, trimming behavior, or required notice triggers the existing
dependency-governance procedure and a manifest/published-inventory review in
the same change. The page is attribution delivery evidence, not the process
that approves a dependency.

For Feature 003's existing closure, this page may provide required shipped
notice delivery and final-output inventory evidence for the applicable Lucide,
Feather, Blazicons, and BlazorComponentUtilities material. It still leaves the
review's required inspectable vulnerability result, exact icon-to-Feather
mapping or conservative attribution decision, and all other governance evidence
to their assigned owners. A narrow Feature 003 re-audit remains required.

## Acceptance Criteria

| Criterion | Observable outcome | Completion evidence |
| --- | --- | --- |
| AC-01: Dedicated reachable page | Following the bottom-area auxiliary/legal `Open-source notices` link reaches `/open-source-notices`, whose main content has one `Open-source notices` heading. The root calendar route remains the default, and the primary navigation pane gains no destination item. | Focused Client route/component assertions and hosted desktop browser proof. |
| AC-02: Complete published attribution | The page lists every third-party component in the current final Client and hosted publish inventory that requires an included notice, with accurate component identity, exact included version, required attribution/notice text, license, and relevant native links. It does not claim that absent or trimmed restored packages ship. | Review of source-controlled manifest against retained fresh Client and hosted publish inventories; focused content assertions; manual notice comparison. |
| AC-03: Publication-form truthfulness | Every listed component is visibly classified as `Static/bundled asset`, `Runtime assembly`, or `Both`, matching retained final-output inventory evidence. | Fresh publish inventory inspection and focused rendered-content checks. |
| AC-04: Accessible no-frills notice reading | The route uses native link and landmark semantics, a logical heading hierarchy, visible keyboard focus, readable selectable notice text, and no overlay, scroll trap, clipped content, icon-only control, or dependence on JavaScript-only navigation. | Client semantic/accessibility checks and hosted browser keyboard/visible-layout proof. |
| AC-05: Attribution update boundary | A dependency or publish-output change cannot be accepted without reconciling the source-controlled manifest to fresh final-output inventory and following the existing dependency-governance review procedure. Documentation names the responsible owner and records the retained evidence location. | Scoped review of the manifest, inventory record, dependency-governance record, and implementation diff. |
| AC-06: Bounded scope | The feature adds only the notices route, one auxiliary/legal link, the static manifest/rendering needed for it, proportionate tests, and attribution documentation. It adds no package, product/data behavior, API, persistence, settings, broad navigation, or automated supply-chain system. | Scoped diff, project/package/configuration inspection, and read-only audit. |

## Validation Strategy

Focused Client component tests prove route/link semantics, the unchanged empty
primary navigation, heading/landmark structure, manifest rendering, required
publication-form labels, and notice content supplied by a deterministic test
manifest. They do not establish the real shipped closure.

The documented fresh publish workflow in
[Testing](../development/testing.md) produces the Client and hosted output used
for the required inventory comparison. Test Architecture selects the narrowest
hosted browser proof at Feature 003's `1440 x 900` desktop viewport for link
activation, page readability, keyboard focus, and preserved calendar root
behavior. Dependency Governance owns vulnerability, integrity, and admission
validation; those checks remain distinct from page tests.

## Dependency-Ordered Implementation Waves

Implementation is approved and pending specialist assignment. The orchestrator
assigns the following exclusive ownership. No wave may edit a file owned by
another wave.

| Order | Specialist | Exclusive writable ownership | Responsibility and required handoff |
| --- | --- | --- | --- |
| 1 | Documentation and Attribution | `src/HouseholdLedger.Client/Notices/OpenSourceNoticesManifest.cs` and `docs/development/third-party-notice-inventory.md` | Use the existing dependency-governance and fresh-publish procedures to determine the actual shipped closure; author the canonical static manifest and retained inventory record. Every manifest entry must distinguish third-party components from proprietary/general dependencies and include exact version, publication form, applicable attribution, and verbatim required notice text. Include the complete MIT or ISC license text and copyright statement in-app where applicable; treat URLs as supplementary. Do not edit layout, pages, tests, projects, packages, or configuration. Hand the manifest's stable content contract to wave 2 and evidence locations to wave 4. |
| 2 | Blazor UI | `src/HouseholdLedger.Client/Layout/MainLayout.razor`, `MainLayout.razor.css`, `src/HouseholdLedger.Client/Pages/OpenSourceNoticesPage.razor`, `OpenSourceNoticesPage.razor.cs`, and `OpenSourceNoticesPage.razor.css` | Add only the auxiliary footer link, route page, and accessible static rendering of wave 1's manifest. Do not edit the manifest, attribution documentation, tests, projects, packages, or non-Client code. Hand selectors and semantic expectations to wave 3. |
| 3 | Test Architecture | Only feature-specific files under `tests/HouseholdLedger.Client.ComponentTests/` and `tests/HouseholdLedger.EndToEndTests/` | Add focused route, semantic, rendered-notice, unchanged-navigation, and browser checks for AC-01 through AC-04. Do not alter production, manifest, attribution documentation, package policy, or publish workflow. |
| 4 | Research and Documentation | One new assigned report under `docs/audit/` | Audit AC-01 through AC-06 against current code, manifest, retained publish inventories, notices, and command/browser evidence. Do not edit implementation, tests, package/configuration files, or this specification. |

Wave 1 must complete its final-output inventory before wave 2 receives its
manifest contract. Wave 3 starts after wave 2 releases its files. Wave 4 starts
only after the evidence from waves 1 through 3 is retained. The Documentation
and Attribution owner and Blazor UI owner have no shared writable file.

## Definition of Done

This feature is done only when all of the following are true:

1. The recorded user approval of 2026-08-07 authorizes implementation under
  this approved scope; it is not completion evidence.
2. A read-only audit marks AC-01 through AC-06 `Met` using current code and
   retained publish, test, and browser evidence.
3. The audit verifies that the page's source-controlled manifest matches fresh
   Client and hosted final-output inventories and covers every included
   component requiring notice.
4. The audit verifies the page's precise publication-form labels and the
   required notice/attribution text, including applicable Lucide/Feather,
   Blazicons, and BlazorComponentUtilities material when those components are
   included.
5. Focused Client tests and the documented hosted browser proof pass; the
   fresh-publish inventory comparison is retained.
6. The audit verifies the sole auxiliary/legal link and notices route did not
   change primary navigation, calendar behavior, product data, packages,
   configuration, or broader information architecture.
7. Any Feature 003 evidence supplied by this work is handed to its dependency
   governance owner and a separate narrow Feature 003 re-audit; this feature
   does not itself claim Feature 003 completion.

## Approved Scope Decisions

The user approved the following decisions on 2026-08-07:

1. The page route is `/open-source-notices`, reached only through one
  bottom-area auxiliary/legal link outside the empty primary navigation.
2. The first slice uses a source-controlled, human-curated manifest rendered
  as static Client content. It introduces no build-time inventory generator or
  other supply-chain inventory tooling.
3. The manifest directly carries the required attribution and license/notice
  text for every included component. For MIT and ISC material, it carries the
  complete applicable license text and required copyright statement. Native
  external source and license links may assist readers but are not treated as
  the notice-delivery mechanism.
4. Initial implementation is desktop-only, consistent with Feature 003's
  workspace scope. This does not authorize a mobile workspace redesign.

No material approval blocker remains for the first implementation wave. The
actual shipped closure, exact publication forms, and required notices remain
implementation-time facts to establish from fresh Client and hosted publish
inventories; they are not decided by this approval.

## Decision History

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-08-06 | Create a dedicated in-app notices page as one observable feature. | Shipped attribution is user-visible and distinct from dependency-review records. |
| 2026-08-06 | Use `/open-source-notices` from one bottom-area auxiliary/legal footer link, outside primary navigation. | It makes the page reachable while preserving Feature 003's explicitly empty navigation pane and avoiding a broader information-architecture decision. |
| 2026-08-06 | Recommend a source-controlled, human-curated manifest reconciled to fresh final publish inventory. | Existing policy requires published-output inspection but provides no designated notice mechanism; a small static manifest avoids inventing a build-time supply-chain system. |
| 2026-08-06 | Keep Feature 003 governance completion separate. | The Lucide review requires several evidence types beyond shipped notice delivery and inventory. |
| 2026-08-07 | User approved Feature 008 and requested implementation. | The draft's recommended route, link placement, static manifest, and desktop-only scope are now authorized. |
| 2026-08-07 | Direct in-app required notice delivery is the approved conservative treatment. | Current dependency governance requires canonical license expressions and required notices for the complete closure, while the Feature 003 review says MIT/ISC copyright and license notices cannot be omitted. Complete MIT/ISC text with copyright statements avoids treating outbound URLs as the delivery mechanism. |

## References and Research Limits

- [Feature 003: Workspace Navigation and UI Foundation](003-workspace-navigation-and-ui-foundation.md)
  defines the empty primary navigation pane, desktop workspace, and the
  Lucide exception.
- [Feature 003 Lucide Dependency-Governance Review](../audit/2026-08-06-feature-003-lucide-dependency-governance-review.md)
  identifies the missing notice, inventory, published-output, and remaining
  governance evidence.
- [Dependency Governance](../development/dependency-governance.md) requires
  complete closure review and published-output inspection after package changes.
- [Testing](../development/testing.md) documents the existing fresh Client and
  hosted publish workflow used as inventory evidence.

This approved specification is grounded in local source and documentation
inspected on 2026-08-06 and the user approval recorded on 2026-08-07. It does
not claim implementation, a current shipped inventory, or that any existing
package's notice is already delivered to users.
