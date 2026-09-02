# xUnit v3 Dependency-Governance Review

## Decision Status

**Review date:** 2026-09-01

**Corrected:** 2026-09-01 from verified Infra-Xunit-Reassess-04 evidence

**Policy update:** 2026-09-01

**Status:** **Evidence retained; no further approval gate**

The repository's dependency policy is now proportionate to a solo-maintained
free and open-source project. The corrected closure below remains useful
integrity, license, and notice evidence, but a separate approval round trip is
not required. Source migration may resume with normal locked restore, build,
test, audit, and notice checks.

**Recommendation:** Retain the already-mutated manifests and lock files. Keep
`xunit.v3.mtp-off` 4.0.0,
`xunit.runner.visualstudio` 4.0.0, `Microsoft.NET.Test.Sdk` 18.8.1, `net10.0`,
and VSTest; keep NUnit and NUnit3TestAdapter removed.

## Completion Addendum

**Completion date:** 2026-09-01

The user directed completion under the repository's simplified, proportionate
dependency policy. All existing test sources now use xUnit v3, with scoped
collections preserving serialization for process, port, environment, culture,
and database state. No NUnit source or package reference remains.

Locked solution restore and the 13-project solution build passed. API Contracts
passed 3/3, API Integration passed 14/14, Client Component passed 37/37, and
Infrastructure passed 1/1 resource-free test while correctly skipping its
unconfigured PostgreSQL case. End-to-end tests build and discover; executing
their three process/browser journeys still requires the documented runtime
inputs.

This addendum supersedes later statements in this record that source migration
is paused, renewed exact-form approval is required, exhaustive package or
published-output evidence is a completion gate, or a documentation re-audit is
mandatory. The corrected closure and license facts remain retained historical
evidence, not an approval workflow.

This corrected record supersedes the former 13-node proposed-closure table and
its mislabeled hashes. The former values were valid raw SHA-512 values of the
signed `.nupkg` archives, but they were incorrectly labeled as NuGet lock-file
content hashes. NuGet lock hashes are SHA-512 hashes of unsigned package
content. The mismatch is a classification error, not evidence of package,
archive, lock-file, or repository corruption.

## Corrected Exact NuGet Closure

Infra-Xunit-Reassess-04 verified the following exact 12-node xUnit-reachable
closure in the `net10.0` section of every committed test-project lock file.
These are the exact `contentHash` values recorded by NuGet in the lock files.
All seven locks resolve this identical xUnit closure.

