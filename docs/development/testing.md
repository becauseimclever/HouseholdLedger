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
| Infrastructure integration | `HouseholdLedger.Infrastructure.IntegrationTests` | 2 | One in-process; one Podman PostgreSQL |
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
separately running API. The 22 Client cases are classified as 12 unit, 8 bUnit
component, and 2 structural tests. They do not require a browser or live API.

The EndToEndTests project uses NUnit and contains one separate-process HTTP
system smoke plus desktop and mobile browser cases. Its test project has no
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
Testcontainers, or Docker Desktop. Podman 5.8.3 is installed in the evidenced
Windows environment, but its engine cannot run until an administrator upgrades
WSL to required version 2.7.11. After that upgrade, make sure the Podman machine
is running before invoking the owned harness:

```powershell
podman version
pwsh tests/HouseholdLedger.Infrastructure.IntegrationTests/Run-PostgreSqlTests.ps1
```

The script pulls `docker.io/library/postgres:18` by default, selects a free
loopback port, creates a unique container and database, generates a temporary
password, and exposes the connection only to the test process through
`HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING`. Its `finally` block removes
the container and confirms removal.

Choose another already reviewed image only through the script parameter:

```powershell
pwsh tests/HouseholdLedger.Infrastructure.IntegrationTests/Run-PostgreSqlTests.ps1 `
  -PostgresImage "docker.io/library/postgres:18"
```

The resource-free infrastructure registration test currently passes. The real
PostgreSQL connectivity test skips when
`HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING` is absent. The required WSL
2.7.11 upgrade needs unavailable administrator elevation, so Podman provisioning
could not start in the current validation.
Until the owned harness produces a successful real-provider transcript,
PostgreSQL acceptance evidence and Feature 001 completion remain blocked.

## Browser End-to-End Tests

Browser E2E uses a package-free test-owned W3C WebDriver client built with
`HttpClient` and `System.Text.Json`. It talks directly to the pinned user-local
Firefox 153.0.1 Windows x64 en-US EME-free and geckodriver 0.37.1 Windows x64
runtime. It does not use Selenium, Selenium Manager, Playwright, a cloud grid,
or a runtime downloader. Before launching any browser process, the tests enforce
the approved Firefox and geckodriver SHA-256 values.

The complete E2E run requires six normalized absolute paths and two distinct,
available loopback ports:

- `HOUSEHOLDLEDGER_API_ARTIFACT`: a freshly published file named exactly
  `HouseholdLedger.Api.dll`.
- `HOUSEHOLDLEDGER_CLIENT_PUBLISH_DIR`: the freshly published Client
  `wwwroot` containing `index.html`, `appsettings.json`, and
  `_framework/blazor.webassembly.js`.
- `HOUSEHOLDLEDGER_FIREFOX_BINARY`: the approved Firefox executable.
- `HOUSEHOLDLEDGER_GECKODRIVER`: the approved geckodriver executable.
- `HOUSEHOLDLEDGER_E2E_PROFILE_ROOT`: an existing, test-owned directory where
  each browser case creates and removes its unique Firefox profile.
- `HOUSEHOLDLEDGER_E2E_OUTPUT_DIR`: an existing, test-owned directory where the
  run writes screenshots and diagnostics.
- `HOUSEHOLDLEDGER_E2E_API_PORT`: an available loopback TCP port reserved for
  the test-owned API host.
- `HOUSEHOLDLEDGER_E2E_CLIENT_PORT`: a different available loopback TCP port
  reserved for the test-owned static Client host.

Use fresh API and Client publishes for every run:

```powershell
$publishRoot = Join-Path ([System.IO.Path]::GetTempPath()) `
  ("HouseholdLedger-BrowserE2E-" + [guid]::NewGuid().ToString("N"))
$apiOutput = Join-Path $publishRoot "api"
$clientOutput = Join-Path $publishRoot "client"
$profileRoot = Join-Path $publishRoot "profiles"
$e2eOutput = Join-Path $publishRoot "output"

try {
  dotnet publish src/HouseholdLedger.Api --configuration Release `
    --no-restore --output $apiOutput
  if ($LASTEXITCODE -ne 0) { throw "API publish failed." }

  dotnet publish src/HouseholdLedger.Client --configuration Release `
    --no-restore --output $clientOutput
  if ($LASTEXITCODE -ne 0) { throw "Client publish failed." }

  $env:HOUSEHOLDLEDGER_API_ARTIFACT = Join-Path `
    $apiOutput "HouseholdLedger.Api.dll"
  $env:HOUSEHOLDLEDGER_CLIENT_PUBLISH_DIR = Join-Path `
    $clientOutput "wwwroot"
  $env:HOUSEHOLDLEDGER_FIREFOX_BINARY = Join-Path $env:LOCALAPPDATA `
    "HouseholdLedger\BrowserTestRuntime\firefox\153.0.1-eme-free\core\firefox.exe"
  $env:HOUSEHOLDLEDGER_GECKODRIVER = Join-Path $env:LOCALAPPDATA `
    "HouseholdLedger\BrowserTestRuntime\geckodriver\0.37.1\geckodriver.exe"
  New-Item -ItemType Directory -Path $profileRoot, $e2eOutput | Out-Null
  $env:HOUSEHOLDLEDGER_E2E_PROFILE_ROOT = $profileRoot
  $env:HOUSEHOLDLEDGER_E2E_OUTPUT_DIR = $e2eOutput
  $env:HOUSEHOLDLEDGER_E2E_API_PORT = "51271"
  $env:HOUSEHOLDLEDGER_E2E_CLIENT_PORT = "51272"

  dotnet test tests/HouseholdLedger.EndToEndTests `
    --configuration Release --no-restore
  if ($LASTEXITCODE -ne 0) { throw "End-to-end tests failed." }
}
finally {
  Remove-Item Env:HOUSEHOLDLEDGER_API_ARTIFACT -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_CLIENT_PUBLISH_DIR -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_FIREFOX_BINARY -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_GECKODRIVER -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_E2E_PROFILE_ROOT -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_E2E_OUTPUT_DIR -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_E2E_API_PORT -ErrorAction SilentlyContinue
  Remove-Item Env:HOUSEHOLDLEDGER_E2E_CLIENT_PORT -ErrorAction SilentlyContinue
  Remove-Item $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
}
```

Before using the fixed example ports, confirm that both are available and
reserve them for this workflow. The test validates the ports before it starts
its owned hosts and rejects equal or in-use values. Do not point the profile or
output variables at a shared directory. The test removes each profile; the
outer `finally` block removes the allocated output and fresh publishes.

The two browser cases set and verify exact inner viewports of `1440x900` and
`500x844`. Each case proves that the published standalone WebAssembly Client
loads, calls the separately hosted API across origins, and receives the API's
explicit CORS permission. Resource Timing must contain the configured
`/api/v1/health` URL. Assertions cover the document title, landmarks, calendar
period and caption, honest empty state, not-found route and return interaction,
skip link, key-region geometry, and visible text containment.

Two complete consecutive runs passed all three EndToEndTests cases on
2026-08-03 in 8.7 seconds and 8.1 seconds. Each run used fresh publishes and
unique loopback ports and profiles. Cleanup removed the owned API, Firefox,
geckodriver, and Client-host processes; released ports; removed profiles,
temporary publish outputs, and process-scoped environment variables; and left
no running owned process.

The hardened browser checks cover post-navigation `error` and
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
EndToEndTests project. Use the browser workflow's fresh API and Client publishes
and all four environment variables, replace the focused `dotnet test` target
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
