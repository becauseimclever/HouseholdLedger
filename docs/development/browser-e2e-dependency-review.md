# Browser E2E Dependency Review

## Current Verdict

**Status: Exact test runtime provisioned and verified; package-free direct-W3C
browser evidence passed on 2026-08-03.**

The Firefox/geckodriver chain is approved for real desktop and mobile viewport
evidence against the independently hosted standalone WebAssembly client and
API. On 2026-08-03, the user decided that optional vendor-provided paid support
does not disqualify otherwise identical free/open-source software; paid product
tiers, features, and editions still disqualify dependencies. The user also
approved the exact runtime exception below, limited to manually provisioned
Firefox and geckodriver test artifacts and their verified licenses. The exact
artifacts passed the required post-provision checks before two complete browser
runs. This is not a general package-license exception.

`Selenium.WebDriver` 4.46.0 is excluded from the approved design. Its NuGet
package has no transitive NuGet
packages, but it embeds three Selenium Manager executables. Selenium Manager is
a Rust program with a large Cargo closure, can download browsers and drivers,
and sends opt-out telemetry when invoked. Avoiding execution does not remove the
native artifacts from the restored package. A complete crate-by-crate license
classification was not established without downloading the package, so it does
not yet satisfy the complete-artifact gate.

The approved automation design is a small test-owned W3C WebDriver local end
using the shared framework's `HttpClient` and `System.Text.Json`, talking
directly to manually provisioned geckodriver. It adds no NuGet package and is
viable for Feature 001's narrow journeys, subject to specialist implementation
and test review. This review is dependency research, not legal advice.

## Candidate Inventory

| Layer | Exact candidate | Dependency and license finding | Commercial finding | Status |
| --- | --- | --- | --- | --- |
| .NET binding | `Selenium.WebDriver` 4.46.0 | Apache-2.0 direct NuGet package; no transitive NuGet packages; embeds Manager binaries for Windows, Linux, and macOS | Selenium is a Software Freedom Conservancy project. BrowserStack, Sauce Labs, and TestMu are third-party sponsors/services, not Selenium editions. No Selenium-operated paid automation tier was found. | Blocked pending embedded-manager closure review |
| Embedded manager | Selenium Manager 0.4.46 | Apache-2.0 Rust CLI; tagged 3,117-line `Cargo.lock` records the build graph and crates.io checksums, but every crate license was not classified | No separate paid Manager tier found | Exclude by avoiding the package; not invoking it is insufficient inventory control |
| Protocol alternative | Test-owned W3C WebDriver HTTP local end | No package; uses already approved shared-framework APIs | No product or service dependency | Implemented and passed two complete 3/3 runs |
| Driver | geckodriver 0.37.1, Windows x64 ZIP | MPL-2.0 executable; tagged source and Rust lock file; official SHA-256 | No paid geckodriver edition or service found | Exact artifact, executable version, hash, and license verified |
| Browser | Firefox 153.0.1, Windows x64, en-US, EME-free | Principally MPL-2.0; installed `about:license` records the artifact's additional FOSS notices | Firefox is FOSS; optional Mozilla professional support is non-disqualifying under the 2026-08-03 decision | Exact artifact, version, hash, signatures, installed notices, and inventory verified |
| Profile | Fresh geckodriver-created profile per test | Runtime test data; no dependency | None | Required isolation control |
| Application | Separately hosted Client and API | Existing product artifacts | None added | Must prove standalone loading and API integration |

The Selenium NuGet closure is exactly `Selenium.WebDriver` 4.46.0.
`Selenium.Support`, Selenium Server/Grid, driver-manager packages, browser
packages, and cloud services are unnecessary and receive no approval. The raw
protocol alternative has no new NuGet closure but retains the same browser and
driver policy gates.

## Provenance and Integrity