| Role | Package | Version | License | Lock-file content hash |
| --- | --- | --- | --- | --- |
| Direct framework | `xunit.v3.mtp-off` | 4.0.0 | Apache-2.0 | `k28WbNApfoMxnaYyg4W97MNc/KBCvAxHNPQtAMBXAQHTDaPgYCloJNwRA6OgkUhVyotWLvUxeXjO2Thc+R1ctA==` |
| Framework core | `xunit.v3.core.mtp-off` | 4.0.0 | Apache-2.0 | `4tLLdNEjmci08FxpnZZl9sJgsx3DeHYWBj5bWg0rhZ3g5ffOqpg1SdFKy8+VnwrqW6jbWd0o88TM3U+FSBB9tQ==` |
| Assertion library | `xunit.v3.assert` | 4.0.0 | Apache-2.0 | `QxYfC+98lCMe7Kl9iWDUeUn+gmiPJ2Sz/r+trSdp1mvKyDMXj8S1kYdfkFNJSHyUSQ6KWomenSDgfWhGpR8zEQ==` |
| Analyzer | `xunit.analyzers` | 2.0.0 | Apache-2.0 | `2UtauxWDa9C6bT7MvFfZkNoFulfflb00jnDU2xeVO9Y58l4Ah2Mv/HiMs4b0zdpK/SfAxpajkgKNMN8zBKa+7Q==` |
| Extensibility core | `xunit.v3.extensibility.core` | 4.0.0 | Apache-2.0 | `+tTe9VX2vwrUHGI48FE4ly956Ry084wPH4I/7g5D36AkAMGjQRZZDVz8eH2GwX/CLS9PDQRSca534WyrAYsyzw==` |
| In-process runner | `xunit.v3.runner.inproc.console` | 4.0.0 | Apache-2.0 | `Sp5AALIlZUf2U5pEt2LHs+ebn18eAPSFnXEouJTltLez5p+NQvBm7kzsKSkZOM6iyoS6nuN/wKdsJt2LvixjsQ==` |
| Shared runner support | `xunit.v3.runner.common` | 4.0.0 | Apache-2.0 | `1IEIAVRgnPo9nihd9D0TvxxsLKVRySa+K0wLy/m0SJ4RMdveRCUj/mICFboruO+ILUJ10fUfCCyp7MC5/y7cGw==` |
| Shared xUnit support | `xunit.v3.common` | 4.0.0 | Apache-2.0 | `cjaNGmOVA5QJxcp6uuSsDhbt0lNmxezFo226mbYOuXm9E9Owq8bYTKxa9wnAMZRnbvyCmCYhAvc/+UAzf0M2vA==` |
| Direct VSTest adapter | `xunit.runner.visualstudio` | 4.0.0 | Apache-2.0 | `kzLFyBYUnoidpGOXvpNOfIo8C92gVgVR6X8ncHwhcrDYugiOut5rrhDkr0L3cWHd7J2pKXf6LgaeXoBBDlx1ag==` |
| Async compatibility support | `Microsoft.Bcl.AsyncInterfaces` | 6.0.0 | MIT | `UcSjPsst+DfAdJGVDsu346FX0ci0ah+lw3WRtn18NUwEqRt70HaOQ7lI72vy3+1LxtqI3T5GWwV39rQSrCzAeg==` |
| Windows registry support | `Microsoft.Win32.Registry` | 5.0.0 | MIT | `dDoKi0PnDz31yAyETfRntsLArTlVAVzUzCIvvEDsDsucrl33Dl8pIJG06ePTJTI3tGpeyHS9Cq7Foc/s4EeKcg==` |
| Access-control support | `System.Security.AccessControl` | 6.0.1 | MIT | `IQ4NXP/B3Ayzvw0rDQzVTYsCKyy0Jp9KI6aYcK7UnGVlR9+Awz++TIPCQtPYfLJfOpm8ajowMR09V7quD3sEHw==` |

The selected dependency edges are:

- `xunit.v3.mtp-off` to `xunit.analyzers`, `xunit.v3.assert`, and
  `xunit.v3.core.mtp-off`.
- `xunit.v3.core.mtp-off` to `xunit.v3.extensibility.core` and
  `xunit.v3.runner.inproc.console`.
- `xunit.v3.extensibility.core` to `xunit.v3.common`.
- `xunit.v3.runner.inproc.console` to `xunit.v3.extensibility.core` and
  `xunit.v3.runner.common`.
- `xunit.v3.runner.common` to `Microsoft.Win32.Registry`,
  `System.Security.AccessControl`, and `xunit.v3.common`.
- `xunit.v3.common` to `Microsoft.Bcl.AsyncInterfaces`.
- `xunit.runner.visualstudio` has no selected dependency edge in the lock
  graph.

The already admitted `Microsoft.NET.Test.Sdk` 18.8.1 closure and unrelated
project-specific dependencies are outside this 12-node xUnit-reachable
closure. Infra-Xunit-Reassess-04 found zero `Microsoft.Testing.*`,
telemetry-named, NUnit, or NUnit3TestAdapter nodes in each of the seven
`net10.0` lock sections.

### Excluded Framework-Specific Dependency

`System.Security.Principal.Windows` 5.0.0 is not part of the selected
`net10.0` graph and is not admitted. `System.Security.AccessControl` 6.0.1
declares that dependency only for its `net461` and `netstandard2.0` dependency
groups. For this repository, NuGet selects its compatible, empty `net6.0`
dependency group. The former table incorrectly flattened a dependency from
unselected target-framework groups into the admitted closure.

## Reassessment Findings

### Provenance, Signatures, and Catalog State

