# Dependency Governance

HouseholdLedger is a solo-maintained free and open-source project. Dependency
decisions should be careful enough to protect users and preserve license
compliance without turning routine maintenance into an approval process.

## Default Policy

Prefer the .NET shared frameworks, base class libraries, built-in dependency
injection, `HttpClient`, `System.Text.Json`, direct application interfaces, and
explicit test data. AutoFixture, AutoMapper, MediatR, Testcontainers, and Docker
Desktop are outside Feature 001's approved design.

Package versions are centralized in `Directory.Packages.props`. Projects commit
their lock files, restore only from NuGet.org, and restore in locked mode.
`Directory.Build.props` enables direct and transitive NuGet auditing; high and
critical findings are build errors.

Prefer dependencies with clear provenance, active maintenance, and licenses
compatible with this repository. A project offering sponsorship, support,
hosting, or paid products does not disqualify its open-source packages. Avoid a
dependency only when its actual license, terms, cost, security posture, or
operational impact is unsuitable for HouseholdLedger.

## Routine Changes

Routine changes to reputable FOSS packages, test tools, analyzers, and SDK
components do not need advance approval or a standalone review document. Check
the package identity, version, source, license, audit output, expected lock-file
delta, and focused build or test result as part of the change.

Record rationale only when the choice or tradeoff would surprise a future
maintainer. Archive hashes, signature verification, exhaustive transitive
inventories, commercial-model surveys, and published-output inventories are
not normal package-change requirements.

Use deeper review before proceeding when provenance or licensing is unclear;
the dependency handles secrets or sensitive data; it introduces executable
native code, a hosted/paid service, telemetry, or a privileged runtime; it has
significant unresolved vulnerabilities; or it materially changes deployment
or operating cost. Ask the maintainer when one of those concerns requires a
product, privacy, legal, security, or spending decision.

## Change Procedure

1. Update the central version and owning project reference, then regenerate
   only affected lock files and inspect the diff.
2. Run locked restore, build, and the narrowest relevant tests. Review NuGet
   audit output and required notices.
3. Inspect published output only when a dependency may change shipped assets,
   then report meaningful risks or unavailable validation.

Stop and ask before accepting a material legal, security, privacy, or cost risk.
Otherwise, use maintainer judgment and continue without a separate approval
round trip.

Historical dependency investigations remain in `docs/audit` when their facts
are useful. They do not create approval gates for later routine maintenance.