NuGet published `Selenium.WebDriver` 4.46.0 on 2026-07-11 from SeleniumHQ under
Apache-2.0. Restore would remain NuGet.org-only, locked, and subject to the
repository's signature and package-hash controls. Before any future approval,
inspect the actual NUPKG, generated lock file, restored native files, published
output, NuGet audit result, and embedded Manager closure. This review did not
download the package and does not claim those checks passed.

The tagged Selenium build pins its Manager inputs. The Windows Manager SHA-256
is `c8ed9ebfa23faf029fd5060a121b99a09e98a29a83212d8d0d644e62e07a47d0`.
This supports provenance but does not classify the binary's crate licenses.

Provision only this approved driver:

- Artifact: `geckodriver-v0.37.1-win64.zip`.
- SHA-256:
  `dfed9315abe8d2fbc1b6161a2ee8002452e79cf05ee92fdc653a4e26bc35edd8`.
- Source: tag `v0.37.1`, release revision `300705c65d1b`.
- License: `MPL-2.0`.

The GitHub release exposes the digest. Unlike its Linux archives, the expanded
asset list exposes no detached `.asc` asset for the Windows ZIP. After an
authorized manual download, verify the digest, executable version, and Windows
Authenticode status. Do not use an unversioned package-manager feed.

Provision only this approved browser:

- Artifact: `win64-EME-free/en-US/Firefox Setup 153.0.1.exe`.
- SHA-256:
  `d3a94dcfb7609e25ae6a777d019d5df1ee97030b273e7f15da373293ee835934`.
- Release date: 2026-07-28.
- Signature: adjacent `.exe.asc` plus signed `SHA256SUMS` and `SHA512SUMS`.
- Mozilla release-key fingerprint:
  `14F26682D0916CDD81E37B6D61B7B526D98F0353`.

The detached signatures and checksum were verified with the recorded Mozilla
release key before installation. The installed runtime is under the test-owned
versioned user-local path
`%LOCALAPPDATA%\HouseholdLedger\BrowserTestRuntime\firefox\153.0.1-eme-free`.
The tests disable updates and fail unless WebDriver reports Firefox 153.0.1.

## Downloads and Telemetry

Selenium Manager is unsafe at this policy boundary. Official documentation says
bindings invoke it as a fallback when a driver is not supplied. It can discover,
download, extract, cache, update, and prune drivers and browsers under
`~/.cache/selenium`. It reports Selenium version, binding, OS/architecture,
browser/version, and rough IP-derived geolocation to Plausible once per day.
Collection is on by default; `SE_AVOID_STATS=true` opts out.

Manual provisioning is safer. If Selenium is later approved, create the
`FirefoxDriverService` from the exact geckodriver directory and set
`FirefoxOptions.BinaryLocation` to the exact Firefox executable. Do not rely on
`PATH`. Set `SE_OFFLINE=true`, `SE_AVOID_BROWSER_DOWNLOAD=true`, and
`SE_AVOID_STATS=true` as defense in depth, and fail if a Selenium cache is
created. These variables do not compensate for an omitted driver path.

The raw-protocol alternative never invokes Manager. Firefox itself can request
updates, telemetry, Safe Browsing, remote settings, add-ons, and content. Use
test-specific policies/preferences to disable background update and telemetry
traffic, prevent extension installation and prompts, and allow only loopback
Client/API origins. EME-free avoids encrypted-media modules; it does not disable
all background networking.

## Vulnerability Status

On 2026-08-02, Selenium and geckodriver published no GitHub security advisories
for these versions. Absence of a published advisory is not proof of safety.
Their tagged Rust locks require an authorized advisory scan before adoption;
NuGet audit must also cover Selenium if used.

Firefox 153 fixed the high-, moderate-, and low-impact vulnerabilities in MFSA
2026-68. Firefox 153.0.1 is the current stable maintenance release. No later
desktop advisory was listed on 2026-08-02. Review Mozilla advisories before
every run and suspend a pinned browser with an unmitigated high or critical
issue.

## Approved Exact Runtime Exception