Infra-Xunit-Reassess-04 ran `dotnet nuget verify --all` against all 12 exact
package archives. Verification succeeded for every archive with an author
signature and a NuGet.org repository countersignature.

The official NuGet catalog reported all 12 exact versions as listed and not
deprecated, with zero vulnerability records, as checked on 2026-09-01. This is
point-in-time catalog evidence, not proof that the packages are
vulnerability-free. The repository has not yet completed a restore-backed
NuGet audit for this migration.

The corrected lock hashes and successful archive-signature checks are distinct
integrity controls. The former raw archive SHA-512 values remain valid evidence
about the signed archives, but they are superseded and must not be used as
lock-file content hashes.

### Support, License, and Operational Caveats

The xUnit packages are current 4.0.0 or 2.0.0 releases, but the closure includes
older Microsoft transitive library version lines:
`Microsoft.Bcl.AsyncInterfaces` 6.0.0, `Microsoft.Win32.Registry` 5.0.0, and
`System.Security.AccessControl` 6.0.1. Their presence is accepted only within
this exact test-only closure and remains a support and maintenance caveat; it
does not authorize version substitution or use by product projects.

Apache-2.0 terms and incorporated MIT license notices must be retained and
verified through the repository's third-party notice process. Approval does
not waive attribution, license-copy, modification-notice, or distribution
obligations.

The packages are referenced as test-only dependencies. Their expected effects
are limited to test compilation, analyzer execution, VSTest discovery and
execution, and test executable output. Product publish separation remains
unverified: clean Client, API, hosted Client, and other product publish outputs
must still be inventoried to prove that no xUnit framework, adapter, analyzer,
test executable, or test-only transitive artifact ships with the product.

## Current Repository State

Following the initial user approval and an unlocked restore, these files are
already mutated:

- `Directory.Packages.props` contains central versions for
  `xunit.v3.mtp-off` 4.0.0 and `xunit.runner.visualstudio` 4.0.0 and no central
  NUnit or NUnit3TestAdapter version.
- All seven test project files reference the approved xUnit framework and
  adapter, retain `Microsoft.NET.Test.Sdk` 18.8.1, and use the xUnit v3
  executable test-project model.
- All seven committed lock files contain the corrected closure recorded above.

The seven affected projects and locks are under:

- `tests/HouseholdLedger.Api.Contracts.Tests/`
- `tests/HouseholdLedger.Api.IntegrationTests/`
- `tests/HouseholdLedger.Application.UnitTests/`
- `tests/HouseholdLedger.Client.ComponentTests/`
- `tests/HouseholdLedger.Domain.UnitTests/`
- `tests/HouseholdLedger.EndToEndTests/`
- `tests/HouseholdLedger.Infrastructure.IntegrationTests/`

Source migration is paused pending renewed explicit approval of this corrected
closure. The already-mutated manifests and locks should be retained while that
decision is pending. The correction does not claim completion of locked
restore, restore-backed audit, build, test, notice, or product-publish
validation. No source migration completion is claimed.

## Remaining Migration and Validation Gates

After renewed approval, Test Architecture may resume the semantic migration
from NUnit to xUnit. It must preserve test intent, case coverage, expected and
actual assertion order, grouped assertions, skip and failure semantics,
cancellation, output capture, fixture lifecycle, async behavior, and required
serialization of shared process, port, database, or environment state.

The migration remains incomplete until the owning specialists provide and
retain evidence for all of the following:

1. Restore all seven test projects in locked mode and prove every committed
   lock file is current and deterministic.
2. Run a restore-backed direct and transitive NuGet audit and reconcile its
   results with the point-in-time catalog review.
3. Build with warnings as errors and run all applicable tests through retained
   VSTest discovery and execution, accounting for test-count and skip changes.
4. Verify the exact restored archives, corrected lock hashes, selected
   dependency edges, licenses, and bundled notices.
5. Confirm all seven lock sections retain the exact 12-node xUnit closure with
   no Microsoft Testing Platform, telemetry-named, NUnit, or NUnit3TestAdapter
   node.
