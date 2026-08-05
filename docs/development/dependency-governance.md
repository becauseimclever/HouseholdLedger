# Dependency Governance

Dependencies are admitted deliberately because a direct package declaration
does not describe every transitive library, runtime, downloaded artifact,
license obligation, vulnerability, or commercial offering.

## Default Policy

Prefer the .NET shared frameworks, base class libraries, built-in dependency
injection, `HttpClient`, `System.Text.Json`, direct application interfaces, and
explicit test data. AutoFixture, AutoMapper, MediatR, Testcontainers, and Docker
Desktop are outside Feature 001's approved design.

Package versions are centralized in `Directory.Packages.props`. Projects commit
their lock files, restore only from NuGet.org, and restore in locked mode.
`Directory.Build.props` enables direct and transitive NuGet auditing; high and
critical findings are build errors.

Only the SPDX identifiers explicitly listed in the approved
[Feature 001 specification](../features/001-application-scaffolding.md) are
allowed without a user decision. Similar names and deprecated aliases are not
implicit approvals. The narrow PostgreSQL exception and one-time Wave 3
prerelease exception apply only to the dependencies named in that document.

The policy also excludes a dependency when its project offers paid product
tiers, features, editions, proprietary or commercial counterparts, hosted
commercial counterparts, or dual commercial licensing. An allowed license does
not override this commercial-model rule. By explicit user decision on
2026-08-03, optional vendor-provided paid support does not disqualify otherwise
identical free/open-source software. Donations, sponsorship, third-party
hosting, consulting, and optional support are recorded but do not by themselves
make the software a paid product counterpart.

## Review Checklist

Before adding or updating any package, tool, runtime, image, browser, or
downloaded asset, record:

1. The exact name, version, source repository, publisher, and download origin.
2. Every direct, transitive, build, runtime, platform, and downloaded artifact.
3. Canonical license expressions and required notices for the complete closure.
4. Whether each component is free and open source.
5. Paid services, proprietary editions, dual licensing, commercial tiers, and
   other commercial counterparts offered by the project or publisher.
6. Known vulnerabilities and the limits of the scanner used.
7. Archive or package integrity controls, including hashes and signatures.
8. Why built-in functionality or an already approved dependency is
   insufficient.
9. The exact scope of any required exception and the user's recorded decision.

Automated license and vulnerability output is triage evidence, not a final
review. Inspect generated lock files and published output before concluding
that the closure is complete.

## Change Procedure

1. Complete and approve the review before editing package manifests.
2. Add the version centrally and the package reference only to its owning
   project.
3. Regenerate affected lock files deliberately and inspect the diff.
4. Restore in locked mode, build with warnings as errors, and run the narrowest
   relevant tests.
5. Re-run inventory, vulnerability, license, and published-output checks.
6. Record validation and any continuing limitation in the owning feature.

Do not treat a successful restore, an MIT NuGet declaration, or file presence
as complete dependency evidence.

## Current Direct-Package Review

This manual snapshot was performed on 2026-08-02 against the resolved direct
package inventory, exact NuGet pages, tagged licenses where available, and
official project or product pages. It is a human review of those sources, not a
conclusion produced by restore, lock files, NuGet audit, or license automation.
It does not replace a complete transitive, runtime, image, or published-output
inventory.

