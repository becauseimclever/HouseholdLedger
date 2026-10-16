# Development Setup

This guide prepares a contributor to build and run HouseholdLedger locally.

## Prerequisites

- .NET SDK 10.0.402. The repository's `global.json` allows a later patch in the
  same 10.0.4xx feature band. This aligns the build toolchain with the patched
  ASP.NET Core and WebAssembly 10.0.13 runtime used by the baseline container.
- PowerShell 7 for the commands below.
- A trusted ASP.NET Core development certificate for local HTTPS.
- Docker (or Podman using the integration runner's `-ContainerCommand podman`)
  when running disposable PostgreSQL tests.
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
origin. Configure a dedicated manual-development PostgreSQL connection through
ASP.NET Core user secrets. Enter its complete connection string at the masked
prompt.

```powershell
$connectionString = Read-Host "PostgreSQL connection string" -MaskInput
dotnet user-secrets set "ConnectionStrings:HouseholdLedger" `
  $connectionString --project src/HouseholdLedger.Api
Remove-Variable connectionString
```

Do not commit the password or connection string to an appsettings file, script,
`.env` file, or browser-delivered configuration.

For a new empty database, the following optional initializer applies migrations
and adds the canonical three-account, seven-transaction fixture:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/HouseholdLedger.Api -- `
  --initialize-development-database
```

The initializer exits after completion. It refuses to add data when either
accounts or transactions already exist. Do not use it for a fresh manual setup
walkthrough or an existing ledger: apply schema migrations explicitly instead.
Normal API startup never migrates, resets, or seeds the database.

To apply schema migrations without sample data, use the configured API host:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src\HouseholdLedger.Api -- --migrate-database
```

This reads the same API User Secrets as local startup and requires no global
EF tool. It exits after migration. Never point automated tests at this database.

Once the schema is current, run the API:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "https://localhost:7241"
dotnet run --project src/HouseholdLedger.Api --no-build
```

Open `https://localhost:7241`. The shell should show the current calendar period
and your persisted data (or an empty ledger if no fixture was added).
Verify the API independently at
`https://localhost:7241/api/v1/health`. Its generated OpenAPI document is at
`https://localhost:7241/openapi/v1.json`.

For a separately hosted custom frontend, configure an explicit
`Cors:AllowedOrigins` entry on the API and set that frontend's API base URL to
the API origin. The API does not enable a wildcard origin. Browser-delivered
configuration is public and must never contain credentials or secrets.

### Local Authentik Sign-In

For manual sign-in testing at `https://localhost:7241`, add the exact **Strict**
redirect URI `https://localhost:7241/signin-oidc` to the confidential Authentik
provider. Keep any production redirect URI as a separate entry. Use the
per-provider issuer mode described in [Self-Hosted Deployment](deployment.md).
Local HTTPS needs a trusted development certificate.

The ignored root `.env.local.ps1` can set process-scoped host configuration
using the existing `Authentication:*` settings and file-based secrets. It is
local operator configuration, not a checked-in template or automatic startup
hook. Before using a prepared local copy:

- Set the exact owner OIDC `sub` in the script. A username or email is not a
  substitute. Match its client ID and issuer to the Authentik provider.
- Replace `secrets\local-auth\oidc-client-secret` with only the client secret.
- Edit `secrets\local-auth\connection-string` with the complete Npgsql connection
  for a dedicated manual-test database. The placeholder does not create a
  PostgreSQL service or database; use an existing local test server.
- Restrict access to the ignored secret directory, which also holds local
  Data Protection keys. Do not paste these files into chat or command output.

Run from the repository root in a dedicated PowerShell terminal:

```powershell
& .\.env.local.ps1
dotnet run --project src\HouseholdLedger.Api --no-launch-profile
```

If that dedicated database needs schema updates, run the same command with
`-- --migrate-database` first. Review the database target before migrating;
normal startup never migrates. Then open `https://localhost:7241/auth/login`.
This is the API-hosted WASM client; no separate frontend host is needed.
Close the terminal afterward to discard its process-scoped configuration.
Do not use this terminal or database for automated mutation tests.

Development still enforces authentication when the authority is configured.
This setup does not change DNS, production proxy settings, or Authentik
configuration, and does not automatically start PostgreSQL.

## Publish the Hosted Application

Publish the API project to produce the deployable application:

```powershell
dotnet publish src/HouseholdLedger.Api --no-restore --output artifacts/publish/api
```

The API output is an ASP.NET Core application that includes and serves the
referenced Blazor WebAssembly Client. A replacement frontend may still consume
the language-neutral HTTP/OpenAPI contract without referencing server
implementation assemblies.

Production now requires HTTPS OIDC issuer, confidential client, and exact owner
subject configuration. Follow [Self-Hosted Deployment](deployment.md) for
Authentik, secret files, TLS forwarding, migrations, and persistent keys.
Do not start a published Production host with missing authentication settings.
For an unauthenticated **loopback-only development smoke**, run from the output
directory so the process uses the deployed `wwwroot` as its content root:

```powershell
Push-Location artifacts/publish/api
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "https://localhost:7241"
dotnet HouseholdLedger.Api.dll
Pop-Location
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
- [Self-Hosted Deployment](deployment.md)
- [Contribution Guide](contributing.md)
- [Troubleshooting](troubleshooting.md)
- [Architecture Overview](../architecture/overview.md)
