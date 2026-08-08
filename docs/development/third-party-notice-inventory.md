# Third-Party Notice Inventory

## Record Purpose and Basis

This source-controlled record retains the third-party notice inventory for
Feature 008: Open-Source Notices Page. The inventory basis is the fresh Client
and hosted API Release publish output inspected on 2026-08-07, together with
the associated `.deps.json` files, physical output inspection, locked package
closure, and publish-output records. The canonical in-app notice input is
[OpenSourceNoticesManifest.cs](../../src/HouseholdLedger.Client/OpenSourceNotices/OpenSourceNoticesManifest.cs).

The 2026-08-07 reconciliation compared that manifest with fresh Client and
hosted publish outputs rather than inferring publication from a
`PackageReference` or lock file alone. It inspected the output assemblies,
the Blazicons static CSS assets, and the `.deps.json` dependency declarations.
The retained Release output records include:

- `src/HouseholdLedger.Client/obj/Release/net10.0/PublishOutputs.dec0b22e2a.txt`
  for the Feature008 closure Client output.
- `src/HouseholdLedger.Api/obj/Release/net10.0/PublishOutputs.af997bfc1b.txt`
  for the Feature008 closure hosted output.
- `src/HouseholdLedger.Api/obj/Release/net10.0/PublishOutputs.abf3d1c172.txt`
  for the Feature008 E2E hosted output confirmation.
- The corresponding Client and hosted API Release `.deps.json` files and
  static-web-asset publish manifests under `src/HouseholdLedger.Client/obj/`
  and `src/HouseholdLedger.Api/obj/`.

These generated output records are supporting retained evidence for this
snapshot; they are not an SBOM and do not replace the manifest as the
source-of-truth notice input.

## Current Published Notice Closure

The current published non-platform third-party closure is exactly the following
five resolved packages. Each component's complete required notice, including
direct license and copyright text, is rendered in the application at
`/open-source-notices` from the source-controlled manifest.

| Component | Version | Publication form | Publish and package evidence basis |
| --- | ---: | --- | --- |
| `Blazicons.Lucide` | 3.0.8 | Runtime assembly | Client and hosted output include the WebAssembly assembly; hosted output includes `Blazicons.Lucide.dll`. The Client lock records this direct Client package and its resolved version. |
| `Blazicons` | 4.0.21 | Both | Client and hosted output include the WebAssembly assembly and Blazicons scoped/minified CSS assets; hosted output includes `Blazicons.dll`. The Client lock records it as the resolved dependency of `Blazicons.Lucide`. |
| `BlazorComponentUtilities` | 1.8.0 | Runtime assembly | Client and hosted output include the WebAssembly assembly; hosted output includes `BlazorComponentUtilities.dll`. The Client lock records it as the resolved Blazicons dependency. |
| `Npgsql` | 10.0.3 | Runtime assembly | Hosted output includes `Npgsql.dll`. The hosted API lock and `.deps.json` identify the resolved runtime dependency. |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | Runtime assembly | Hosted output includes `Npgsql.EntityFrameworkCore.PostgreSQL.dll`. The central package manifest and hosted API lock and `.deps.json` identify the direct provider and resolved runtime dependency. |

The package identity and version evidence comes from the applicable committed
lock files and [Directory.Packages.props](../../Directory.Packages.props). The
publication-form evidence comes from the dated fresh outputs, `.deps.json`
files, and physical output inspection described above. The manifest is the
record of the notice text and its in-app delivery; the locks and central package
manifest do not independently prove that a package is published.

## Notice Treatment and Exclusions

The notice page carries the complete applicable MIT, ISC, and PostgreSQL
license text with required copyright text directly in the application. Source
and license links are supplementary, not the delivery mechanism.

`Blazicons.Lucide` is treated conservatively. HouseholdLedger uses `PanelLeft`
and `PanelRight` through that package. The manifest includes the Lucide ISC
notice and the Feather MIT notice because the available evidence does not map
the exact icons to Lucide's Feather-derived list. This is a conservative notice
delivery decision; it does not assert an exact icon-to-Feather derivation
mapping.

Microsoft platform/framework assets are platform exclusions under
[Dependency Governance](dependency-governance.md). They are not independently
listed as third-party package notices in this Feature 008 manifest.
HouseholdLedger assemblies are proprietary product code, not third-party
notices. These classifications describe this inventory boundary only; they do
not state that Microsoft assets or HouseholdLedger are exempt from any future
policy review.

## Update and Reconciliation Process

The Documentation and Attribution owner must reconcile this record and the
source-controlled manifest before publishing whenever a package, resolved
publish closure, static web asset, published runtime assembly, trimming
behavior, or required notice changes. The reconciliation requires a fresh
Client and hosted API publish, `.deps.json` and physical-output inspection, and
comparison of the resulting third-party notice closure and publication forms
against the manifest.

The same change must be reviewed against [Dependency
Governance](dependency-governance.md), including its published-output and
closure-review requirements. This record does not introduce or claim an
automated inventory, validation, or governance process.

## Governance Limits and Handoff

This inventory proves neither a vulnerability review nor package integrity
beyond the evidence already recorded in the [Feature 003 Lucide
Dependency-Governance Review](../audit/2026-08-06-feature-003-lucide-dependency-governance-review.md).
In particular, it does not supply that review's required inspectable
target-framework vulnerability result, nor does it replace its previously
recorded NuGet cache, lock-hash, repository-signature, provenance, or
commercial-model findings.

The inventory and shipped-notice evidence are handed to the Feature 003
dependency-governance owner for the required narrow Feature 003 re-audit. They
do not close Feature 003, its AC-06, or its Definition of Done. The current
[Feature 008 final audit](../audit/2026-08-07-feature-008-open-source-notices-page-audit.md)
records the evidence boundary and requires that final Feature 008 re-audit
after this retained inventory record is present.
