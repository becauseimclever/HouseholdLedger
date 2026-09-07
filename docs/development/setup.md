# Development Setup

This guide prepares a contributor to build and run HouseholdLedger locally.

## Prerequisites

- .NET SDK 10.0.302. The repository's `global.json` allows a later patch in the
  same 10.0.3xx feature band.
- PowerShell 7 for the commands below.
- A trusted ASP.NET Core development certificate for local HTTPS.
- Podman only when running the PostgreSQL integration tests.
- The verified user-local Firefox 153.0.1 Windows x64 en-US EME-free and
  geckodriver 0.37.1 Windows x64 runtime only when running browser E2E tests.

Confirm the selected SDK and prepare HTTPS:

```powershell
dotnet --version
dotnet dev-certs https --check --trust
```

The repository does not currently have a local .NET tool manifest. Do not run
`dotnet tool restore` or install a global `dotnet-ef` tool as part of setup.

## Restore and Build

Run all commands from the `HouseholdLedger` repository root:

```powershell
dotnet restore HouseholdLedger.slnx --locked-mode
dotnet build HouseholdLedger.slnx --no-restore
```

Locked restore uses the committed `packages.lock.json` files and the NuGet.org
source fixed by `Directory.Build.props`. Restore also audits direct and
transitive packages. High and critical audit findings fail the build.

## Synchronize OpenAPI

Compare runtime OpenAPI with the checked canonical artifact without changing
files:

```powershell
pwsh scripts/openapi/Sync-OpenApi.ps1
```

After reviewing an intentional controller-contract change, update the checked
artifact explicitly and then run the compare again:

```powershell
pwsh scripts/openapi/Sync-OpenApi.ps1 -Update
pwsh scripts/openapi/Sync-OpenApi.ps1
```

Use `-Port <port>` only when a fixed isolated loopback port is needed. The
default selects an available port. Compare mode is nonmutating, and automated
tests do not update the artifact.

## Run the Application

The API hosts the Blazor WebAssembly client and serves the API from the same
origin. Configure the permanent manual-development PostgreSQL connection through
ASP.NET Core user secrets. Enter the complete connection string at the masked
prompt; for the shared local database its non-secret fields are `Host=PiDB`,
`Port=5432`, `Database=BudgetV2`, and `Username=BudgetApp`.

```powershell
$connectionString = Read-Host "PostgreSQL connection string" -MaskInput
dotnet user-secrets set "ConnectionStrings:HouseholdLedger" `
  $connectionString --project src/HouseholdLedger.Api
Remove-Variable connectionString
```

Do not commit the password or connection string to an appsettings file, script,
`.env` file, or browser-delivered configuration.

For a new empty database, explicitly apply migrations and add the canonical
three-account, seven-transaction development fixture once:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/HouseholdLedger.Api -- `
  --initialize-development-database
```

The initializer exits after completion. It refuses to add data when either
accounts or transactions already exist, and normal API startup never migrates,
resets, or seeds the database. After the one-time initialization, run the API:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "https://localhost:7241"
dotnet run --project src/HouseholdLedger.Api --no-build
```

Open `https://localhost:7241`. The shell should show the current calendar period
and persisted fixture data. Verify the API independently at
`https://localhost:7241/api/v1/health`. Its generated OpenAPI document is at
`https://localhost:7241/openapi/v1.json`.

For a separately hosted custom frontend, configure an explicit
`Cors:AllowedOrigins` entry on the API and set that frontend's API base URL to
the API origin. The API does not enable a wildcard origin. Browser-delivered
configuration is public and must never contain credentials or secrets.

## Publish the Hosted Application

Publish the API project to produce the deployable application:

```powershell
dotnet publish src/HouseholdLedger.Api --no-restore --output artifacts/publish/api
```

The API output is an ASP.NET Core application that includes and serves the
referenced Blazor WebAssembly Client. A replacement frontend may still consume
the language-neutral HTTP/OpenAPI contract without referencing server
implementation assemblies.

Run the published application from its output directory so that the process
uses the deployed `wwwroot` as its content root:

```powershell
Push-Location artifacts/publish/api
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "https://localhost:7241"
dotnet HouseholdLedger.Api.dll
```

Starting the published DLL from another working directory requires setting its
content root explicitly. Starting unpublished build output under `Production`
is not a deployment substitute: ASP.NET Core intentionally enables source-tree
static web assets only for local `Development`. Published output serves the
Client in `Production`, `Staging`, and other runtime environments without that
development-only behavior.

Generated `artifacts` content is local output and must not be committed.

## Run Browser E2E

Browser E2E uses a package-free direct W3C WebDriver client implemented with
built-in .NET HTTP and JSON APIs. It does not install or use Selenium,
Playwright, a browser manager, or a runtime downloader.

The approved runtime is pinned below
`%LOCALAPPDATA%\HouseholdLedger\BrowserTestRuntime`. Before running the tests,
publish the API to a fresh temporary root and set five normalized absolute
paths plus one available loopback port. The tests enforce the
approved Firefox and geckodriver SHA-256 values before launching a browser
process:

- `HOUSEHOLDLEDGER_API_ARTIFACT` for the fresh
  `HouseholdLedger.Api.dll`.
- `HOUSEHOLDLEDGER_FIREFOX_BINARY` for the exact Firefox 153.0.1 EME-free
  `firefox.exe`.
- `HOUSEHOLDLEDGER_GECKODRIVER` for the exact geckodriver 0.37.1
  `geckodriver.exe`.
- `HOUSEHOLDLEDGER_E2E_PROFILE_ROOT` and `HOUSEHOLDLEDGER_E2E_OUTPUT_DIR` for
  existing, test-owned directories beneath the run's temporary root.
- `HOUSEHOLDLEDGER_E2E_API_PORT` for an available loopback port reserved for
  that run.

Use the complete self-cleaning PowerShell workflow in [Testing](testing.md).
The test owns its loopback ports, processes, Firefox profiles, and
screenshots; its cleanup verifies that processes stop, ports are released, and
temporary profiles are removed. The workflow removes its publish output and
process-scoped environment variables in `finally`.

## Next Steps

- [Testing](testing.md)
- [Contribution Guide](contributing.md)
- [Troubleshooting](troubleshooting.md)
- [Architecture Overview](../architecture/overview.md)
