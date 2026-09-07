# Testing

HouseholdLedger places tests at the boundary they are intended to prove. Run a
focused project while developing, then run every available layer before a
handoff. Database and system tests have additional gates described below.

## Test Projects

| Layer | Project | Current cases | External resource |
| --- | --- | ---: | --- |
| Domain unit | `HouseholdLedger.Domain.UnitTests` | 0 | None |
| Application unit | `HouseholdLedger.Application.UnitTests` | 0 | None |
| API contract | `HouseholdLedger.Api.Contracts.Tests` | 2 | None |
| API structural | `HouseholdLedger.Api.Contracts.Tests` and `HouseholdLedger.Api.IntegrationTests` | 2 | None |
| Client unit | `HouseholdLedger.Client.ComponentTests` | 12 | None |
| Client component | `HouseholdLedger.Client.ComponentTests` | 8 | None |
| Client structural | `HouseholdLedger.Client.ComponentTests` | 2 | None |
| API integration | `HouseholdLedger.Api.IntegrationTests` | 12 | In-process API host |
| Infrastructure integration | `HouseholdLedger.Infrastructure.IntegrationTests` | 3 | One in-process; two Podman PostgreSQL |
| HTTP system smoke | `HouseholdLedger.EndToEndTests` | 1 | Separate API process |
| Browser end to end | `HouseholdLedger.EndToEndTests` | 2 | Published API and Client, Firefox, and geckodriver |

## Resource-Free Tests

After restoring and building, run the resource-free layers individually:

```powershell
dotnet test tests/HouseholdLedger.Domain.UnitTests --no-build --no-restore
dotnet test tests/HouseholdLedger.Application.UnitTests --no-build --no-restore
dotnet test tests/HouseholdLedger.Api.Contracts.Tests --no-build --no-restore
dotnet test tests/HouseholdLedger.Client.ComponentTests --no-build --no-restore
dotnet test tests/HouseholdLedger.Api.IntegrationTests --no-build --no-restore
```

API integration tests use `WebApplicationFactory`; they do not require a
separately running API. The 37 Client cases cover unit, bUnit component, and
structural behavior. They do not require a browser or live API.

The EndToEndTests project uses xUnit v3 and contains one separate-process HTTP
system smoke plus two desktop browser journeys. Its test project has no
HouseholdLedger project reference. The HTTP smoke starts a freshly published
API assembly with a minimal environment, calls health and runtime OpenAPI over
HTTP, and stops its owned process. Run it alone when the approved browser
runtime is not available.

The test does not discover an arbitrary build artifact. Set
`HOUSEHOLDLEDGER_API_ARTIFACT` to the normalized absolute path of a freshly
published file named exactly `HouseholdLedger.Api.dll`. This PowerShell 7
workflow creates an explicit temporary publish output and removes both the
variable and output even when publishing or testing fails:

```powershell
$publishOutput = Join-Path ([System.IO.Path]::GetTempPath()) `
  ("HouseholdLedger-SystemTest-" + [guid]::NewGuid().ToString("N"))

try {
  dotnet publish src/HouseholdLedger.Api --configuration Release `
    --no-restore --output $publishOutput
  if ($LASTEXITCODE -ne 0) { throw "API publish failed." }

  $env:HOUSEHOLDLEDGER_API_ARTIFACT = Join-Path `
    $publishOutput "HouseholdLedger.Api.dll"
  dotnet test tests/HouseholdLedger.EndToEndTests `
    --configuration Release --no-restore `
    --filter "FullyQualifiedName~ApiSystemSmokeTests"
  if ($LASTEXITCODE -ne 0) { throw "HTTP system test failed." }
}
finally {
  Remove-Item Env:HOUSEHOLDLEDGER_API_ARTIFACT -ErrorAction SilentlyContinue
  Remove-Item $publishOutput -Recurse -Force -ErrorAction SilentlyContinue
}
```

Two consecutive executions of this workflow passed on 2026-08-02. Each run
used its explicitly configured fresh publish, stopped the owned API process,
cleared the environment variable, and removed the temporary output.

## PostgreSQL Integration Tests

PostgreSQL behavior must be tested against PostgreSQL, not EF InMemory, SQLite,
Testcontainers, or Docker Desktop. Start the repository's existing Podman
machine before invoking the owned harness:

### Manual Development and Automated Test Databases

The persistent `PiDB` / `BudgetV2` database is for manual development and keeps
its data between runs. Configure it through API user secrets and initialize it
once as described in [Development Setup](setup.md). Do not point automated tests
at that database because integration and browser tests create, mutate, and remove
records as part of their assertions.

Continue to use the harness below for automated PostgreSQL tests. It creates and
removes an isolated database and supplies
`HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING` only to the test process.

```powershell
podman version
pwsh tests/HouseholdLedger.Infrastructure.IntegrationTests/Run-PostgreSqlTests.ps1
```

The script pulls `docker.io/library/postgres:18` by default, selects a free
loopback port, creates a unique container and database, generates a temporary
password, and exposes the connection only to the test process through
`HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING`. Its `finally` block removes
the container and confirms removal.

To run the real-provider tests followed by the hosted transaction browser journey
against that same isolated database and a fresh Release publish:

```powershell
pwsh tests/HouseholdLedger.Infrastructure.IntegrationTests/Run-PostgreSqlTests.ps1 `
  -RunHostedBrowserJourneys
```