The user's 2026-08-03 narrow exception names exactly:

1. geckodriver 0.37.1 Windows x64 under `MPL-2.0`, including its compiled Rust
  closure represented by the tagged source and lock file.
2. Firefox 153.0.1 Windows x64 en-US EME-free under `MPL-2.0`, including only
  the LGPL-2.1-only, LGPL-3.0-only, Apache-2.0, MIT, BSD-family, ISC-style,
  Unicode, ICU, Boost, zlib, and other FOSS notices actually listed by that
  artifact's `about:license` page.
3. Windows system components Firefox uses under the operating-system exception;
  do not copy or redistribute them with HouseholdLedger.

The exception is runtime-only and test-only. It does not allow these licenses
generally in manifests or approve Selenium Manager, cloud grids, other browsers,
versions, locales, platforms, proprietary codecs, extensions, or downloaded
blocklists. Optional support is governed by the commercial-model interpretation,
not licensed by this exception. Every update requires renewed hashes,
advisories, notices, and commercial review.

The installed Firefox `about:license` output and file inventory were captured
under `%LOCALAPPDATA%\HouseholdLedger\BrowserTestRuntime\evidence`. This closes
the artifact-specific notice check without treating the source notice template
as proof of installed contents.

## Approved Execution Contract

Use normalized absolute paths supplied only through:

- `HOUSEHOLDLEDGER_FIREFOX_BINARY`: exact Firefox 153.0.1 EME-free
  `firefox.exe`.
- `HOUSEHOLDLEDGER_GECKODRIVER`: exact geckodriver 0.37.1
  `geckodriver.exe`.

Validate files, versions, and hashes before launch. Bind geckodriver, Client,
and API only to loopback on dynamically reserved ports. Create unique temporary
roots and Firefox profiles per test. Start only owned processes and always
remove processes, profiles, screenshots, logs, ports, and environment changes.
Parallel tests must not share a profile, port, cache, or output directory.

The test-enforced executable SHA-256 values are:

- Firefox `firefox.exe`:
  `79f01d224fe7f31795f2d4edcb31f497c96e11e9d0770704ed8495861f70d1c1`.
- geckodriver `geckodriver.exe`:
  `e95b4eac7960ffcd5acbfd92bb7d49d48f99c1d01a20ddd297fef8c80821020d`.

Run the same critical journey at desktop and mobile-sized visual viewports,
with dimensions owned by Test Architecture. Set and verify `window.innerWidth`
and `window.innerHeight`; outer window dimensions are insufficient. A
mobile-sized Firefox viewport is responsive evidence, not Firefox Android or
device emulation.

For each viewport, verify the standalone Client loads, WebAssembly starts, a
user interaction succeeds, the Client reaches the separate API, visible state
reflects the API result, and no uncaught page or network failure occurred.
Capture screenshots and reported viewport dimensions.

## Alternative Without Selenium

Implement only the W3C commands Feature 001 needs with `HttpClient` and
`System.Text.Json`: status, new/delete session, navigation, set/get window rect,
find element, click/send keys, execute script, and screenshot. Mozilla documents
direct HTTP operation because geckodriver is a complete WebDriver remote end.

This avoids `Selenium.WebDriver`, all embedded Manager binaries, runtime
downloads, and Manager telemetry. It does not avoid the Firefox/geckodriver
exception or its conditional post-provision verification. Keep the client
internal to `HouseholdLedger.EndToEndTests`. Test Architecture owns protocol
design and E2E assertions; production owners retain Client/API defects;
Research and Documentation audits evidence without changing implementation.

## Completed Execution Evidence

The user resolved both policy questions on 2026-08-03: optional paid support is
not disqualifying for otherwise identical FOSS, and the exact runtime-only
Firefox/geckodriver exception is approved. Paid product tiers, features, and
editions remain disqualifying.

