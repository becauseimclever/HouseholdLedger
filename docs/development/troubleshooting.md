# Troubleshooting

## The Locked Restore Reports NU1004

`NU1004` means a committed `packages.lock.json` no longer matches its project
dependencies. The current committed lock files passed locked restore on
2026-08-02. If this error appears after a dependency or project-reference
change, the implementation owner must review that change and intentionally
regenerate only the affected lock files. Do not disable locked mode as a routine
workaround, and do not hand-edit lock files. After regeneration, inspect the
dependency diff and rerun:

```powershell
dotnet restore HouseholdLedger.slnx --locked-mode
dotnet build HouseholdLedger.slnx --no-restore
```

## HTTPS Is Not Trusted

Check and trust the local development certificate:

```powershell
dotnet dev-certs https --check --trust
```

If organizational policy prevents trust changes, use an approved local HTTPS
method and update both API/client origins and CORS together. Do not bypass
certificate validation in application code.

## The Client Cannot Reach the API

Confirm all three values agree:

1. The API is listening at the Client's `Api:BaseUrl`.
2. The browser opened the Client at the origin listed in the API's
   `Cors:AllowedOrigins` configuration.
3. The API health endpoint responds directly.

With the documented defaults, test the endpoint:

```powershell
Invoke-RestMethod https://localhost:7241/api/v1/health
```

A direct health response with a browser CORS failure usually means the Client's
origin was not configured exactly, including scheme and port.

## Podman Tests Do Not Start

Podman 5.8.3 is installed in the evidenced environment, although its executable
may not yet be available on every existing terminal's `PATH`. Confirm the
installation and machine after opening a refreshed terminal:

```powershell
podman version
podman info
```

Run the repository harness rather than invoking the test project directly. The
harness owns port allocation, credentials, environment setup, and cleanup. If a
run is interrupted, use `podman ps --all` to identify a container whose name
starts with `householdledger-pg-`; verify it belongs to your failed run before
removing it.

The retained Feature 001 closure validation used an isolated Docker 29.6.2
container and passed the real PostgreSQL test. The container, database, port,
and generated credentials were removed after the run. Use the repository
harness for future runs; it remains responsible for allocating and cleaning
those resources.

## Browser E2E Rejects Its Inputs

Use the self-cleaning fresh-publish workflow in [Testing](testing.md). The
complete run requires six normalized absolute paths and two distinct, available
loopback ports:

- `HOUSEHOLDLEDGER_API_ARTIFACT` must name an existing file named exactly
   `HouseholdLedger.Api.dll`.
- `HOUSEHOLDLEDGER_CLIENT_PUBLISH_DIR` must name the published Client
   `wwwroot`, including `index.html`, `appsettings.json`, and
   `_framework/blazor.webassembly.js`.
- `HOUSEHOLDLEDGER_FIREFOX_BINARY` must name the approved Firefox 153.0.1
   EME-free `firefox.exe`.
- `HOUSEHOLDLEDGER_GECKODRIVER` must name the approved geckodriver 0.37.1
   `geckodriver.exe`.
- `HOUSEHOLDLEDGER_E2E_PROFILE_ROOT` and `HOUSEHOLDLEDGER_E2E_OUTPUT_DIR` must
   name existing, test-owned directories. They must not be shared with another
   run.
- `HOUSEHOLDLEDGER_E2E_API_PORT` and `HOUSEHOLDLEDGER_E2E_CLIENT_PORT` must be
   different, currently available loopback ports.

Relative paths, unresolved `.` or `..` segments, stale publish directories,
different executable names, version mismatches, and executable SHA-256
mismatches are rejected before browser launch. Do not add
Selenium, Playwright, a browser manager, or a runtime downloader as a
workaround. See the
[Browser E2E Dependency Review](browser-e2e-dependency-review.md).

## A Browser E2E Run Leaves Local Output

Screenshots are written beneath the caller-supplied
`HOUSEHOLDLEDGER_E2E_OUTPUT_DIR`. The test must remove its temporary Firefox
profile and release its API, Client, and geckodriver ports. The outer workflow
must remove all eight process-scoped environment variables and its fresh
publish/profile/output root even after failure.

If cleanup reports a failure, preserve the test output for diagnosis before
rerunning. Do not terminate or delete a process, profile, or temporary root
until its ownership by the failed run is established.

## The HTTP System Test Cannot Find the API

The system test does not search build or publish directories. Follow the
self-cleaning PowerShell 7 workflow in [Testing](testing.md) to publish the API
to an explicit temporary output and set `HOUSEHOLDLEDGER_API_ARTIFACT` for the
test command.

The value must be a normalized absolute path to an existing file named exactly
`HouseholdLedger.Api.dll`. A relative path, a directory, a path containing
unresolved `.` or `..` segments, or an artifact from an unspecified prior build
is rejected. Clear the variable and remove the temporary publish output in a
`finally` block even when the test fails.