This option uses the pinned user-local Firefox and geckodriver runtime, selects
an available API port, and removes the temporary publish, profiles, output,
environment variables, and PostgreSQL container in its `finally` cleanup.

Choose another already reviewed image only through the script parameter:

```powershell
pwsh tests/HouseholdLedger.Infrastructure.IntegrationTests/Run-PostgreSqlTests.ps1 `
  -PostgresImage "docker.io/library/postgres:18"
```

The resource-free infrastructure registration test passes. Real PostgreSQL
tests skip when
`HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING` is absent. The required WSL
real-provider and development-initialization tests, ran the hosted browser
journey, and cleaned its PostgreSQL 18 container on 2026-09-05.

## Browser End-to-End Tests

Browser E2E uses a package-free test-owned W3C WebDriver client built with
`HttpClient` and `System.Text.Json`. It talks directly to the pinned user-local
Firefox 153.0.1 Windows x64 en-US EME-free and geckodriver 0.37.1 Windows x64
runtime. It does not use Selenium, Selenium Manager, Playwright, a cloud grid,
or a runtime downloader. Before launching any browser process, the tests enforce
the approved Firefox and geckodriver SHA-256 values.

The complete E2E run requires five normalized absolute paths, one available
loopback port, and the isolated PostgreSQL connection supplied by the combined
harness:

- `HOUSEHOLDLEDGER_API_ARTIFACT`: a freshly published file named exactly
  `HouseholdLedger.Api.dll`.
- `HOUSEHOLDLEDGER_FIREFOX_BINARY`: the approved Firefox executable.
- `HOUSEHOLDLEDGER_GECKODRIVER`: the approved geckodriver executable.
- `HOUSEHOLDLEDGER_E2E_PROFILE_ROOT`: an existing, test-owned directory where
  each browser case creates and removes its unique Firefox profile.
- `HOUSEHOLDLEDGER_E2E_OUTPUT_DIR`: an existing, test-owned directory where the
  run writes screenshots and diagnostics.
- `HOUSEHOLDLEDGER_E2E_API_PORT`: an available loopback TCP port reserved for
  the test-owned API host.
- `HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING`: a test-only connection to
  the migrated isolated database used by the transaction journey.

Use a fresh API publish for every run. It includes the referenced Client static
web assets:

```powershell
$publishRoot = Join-Path ([System.IO.Path]::GetTempPath()) `
  ("HouseholdLedger-BrowserE2E-" + [guid]::NewGuid().ToString("N"))
$apiOutput = Join-Path $publishRoot "api"
$profileRoot = Join-Path $publishRoot "profiles"
$e2eOutput = Join-Path $publishRoot "output"

try {
  dotnet publish src/HouseholdLedger.Api --configuration Release `
    --no-restore --output $apiOutput
  if ($LASTEXITCODE -ne 0) { throw "API publish failed." }

  $env:HOUSEHOLDLEDGER_API_ARTIFACT = Join-Path `
    $apiOutput "HouseholdLedger.Api.dll"
  $env:HOUSEHOLDLEDGER_FIREFOX_BINARY = Join-Path $env:LOCALAPPDATA `
    "HouseholdLedger\BrowserTestRuntime\firefox\153.0.1-eme-free\core\firefox.exe"
  $env:HOUSEHOLDLEDGER_GECKODRIVER = Join-Path $env:LOCALAPPDATA `
    "HouseholdLedger\BrowserTestRuntime\geckodriver\0.37.1\geckodriver.exe"
  New-Item -ItemType Directory -Path $profileRoot, $e2eOutput | Out-Null
  $env:HOUSEHOLDLEDGER_E2E_PROFILE_ROOT = $profileRoot
  $env:HOUSEHOLDLEDGER_E2E_OUTPUT_DIR = $e2eOutput
  $env:HOUSEHOLDLEDGER_E2E_API_PORT = "51271"

  dotnet test tests/HouseholdLedger.EndToEndTests `
    --configuration Release --no-restore
  if ($LASTEXITCODE -ne 0) { throw "End-to-end tests failed." }
}
finally {
  Remove-Item Env:HOUSEHOLDLEDGER_API_ARTIFACT -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_FIREFOX_BINARY -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_GECKODRIVER -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_E2E_PROFILE_ROOT -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_E2E_OUTPUT_DIR -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_E2E_API_PORT -ErrorAction SilentlyContinue
  Remove-Item $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
}
```

Before using the example port, confirm that it is available and reserve it for
this workflow. The test validates the port before it starts its owned host. Do
not point the profile or output variables at a shared directory. The test
removes each profile; the outer `finally` block removes the allocated output
and fresh publish.

The browser cases use the published API URL to exercise the API-hosted
WebAssembly Client. Current assertions cover the calendar workspace, calendar
interaction, persisted transaction creation/correction/removal with backend
rereads, open-source notices navigation and return behavior, accessibility,
normal-flow geometry, and visible text containment. Prefer the combined owned
harness above for the transaction journey because it configures the database
and all browser inputs together.

Historical two-host evidence consists of two complete consecutive runs that
passed all three EndToEndTests cases on 2026-08-03 in 8.7 seconds and 8.1
seconds. Each run used fresh publishes and unique loopback ports and profiles.
Cleanup removed the owned API, Firefox, geckodriver, and Client-host processes;
released ports; removed profiles, temporary publish outputs, and
process-scoped environment variables; and left no running owned process.

Those historical hardened browser checks cover post-navigation `error` and
`unhandledrejection` events, explicit API health status, critical same-origin
Resource Timing entries, and static-host response status. Direct W3C WebDriver
Classic cannot inject diagnostics before navigation or retrieve Firefox console
logs, so errors occurring before the post-navigation listener is installed are
the residual observability limitation.

Screenshots are generated below the ignored
`tests/HouseholdLedger.EndToEndTests/bin/Release/net10.0/TestResults/browser-e2e`
directory. The two runs produced:

| Run | Viewport | File | Bytes | SHA-256 |
| --- | --- | --- | ---: | --- |
| 1 | Desktop | `calendar-20260803-222147485-35300-desktop.png` | 47,332 | `a63a564fff9a9811ea7d58c832ad0e57b3062e1ba2648e56277d46223356f882` |
| 1 | Mobile | `calendar-20260803-222151070-35300-mobile.png` | 26,266 | `5ba6d48e6b703d50dfd512e95f9c2cf58ab8ef4eb2c37791428f6ef0fca28ddf` |
| 2 | Desktop | `calendar-20260803-222201916-52784-desktop.png` | 47,332 | `a63a564fff9a9811ea7d58c832ad0e57b3062e1ba2648e56277d46223356f882` |
| 2 | Mobile | `calendar-20260803-222205450-52784-mobile.png` | 26,266 | `5ba6d48e6b703d50dfd512e95f9c2cf58ab8ef4eb2c37791428f6ef0fca28ddf` |

The exact runtime decision and post-provision evidence are in the
[Browser E2E Dependency Review](browser-e2e-dependency-review.md). Do not add
Selenium or Playwright to this package-free test path.

## Full Validation

Run the focused system-test workflow above first. For a full solution test, the
same explicit artifact contract applies because the solution includes the
EndToEndTests project. Use the browser workflow's fresh API publish and explicit
environment variables, replace the focused `dotnet test` target
with `HouseholdLedger.slnx`, and retain the same `finally` cleanup. The remaining
validation commands are:

```powershell
dotnet build HouseholdLedger.slnx --no-restore
dotnet format HouseholdLedger.slnx --verify-no-changes --no-restore
```

Do not run a bare `dotnet test HouseholdLedger.slnx` and expect E2E tests to
search build or runtime directories. They intentionally fail when their
required explicit artifact paths are absent or do not satisfy their contracts.

With the two browser cases, the suite has 43 cases classified as 12 unit, 8
component, 2 contract, 14 integration, 1 system, 2 browser E2E, and 4 structural
tests. The available result is 42 passed and one skipped; the skipped case is
the real PostgreSQL connectivity test. Unit tests are 27.9% of the current
suite and are not yet its majority. The empty Domain and Application unit
projects are intentional because the scaffold has no domain or application
behavior to test. Browser evidence is complete for Feature 001; PostgreSQL
evidence remains blocked because the WSL 2.7.11 upgrade required by the Podman
engine needs unavailable administrator elevation.