The runtime is pinned below the user's local application-data directory. The
tests require exact normalized absolute paths through
`HOUSEHOLDLEDGER_API_ARTIFACT`, `HOUSEHOLDLEDGER_CLIENT_PUBLISH_DIR`,
`HOUSEHOLDLEDGER_FIREFOX_BINARY`, and `HOUSEHOLDLEDGER_GECKODRIVER`. No fallback
discovery or download occurs.

Two consecutive fresh-publish runs passed all three E2E cases on 2026-08-03 in
8.7 seconds and 8.1 seconds. One case is an API process smoke; browser cases use
exact `1440x900` desktop and `500x844` mobile inner viewports. They prove the
standalone WebAssembly Client, the separately hosted API, cross-origin Resource
Timing and API CORS, title and landmarks, calendar period semantics, an honest
empty state, not-found recovery, skip navigation, key-region geometry, and
visible text containment.

The hardened checks also assert post-navigation page `error` and
`unhandledrejection` events, explicit API health status, critical same-origin
Resource Timing entries, and static-host response status. Direct W3C WebDriver
Classic provides neither pre-navigation script injection nor Firefox console-log
retrieval. Errors raised before the post-navigation listener is installed are
therefore the remaining observability limitation; this is not a browser-evidence
blocker.

The four screenshots are under the repository-ignored `TestResults` tree. Both
desktop files are 47,332 bytes with SHA-256
`a63a564fff9a9811ea7d58c832ad0e57b3062e1ba2648e56277d46223356f882`.
Both mobile files are 26,266 bytes with SHA-256
`5ba6d48e6b703d50dfd512e95f9c2cf58ab8ef4eb2c37791428f6ef0fca28ddf`.
Cleanup verification found no owned API, Firefox, geckodriver, or Client-host
process left running; ports, temporary profiles, publish output, and
process-scoped environment variables were removed.

This evidence meets Feature 001's browser-specific acceptance and Definition
of Done requirements. It does not satisfy the separate PostgreSQL/Podman gate,
which remains blocked because the installed Podman 5.8.3 engine requires an
administrator upgrade to WSL 2.7.11.

## Current Sources

All sources were accessed 2026-08-02.

