[CmdletBinding()]
param(
    [string] $TestFilter = "FullyQualifiedName~BrowserCalendarJourneyTests",

    [System.Security.SecureString] $PiDbConnectionString
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$publishRoot = Join-Path ([System.IO.Path]::GetTempPath()) "HouseholdLedger-BrowserE2E-$PID-$([Guid]::NewGuid().ToString("N"))"
$apiOutput = Join-Path $publishRoot "api"
$profileRoot = Join-Path $publishRoot "profiles"
$outputRoot = Join-Path $publishRoot "output"
$plainTextConnectionString = $null

if ($null -eq $PiDbConnectionString) {
    $PiDbConnectionString = Read-Host "PiDB PostgreSQL connection string" -AsSecureString
}

try {
    $credential = [System.Management.Automation.PSCredential]::new("unused", $PiDbConnectionString)
    $plainTextConnectionString = $credential.GetNetworkCredential().Password
    if ([string]::IsNullOrWhiteSpace($plainTextConnectionString)) {
        throw "A PiDB PostgreSQL connection string is required."
    }

    New-Item -ItemType Directory -Path $profileRoot, $outputRoot -Force | Out-Null
    & dotnet publish (Join-Path $PSScriptRoot "..\..\src\HouseholdLedger.Api") `
        --configuration Release `
        --no-restore `
        --output $apiOutput
    if ($LASTEXITCODE -ne 0) {
        throw "The API publish for browser E2E failed."
    }

    $listener = [System.Net.Sockets.TcpListener]::new(
        [System.Net.IPAddress]::Loopback,
        0)
    $listener.Start()
    $apiPort = ([System.Net.IPEndPoint] $listener.LocalEndpoint).Port
    $listener.Stop()

    $browserRuntime = Join-Path $env:LOCALAPPDATA "HouseholdLedger\BrowserTestRuntime"
    $env:HOUSEHOLDLEDGER_API_ARTIFACT = Join-Path $apiOutput "HouseholdLedger.Api.dll"
    $env:HOUSEHOLDLEDGER_FIREFOX_BINARY = Join-Path $browserRuntime "firefox\153.0.1-eme-free\core\firefox.exe"
    $env:HOUSEHOLDLEDGER_GECKODRIVER = Join-Path $browserRuntime "geckodriver\0.37.1\geckodriver.exe"
    $env:HOUSEHOLDLEDGER_E2E_PROFILE_ROOT = $profileRoot
    $env:HOUSEHOLDLEDGER_E2E_OUTPUT_DIR = $outputRoot
    $env:HOUSEHOLDLEDGER_E2E_API_PORT = $apiPort.ToString([System.Globalization.CultureInfo]::InvariantCulture)
    $env:HOUSEHOLDLEDGER_E2E_POSTGRES_CONNECTION_STRING = $plainTextConnectionString

    & dotnet test (Join-Path $PSScriptRoot "HouseholdLedger.EndToEndTests.csproj") `
        --configuration Release `
        --no-restore `
        --filter $TestFilter
    if ($LASTEXITCODE -ne 0) {
        throw "The browser E2E tests failed."
    }
}
finally {
    foreach ($variable in @(
        "HOUSEHOLDLEDGER_API_ARTIFACT",
        "HOUSEHOLDLEDGER_FIREFOX_BINARY",
        "HOUSEHOLDLEDGER_GECKODRIVER",
        "HOUSEHOLDLEDGER_E2E_PROFILE_ROOT",
        "HOUSEHOLDLEDGER_E2E_OUTPUT_DIR",
        "HOUSEHOLDLEDGER_E2E_API_PORT",
        "HOUSEHOLDLEDGER_E2E_POSTGRES_CONNECTION_STRING")) {
        [Environment]::SetEnvironmentVariable($variable, $null, "Process")
    }

    Remove-Variable plainTextConnectionString -ErrorAction SilentlyContinue
    Remove-Item $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
}