| Direct family and resolved versions | Primary-source finding | Manual commercial/no-paid-option finding | Status |
| --- | --- | --- | --- |
| Microsoft ASP.NET Core: `Microsoft.AspNetCore.Components.WebAssembly`, `Microsoft.AspNetCore.OpenApi`, and `Microsoft.AspNetCore.Mvc.Testing` 10.0.10 | NuGet identifies Microsoft/dotnet sources and MIT; the tagged ASP.NET Core license is MIT. | The [.NET free-use page](https://dotnet.microsoft.com/en-us/platform/free) says .NET is open source with no licensing cost, including commercial use. Optional paid Microsoft support does not disqualify the otherwise identical FOSS. | Direct-family license, provenance, and commercial review supported. |
| Microsoft EF Core: `Microsoft.EntityFrameworkCore`, `.Design`, and `.Relational` 10.0.10 | NuGet identifies Microsoft/dotnet sources and MIT; all direct EF packages are patch-aligned. | Same official .NET statement and non-disqualifying optional paid-support finding as the ASP.NET Core family. | Direct-family license, provenance, and commercial review supported. |
| OpenAPI.NET: `Microsoft.OpenApi` 2.11.0 | [NuGet](https://www.nuget.org/packages/Microsoft.OpenApi/2.11.0) identifies Microsoft/OpenAPI.NET provenance and MIT. | The reviewed official package and repository surfaces did not advertise a paid edition, hosted counterpart, or dual commercial license. | No direct-family concern found. |
| VSTest: `Microsoft.NET.Test.Sdk` 18.8.1 | [NuGet](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1) identifies Microsoft/vstest provenance and MIT; the tagged repository license is MIT. | The package is part of the .NET toolchain; optional paid support is non-disqualifying, and no package-specific paid tier was found. | Direct-family license, provenance, and commercial review supported. |
| Npgsql EF provider 10.0.3 and resolved Npgsql driver | [NuGet](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3) and the [Npgsql project](https://www.npgsql.org/) identify the project as free, open source, and PostgreSQL-licensed. | The reviewed official project and repository surfaces did not advertise a paid edition, hosted counterpart, or dual commercial license. Donated licenses are acknowledgements, not a project product tier. | Within the exact PostgreSQL-license exception at the direct-family level; real server/image and full closure review remain open. |
| NUnit 4.6.1 and NUnit3TestAdapter 4.6.0 | Exact NuGet pages declare MIT, identify NUnit repositories, and describe both as open source. | The [NUnit project page](https://nunit.org/) describes volunteer and .NET Foundation support; no project-offered paid edition, hosted counterpart, or dual commercial license was found. | No direct-family concern found. |
| `xunit.runner.visualstudio` 3.1.5 | [NuGet](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5) identifies xUnit provenance and Apache-2.0. | The [xUnit project page](https://xunit.net/) calls xUnit free and open source and requests sponsorship. Links to commercial third-party IDE/test tools are compatibility links, not xUnit product tiers. | No direct-family concern found. |
| `bunit` 2.8.6 | [NuGet](https://www.nuget.org/packages/bunit/2.8.6) and the tagged repository identify bUnit provenance and MIT. | Official surfaces request sponsorship and display a commercial sponsor. Sponsorship alone is not a bUnit commercial tier under the approved rule; no bUnit-offered paid edition, hosted counterpart, or dual commercial license was found. | Direct family passes; exact prerelease exception applies and closure remains separately reviewed. |
| `StyleCop.Analyzers` 1.2.0-beta.556 | [NuGet](https://www.nuget.org/packages/StyleCop.Analyzers/1.2.0-beta.556) and the tagged repository identify DotNetAnalyzers provenance and MIT. | The reviewed official package and repository surfaces did not advertise a paid edition, hosted counterpart, or dual commercial license. | Direct family passes under the approved StyleCop prerelease exception. |

The bUnit closure resolves `AngleSharp.Css` 1.0.0-beta.224. Its
[NuGet page](https://www.nuget.org/packages/AngleSharp.Css/1.0.0-beta.224) and
official repository identify MIT and .NET Foundation support. GitHub Sponsors,
PayPal, and Buy Me a Coffee are donation channels, not a paid product tier. No
project-offered paid edition, hosted counterpart, or dual commercial license
was found on the reviewed official surfaces. The one-time Wave 3 exception
covers its prerelease status only; the remaining AngleSharp/bUnit transitive
closure still requires complete notice and published-output review.

### Unresolved Package Concerns

- The manual direct-family review does not prove that every transitive package,
   WebAssembly runtime asset, Npgsql dependency, PostgreSQL image layer, or
   published file has passed the same review.
- Isolated Docker 29.6.2 validation pulled and ran
   `docker.io/library/postgres:18` at
   `postgres@sha256:a9abf4275f9e99bff8e6aed712b3b7dfec9cac1341bba01c1ffdfce9ff9fc34a`.
   PostgreSQL 18.3 Debian passed the focused real test, full Infrastructure
   tests, and API composition tests before deterministic cleanup. Complete
   image-layer, bundled-runtime notice, commercial-model, and vulnerability
   closure remains unrecorded; do not describe the PostgreSQL closure review as
   complete.
- The exact Firefox 153.0.1 EME-free and geckodriver 0.37.1 test-runtime chain
   passed provenance, license, version, vulnerability, and artifact checks.
   Tests enforce both executable SHA-256 values before launch, and two fresh
   hardened E2E runs passed 3/3. See the
   [Browser E2E Dependency Review](browser-e2e-dependency-review.md).
- The resolved NuGet closure can be enumerated from lock files and
   `dotnet list package --include-transitive`, but complete manual notices and
   published-runtime review are not yet evidenced. Do not describe the overall
   supply-chain review as complete.
