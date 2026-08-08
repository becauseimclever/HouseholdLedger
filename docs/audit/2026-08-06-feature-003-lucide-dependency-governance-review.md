# Feature 003 Lucide Dependency-Governance Review

## Scope and Verdict

**Date:** 2026-08-06

**Feature:** [Feature 003: Workspace Navigation and UI Foundation](../features/003-workspace-navigation-and-ui-foundation.md)

**Verdict:** **Not approved for full dependency-governance closure.** The user
explicitly accepts Lucide regardless of license and requests completion of its
approval. That decision resolves the license-allowlist decision for this
Feature 003 icon-source exception only. It does not waive the repository's
requirements for closure inventory, notice handling, vulnerability evidence,
or published-output review. Those requirements are not yet met or verifiable.

This review closes the prior lack of a Lucide-specific governance record. It
does **not** close Feature 003 AC-06 or its related Definition of Done items.
A narrow Feature 003 re-audit is required after the blockers below have current
evidence and an authorized owner has made any required shipped-attribution
change.

## Governed Closure

The Client targets `net10.0`. Its committed lock file resolves the following
Lucide-specific closure for that target framework. The existing Microsoft
shared-framework dependency edge is already covered by the repository's
direct-package review and is not a new Lucide-specific component.

| Role | Package and version | Locked dependency edge | Lock content hash (SHA-512, Base64) |
| --- | --- | --- | --- |
| Direct, Client-only | `Blazicons.Lucide` 3.0.8 | `Blazicons` 4.0.21 | `fcoaK2/g1l93rqOFIsYG8nyRLrSIYUmA0zOnqDTjpkjAcathhSqeRqd+0pLjgxKViZ1+I/+HMWvEs5OySYilaQ==` |
| Transitive | `Blazicons` 4.0.21 | `BlazorComponentUtilities` 1.8.0; `Microsoft.AspNetCore.Components.Web` 10.0.10 | `KBMo/nlpJBkYI27XM8atlEF+YYFUxm11JuaHOShTThYD/4O1AtNItP9NLqF4VD31PojL4QNL+R9Q1zOcKbDB4w==` |
| Transitive | `BlazorComponentUtilities` 1.8.0 | None | `M0HwFRId0RLNxDlG0UntB92oeduAaHHhgtUrHb9eAw6KzOVMemlxY8sNWhDQWiLfBCXIVnd/wtm6mmQbURfzfA==` |

`Blazicons.Lucide` is the only project-level `PackageReference` matching this
family, and it is in `src/HouseholdLedger.Client/HouseholdLedger.Client.csproj`.
The central version is in `Directory.Packages.props`. Other project lock files
and generated `obj` records resolve the graph because they consume the Client
or hosted Client output; they are not direct package declarations. This is a
Client-only direct-reference scope, not a claim that the hosted application
does not publish Client assets.

## Evidence and Findings

### Provenance and Integrity

All three local cache metadata records identify
`https://api.nuget.org/v3/index.json` as their source. Each cache hash matches
the Client lock-file hash above. The following commands completed without an
error and reported a NuGet.org repository signature:

```powershell
dotnet nuget verify --all C:\Users\thegu\.nuget\packages\blazicons.lucide\3.0.8\blazicons.lucide.3.0.8.nupkg
dotnet nuget verify --all C:\Users\thegu\.nuget\packages\blazicons\4.0.21\blazicons.4.0.21.nupkg
dotnet nuget verify --all C:\Users\thegu\.nuget\packages\blazorcomponentutilities\1.8.0\blazorcomponentutilities.1.8.0.nupkg
```

The command reported the direct package's NuGet.org repository certificate
SHA-256 as `1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D`,
the `Blazicons` certificate as the same value, and the utility package's
certificate as `5A2901D6ADA3D18260B9C6DFE2133C95D74B9EEF6AE0E5DC334C8454D1477DF4`.
The command output listed each expected content hash and did not report a
verification failure.

The cached nuspecs identify these publishers and source commits:

| Package | Publisher and repository evidence |
| --- | --- |
| `Blazicons.Lucide` 3.0.8 | Author Kyle Herzog; `https://github.com/kyleherzog/Blazicons.Lucide`; repository commit `0528196e60d2ba731470fa38992661afec432b06`. |
| `Blazicons` 4.0.21 | Author Kyle Herzog; `https://github.com/kyleherzog/Blazicons`; repository commit `5e55c733ca9302329007d49c33856152cb88a923`. |
| `BlazorComponentUtilities` 1.8.0 | Author Ed Charbeneau; `https://github.com/EdCharbeneau/CssBuilder` in the nuspec, whose current GitHub page redirects to `EdCharbeneau/BlazorComponentUtilities`. |

### License and Attribution

The locked source commits for both Blazicons repositories contain MIT license
text: the wrapper integration is copyright Kyle Herzog (2024), and Blazicons is
copyright Kyle Herzog (2022). `BlazorComponentUtilities` declares a file
license in its nuspec and bundles `LICENSE.txt`, an MIT notice naming Ed
Charbeneau (2011-2019).

The `Blazicons.Lucide` package README credits Lucide but contains no license
declaration or bundled license/notice. The current [Lucide license page](https://lucide.dev/license)
(accessed 2026-08-06) states ISC for Lucide and separately describes MIT terms
for listed Feather-derived icons. The exact two icons used by Feature 003 and
their derivation status were not mapped to that list. The user license decision
permits this license uncertainty for the narrow exception, but it does not make
the required copyright and license notices distributable by omission.

Neither Blazicons package's nuspec declares a license or includes a license or
notice file in the extracted package content. Their upstream source MIT files
are provenance evidence, not a substitute for a notice carried in the package
or final published output.

No repository-designated third-party inventory or shipped-notice file exists:
the policy's required record location is documentation, but it does not name a
file that satisfies downstream MIT or ISC notice distribution. This report is
the required governance record; it is not evidence that notices reach a
published Client or hosted output. An owner authorized to change published
attribution must decide and implement the distribution mechanism, then retain
the final-output inventory and notice verification. That work is outside this
documentation-only assignment.

### Commercial Model and Maintenance Risk

The NuGet package pages and linked official GitHub repositories were reviewed
on 2026-08-06. They identify public source repositories and do not advertise a
paid software edition, hosted paid counterpart, dual commercial license, or
project-operated paid feature tier for these three packages. Lucide's official
site links to branded merchandise; that is not an alternative paid icon
software product. This is a manual, source-surface finding, not an assurance
that no consulting, support, sponsorship, or unadvertised commercial activity
exists.

Maintenance concentration is a recorded risk: each Blazicons repository shows
one listed contributor, and the `BlazorComponentUtilities` 1.8.0 release is
approximately five years old on its current repository page. This is not a
policy prohibition, but it weighs against treating the dependency as a
low-maintenance baseline.

### Vulnerability Evidence

The following scoped command was run on 2026-08-06:

```powershell
dotnet list C:\ws\BudgetRestart\HouseholdLedger\src\HouseholdLedger.Client\HouseholdLedger.Client.csproj package --include-transitive --vulnerable --format json
```

It returned JSON containing only `version: 1`, parameters
`--vulnerable --include-transitive`, the NuGet.org and SDK package sources, and
the Client project path. It returned no target-framework section, no package
list, and no vulnerability findings. Therefore it is **not** evidence that a
vulnerability scan passed. No alternative current, package-level vulnerability
result was retained for this review.

`Directory.Build.props` enables NuGet auditing for direct and transitive
packages, at the `high` threshold, and treats `NU1903` and `NU1904` as errors.
That configured control does not replace a successful, inspectable scan result
for this closure.

### Published Output

The existing Client and API `bin/Release/net10.0/publish` directories were
present, but direct read-only file filters for `Blazicons*.dll`,
`BlazorComponentUtilities.dll`, and `Blazicons.886t4n1lry.bundle.scp.css`
returned no files. Generated static-web-asset manifests do identify the
Blazicons CSS asset and generated records identify the three assemblies, but
they are not a retained final publish inventory. This review did not build or
publish because that would create artifacts outside the allowed documentation
scope.

Consequently, the actual shipped closure, trimming outcome, static assets,
attribution placement, and final-output vulnerability state are **Not
Verifiable**.

## Governance Requirement Matrix

| Policy requirement | Verdict | Evidence and limitation |
| --- | --- | --- |
| Exact package identity, version, publisher, and download origin | Met | Committed Client lock, cache metadata, nuspec author/repository/commit, and NuGet.org source are recorded above. |
| Complete locked `net10.0` closure | Met for the NuGet package graph | The Client lock identifies the three new packages and their edges. Existing Microsoft framework closure is not newly introduced by Lucide. Published closure remains separate. |
| Canonical licenses and required notices | Not Met | Source and bundled evidence establish MIT for the wrappers and utility; Lucide's ISC/MIT split is recorded. Neither Blazicons package contains a notice, no final-output notice mechanism is evidenced, and the exact Feather-derived status of used icons is not mapped. |
| Free/open-source and commercial-counterpart review | Met, with stated search limit | Reviewed official package/repository surfaces show FOSS source and no paid software counterpart or dual license. Lucide merchandise is recorded as non-equivalent. |
| Vulnerability evidence and scanner limits | Not Verifiable | The actual `dotnet list` JSON response was non-evaluative; no passing scan result is claimed. |
| Archive/package integrity | Met | Cache source/hash matches the lock and `dotnet nuget verify --all` completed without error for all three packages. |
| Why an existing dependency or built-in mechanism is insufficient | Met | Feature 003 documents a narrow, user-authorized icon-source exception for familiar pane controls while retaining native buttons and bespoke local Blazor/CSS primitives. |
| User decision and exception scope | Met | Feature 003 records user authorization for the official Lucide Blazor/.NET integration only as an icon source. The user separately and explicitly accepts Lucide regardless of license for this review. |
| Published-output and inventory review | Not Met | No current final-output inventory or retained verification of assets, assemblies, notices, or trimming is available. |
| Client-only direct-reference scope | Met | The only matching project `PackageReference` is in the Client project; central version and Client lock are present. |

## Required Closure Evidence

Before this dependency can receive full governance approval, an authorized
owner must retain all of the following for the exact locked versions:

1. A successful, inspectable vulnerability result that enumerates the Client
   target framework and closure, including scanner source, date, and limits.
2. A final Client and hosted publish inventory showing whether the three
   assemblies and Blazicons static assets ship after trimming.
3. A shipped-attribution mechanism that carries the applicable Kyle Herzog,
   Ed Charbeneau, Lucide, and any applicable Feather notices, followed by
   final-output verification. The user license decision remains recorded but
   does not remove this obligation.
4. An explicit mapping from the exact two Feature 003 Lucide icons to the
   current Lucide license's Feather-derived list, or a documented conservative
   attribution treatment that includes both the ISC Lucide and MIT Feather
   notices where applicable.

After those records exist, Research and Documentation must perform a narrow
re-audit of Feature 003 AC-06 and Definition of Done items 1, 5, and 6. No
product-code, project-manifest, lock-file, test, configuration, or artifact
change is recommended by this review itself.

## Sources and Commands

- [Dependency Governance](../development/dependency-governance.md), accessed
  2026-08-06.
- [Feature 003 specification](../features/003-workspace-navigation-and-ui-foundation.md),
  accessed 2026-08-06.
- [Feature 003 audit](2026-08-06-feature-003-workspace-navigation-and-ui-foundation-audit.md),
  accessed 2026-08-06.
- Client project, central package versions, and Client lock file, read locally
  on 2026-08-06.
- Local NuGet cache nuspecs, metadata, package hashes, bundled licenses, and
  repository signatures, read or verified on 2026-08-06.
- Current NuGet package pages and locked-commit source license files for
  Blazicons and Blazicons.Lucide, accessed 2026-08-06.
- [Lucide license](https://lucide.dev/license), accessed 2026-08-06.

## Documentation Validation

The Markdown diagnostics and scoped diff checks for this record are completed
after its creation and recorded in the completion report.