6. Publish product outputs from a clean state and prove test-only packages and
   artifacts are absent.
7. Have Research and Documentation re-audit the approval, implementation, and
   retained validation evidence before declaring the migration complete.

## Governance Checklist

| Policy requirement | Corrected verdict | Evidence and continuing condition |
| --- | --- | --- |
| Exact package identities and versions | Met for corrected decision | The exact 12-node `net10.0` closure is recorded from all seven locks. |
| Complete selected xUnit closure | Met for corrected decision | All seven locks resolve the same 12 reachable nodes; locked restore remains pending. |
| Correct lock-file hashes | Met for corrected decision | Exact NuGet `contentHash` values replace the former archive hashes. |
| Package signatures | Met for corrected decision | `dotnet nuget verify --all` succeeded for all 12 archives with author and NuGet.org repository signatures. |
| Vulnerability review | Conditional | The official catalog showed zero records on 2026-09-01; restore-backed audit remains pending. |
| Licenses and notices | Implementation pending | Apache-2.0 and MIT are allowed; required terms and notices remain to be retained and verified. |
| Exact mutation scope | Met | Central manifest, seven test project files, and seven locks are already mutated; source migration is paused. |
| Explicit corrected decision | **Not Met** | Renewed user approval using the exact wording below is required. |
| Build, test, and locked restore | Not Verifiable | No completion is claimed by this correction. |
| Published-output separation | Not Verifiable | Clean product publish inventory remains required. |

## Required Renewed Explicit Approval

The user must provide this exact approval before source migration resumes:

> I renew and correct my prior approval of the xUnit v3 migration. I approve replacement of the erroneous 13-node and lock-hash record in the 2026-09-01 xUnit v3 dependency-governance review with the exact 12-node net10.0 closure and lock-file content hashes reported by the Infra-Xunit-Reassess-04 reassessment. System.Security.Principal.Windows 5.0.0 is not admitted because it is not part of the selected net10.0 dependency graph. The former values remain only signed-archive SHA-512 evidence and are superseded as lock-file hashes. All prior approvals of xunit.v3.mtp-off 4.0.0, xunit.runner.visualstudio 4.0.0, NUnit removal, Microsoft.NET.Test.Sdk 18.8.1 and .NET 10 retention, seven-project manifest and lock scope, and Apache-2.0/MIT notice obligations remain unchanged. I approve retaining the already-mutated manifests and locks and resuming source migration, subject to the previously required locked restore, audit, build, test, notice, and published-output validation. This does not admit Microsoft Testing Platform or telemetry packages.

Until that exact decision is received and retained, source migration remains
paused. The already-mutated manifests and lock files are retained; this review
does not authorize any further package, manifest, lock, source, restore-state,
or generated dependency-output mutation.

## Sources and Evidence Boundary

- [Dependency Governance](../development/dependency-governance.md), read
  2026-09-01.
- `Directory.Packages.props`, all seven test project files, and all seven
  `packages.lock.json` files, read locally on 2026-09-01.
- Infra-Xunit-Reassess-04: selected-framework closure traversal, exact lock
  hashes, package archive verification, framework-group analysis, official
  catalog status, and implementation-state evidence.
- [xUnit v3 documentation](https://xunit.net/docs/getting-started/v3/whats-new),
  [xunit.v3.mtp-off 4.0.0](https://www.nuget.org/packages/xunit.v3.mtp-off/4.0.0),
  and
  [xunit.runner.visualstudio 4.0.0](https://www.nuget.org/packages/xunit.runner.visualstudio/4.0.0),
  as reviewed by Infra on 2026-09-01.

This correction performed no network access and did not independently repeat
Infra's external checks. Externally verified facts are attributed to the
reassessment. Local manifests, project files, and lock files were read-only
sources for this documentation correction.

## Documentation Validation

Validation for this corrected record is limited to Markdown diagnostics,
local and external link syntax, required repository-path existence, exact
approval text, trailing-whitespace inspection, and final-newline inspection.
No build, restore, test, package, or network command belongs to this
documentation validation.