- [Selenium.WebDriver 4.46.0 on NuGet](https://www.nuget.org/packages/Selenium.WebDriver/4.46.0)
- [Selenium 4.46.0 license](https://github.com/SeleniumHQ/selenium/blob/selenium-4.46.0/LICENSE)
- [Selenium Manager manifest](https://github.com/SeleniumHQ/selenium/blob/selenium-4.46.0/rust/Cargo.toml)
- [Selenium Manager lock file](https://github.com/SeleniumHQ/selenium/blob/selenium-4.46.0/rust/Cargo.lock)
- [Selenium Manager binary pins](https://github.com/SeleniumHQ/selenium/blob/selenium-4.46.0/common/selenium_manager.bzl)
- [Selenium Manager behavior and telemetry](https://www.selenium.dev/documentation/selenium_manager/)
- [Selenium project and funding](https://www.selenium.dev/about/)
- [Selenium third-party ecosystem](https://www.selenium.dev/ecosystem/)
- [geckodriver 0.37.1 release](https://github.com/mozilla/geckodriver/releases/tag/v0.37.1)
- [geckodriver assets and hashes](https://github.com/mozilla/geckodriver/releases/expanded_assets/v0.37.1)
- [geckodriver license](https://github.com/mozilla/geckodriver/blob/v0.37.1/LICENSE)
- [geckodriver compatibility](https://firefox-source-docs.mozilla.org/testing/geckodriver/Support.html)
- [Direct geckodriver HTTP usage](https://firefox-source-docs.mozilla.org/testing/geckodriver/Usage.html)
- [W3C WebDriver](https://www.w3.org/TR/webdriver2/)
- [Firefox 153.0.1 release notes](https://www.firefox.com/en-US/firefox/153.0.1/releasenotes/)
- [Firefox EME-free artifact](https://ftp.mozilla.org/pub/firefox/releases/153.0.1/win64-EME-free/en-US/)
- [Firefox signed checksums](https://ftp.mozilla.org/pub/firefox/releases/153.0.1/)
- [Mozilla release key](https://ftp.mozilla.org/pub/firefox/releases/153.0.1/KEY)
- [Firefox executable licensing](https://www.mozilla.org/en-US/foundation/licensing/)
- [Firefox license inventory source](https://searchfox.org/firefox-main/source/toolkit/content/license.html)
- [Firefox 153 security advisory](https://www.mozilla.org/en-US/security/advisories/mfsa2026-68/)
- [Firefox Professional Support](https://www.firefox.com/en-US/browsers/enterprise/)

## Superseded Playwright Decision

The following Playwright analysis is retained as historical evidence. Its
blocked verdict remains valid, but it is not the current candidate decision.

**Status: Blocked. `Microsoft.Playwright` 1.61.0 does not currently qualify for
adoption or for the approved narrow browser-runtime license exception.**

The exception becomes available only after a complete automation, artifact,
transitive/runtime dependency, provenance, license, and commercial-model
inventory passes review. The current chain fails the repository's independent
no-paid-counterpart rule and remains incomplete at the binary notice level.
The package must not be added, and browsers must not be installed, until the
user approves a policy change or a different mechanism passes review.

Research was performed against tagged upstream sources and current official
product/license pages on 2026-08-02. This is dependency-policy research, not
legal advice.

## Evaluated Chain

| Artifact | Verified provenance and version | License evidence | Result |
| --- | --- | --- | --- |
| .NET API | NuGet `Microsoft.Playwright` 1.61.0; Microsoft `playwright-dotnet` tag `v1.61.0` | NuGet project declares MIT | License allowed, but not sufficient for the runtime chain |
| Driver | Bundled `playwright-core` 1.61.1 beta build in `.playwright/package` | Upstream package declares Apache-2.0 | License allowed; prerelease/runtime status requires an exact decision |
| Node runtime | Bundled Node.js 24.16.0 executables for five platforms | Node license file is bundled; it incorporates a large third-party notice inventory | Complete component-by-component classification not established |
| Chromium | Chrome for Testing 149.0.7827.55, revision 1228 | Chromium top-level BSD-style license plus bundled third-party notices | BSD is outside the allowlist; complete binary notices not captured |
| Chromium headless shell | 149.0.7827.55, revision 1228 | Same broad Chromium closure | Not separately cleared |
| Firefox | Playwright build 151.0, revision 1532 | Mozilla source is primarily MPL-2.0 with additional component notices | MPL-2.0 is outside the allowlist; binary closure not captured |
| WebKit | Playwright build 26.5, revision 2311 with platform overrides | WebKit documents LGPL and BSD portions; platform bundle adds system libraries | LGPL and BSD are outside the allowlist; binary closure not captured |
| FFmpeg helper | Playwright build revision 1011 with macOS override 1010 | FFmpeg is LGPL-2.1-or-later unless GPL parts are enabled | Exact Playwright build configuration and corresponding source/notices not verified |
| Windows dependency helper | `winldd` revision 1007, installed by default on Windows | Exact source, license, and binary notices not completed | Blocked |
| Linux host packages | Distribution packages selected by Playwright install scripts | Varies by distribution and package | Not inventoried; `install-deps` is not approved |

Optional tip-of-tree, beta, Android, branded Chrome, and Microsoft Edge targets
were identified in the registry but are not required by the proposed default
E2E path. They receive no approval from this review.

## Download and Integrity Behavior

The NuGet package ships the .NET assembly, the Apache-licensed JavaScript
driver, and platform-specific Node executables. Running Playwright's install
command then downloads platform-specific browser and helper ZIP archives from
Playwright/Microsoft CDN endpoints into a user cache.

The browser installer checks HTTP success, reports byte progress, retries
mirrors, extracts the archive, checks process exit and expected executable
presence, sets permissions, and writes an `INSTALLATION_COMPLETE` marker. Source
review found no expected cryptographic digest, detached signature, or
certificate-pinning check for these browser archives. A SHA-1 calculation in
the registry identifies a package path for cache ownership; it does not
authenticate archive contents.

The .NET release tooling likewise downloads the `playwright-core` npm tarball
and official Node archives over HTTPS and extracts selected content without
checking a pinned hash or signature. Status, extraction, size, executable, and
marker checks detect operational failures, not artifact substitution.

This does not prove that an archive is malicious. It means the current
installer behavior does not satisfy a claim of cryptographically verified
browser provenance.

## Commercial-Model Finding

Microsoft offers **Playwright Workspaces**, a fully managed Azure service built
on Playwright with cloud-hosted browsers. Its free trial lasts 30 days and
includes 100 test minutes; exceeding the limit converts the workspace to
pay-as-you-go billing. Azure's pricing page describes Playwright-based testing
as usage-priced.

Feature 001 excludes dependencies whose project or publisher offers a paid,
proprietary, commercial, or commercial-tier counterpart. The narrow proposed
browser-runtime exception is license-scoped and does not waive that rule.
Therefore this finding independently blocks `Microsoft.Playwright`, even if all
browser licenses and notices were later classified.

## Unresolved Evidence

- Capture the exact notices and component inventory from every Windows archive
  that would be installed, including Chromium, headless shell, Firefox, WebKit,
  FFmpeg, and `winldd`.
- Classify Node.js 24.16.0's complete third-party inventory against the exact
  allowlist and exception scope.
- Establish the Playwright FFmpeg build flags, corresponding source offer, and
  license obligations.
- Establish browser archive hashes or another independently verified integrity
  control if cryptographic provenance remains required.
- Decide whether the no-paid-counterpart policy changes for Playwright. Only the
  user can approve that change.
- If policy changes, define the minimum browser set. Approval of one artifact
  must not be read as approval of all default downloads.

## Sources

All sources were accessed 2026-08-02.

- [Microsoft.Playwright 1.61.0 on NuGet](https://www.nuget.org/packages/Microsoft.Playwright/1.61.0)
- [playwright-dotnet version properties](https://github.com/microsoft/playwright-dotnet/blob/v1.61.0/src/Common/Version.props)
- [playwright-dotnet package project](https://github.com/microsoft/playwright-dotnet/blob/v1.61.0/src/Playwright/Playwright.csproj)
- [playwright-core package metadata](https://github.com/microsoft/playwright/blob/v1.61.0/packages/playwright-core/package.json)
- [Playwright browser revisions](https://github.com/microsoft/playwright/blob/v1.61.0/packages/playwright-core/browsers.json)
- [Playwright registry and download locations](https://github.com/microsoft/playwright/blob/v1.61.0/packages/playwright-core/src/server/registry/index.ts)
- [Playwright browser downloader](https://github.com/microsoft/playwright/blob/v1.61.0/packages/playwright-core/src/server/registry/browserFetcher.ts)
- [Node.js license inventory](https://github.com/nodejs/node/blob/v24.16.0/LICENSE)
- [Chromium license](https://chromium.googlesource.com/chromium/src/+/main/LICENSE)
- [Mozilla Public License 2.0](https://www.mozilla.org/en-US/MPL/2.0/)
- [WebKit licensing](https://webkit.org/licensing-webkit/)
- [FFmpeg legal information](https://ffmpeg.org/legal.html)
- [Playwright Workspaces overview](https://learn.microsoft.com/en-us/azure/app-testing/playwright-workspaces/overview-what-is-microsoft-playwright-workspaces)
- [Playwright Workspaces free trial](https://learn.microsoft.com/en-us/azure/app-testing/playwright-workspaces/how-to-try-playwright-workspaces-free)
- [Azure App Testing pricing](https://azure.microsoft.com/en-us/pricing/details/app-testing/)
