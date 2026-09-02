[CmdletBinding()]
param(
    [string] $PostgresImage = "docker.io/library/postgres:18",

    [switch] $RunTransactionBrowserJourney
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$projectPath = Join-Path $PSScriptRoot "HouseholdLedger.Infrastructure.IntegrationTests.csproj"
$runId = "{0}_{1}" -f $PID, ([Guid]::NewGuid().ToString("N").Substring(0, 12))
$containerName = "householdledger-pg-$runId"
$databaseName = "householdledger_$runId"
$databaseUser = "householdledger_test"
$databasePassword = [Guid]::NewGuid().ToString("N")
$connectionVariable = "HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING"
$publishRoot = $null

if ($null -eq (Get-Command podman -ErrorAction SilentlyContinue)) {
    throw "Podman is required to run the PostgreSQL integration tests."
}

$listener = [System.Net.Sockets.TcpListener]::new(
    [System.Net.IPAddress]::Loopback,
    0)
$listener.Start()
$hostPort = ([System.Net.IPEndPoint] $listener.LocalEndpoint).Port
$listener.Stop()

try {
    Write-Output "PostgreSQL allocation: image=$PostgresImage container=$containerName database=$databaseName port=$hostPort password=<redacted>"
    & podman run --detach `
        --name $containerName `
        --publish "127.0.0.1:${hostPort}:5432" `
        --env "POSTGRES_DB=$databaseName" `
        --env "POSTGRES_USER=$databaseUser" `
        --env "POSTGRES_PASSWORD=$databasePassword" `
        $PostgresImage | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Podman failed to start the owned PostgreSQL container."
    }

    $ready = $false
    for ($attempt = 1; $attempt -le 60; $attempt++) {
        & podman exec $containerName pg_isready --username $databaseUser --dbname $databaseName 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $ready = $true
            break
        }

        Start-Sleep -Seconds 1
    }

    if (-not $ready) {
        throw "The owned PostgreSQL container was not ready within 60 seconds."
    }

    [Environment]::SetEnvironmentVariable(
        $connectionVariable,
        "Host=127.0.0.1;Port=$hostPort;Database=$databaseName;Username=$databaseUser;Password=$databasePassword;Pooling=false",
        "Process")

    & dotnet test $projectPath --no-restore --nologo --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "The PostgreSQL integration tests failed."
    }

    if ($RunTransactionBrowserJourney) {
        $publishRoot = Join-Path ([System.IO.Path]::GetTempPath()) "HouseholdLedger-BrowserE2E-$runId"
        $apiOutput = Join-Path $publishRoot "api"
        $profileRoot = Join-Path $publishRoot "profiles"
        $outputRoot = Join-Path $publishRoot "output"
        New-Item -ItemType Directory -Path $profileRoot, $outputRoot -Force | Out-Null

        & dotnet publish (Join-Path $PSScriptRoot "..\..\src\HouseholdLedger.Api") `
            --configuration Release `
            --no-restore `
            --output $apiOutput
        if ($LASTEXITCODE -ne 0) {
            throw "The API publish for the browser journey failed."
        }

        $apiListener = [System.Net.Sockets.TcpListener]::new(
            [System.Net.IPAddress]::Loopback,
            0)
        $apiListener.Start()
        $apiPort = ([System.Net.IPEndPoint] $apiListener.LocalEndpoint).Port
        $apiListener.Stop()

        $browserRuntime = Join-Path $env:LOCALAPPDATA "HouseholdLedger\BrowserTestRuntime"
        $env:HOUSEHOLDLEDGER_API_ARTIFACT = Join-Path $apiOutput "HouseholdLedger.Api.dll"
        $env:HOUSEHOLDLEDGER_FIREFOX_BINARY = Join-Path $browserRuntime "firefox\153.0.1-eme-free\core\firefox.exe"
        $env:HOUSEHOLDLEDGER_GECKODRIVER = Join-Path $browserRuntime "geckodriver\0.37.1\geckodriver.exe"
        $env:HOUSEHOLDLEDGER_E2E_PROFILE_ROOT = $profileRoot
        $env:HOUSEHOLDLEDGER_E2E_OUTPUT_DIR = $outputRoot
        $env:HOUSEHOLDLEDGER_E2E_API_PORT = $apiPort.ToString([System.Globalization.CultureInfo]::InvariantCulture)

        & dotnet test (Join-Path $PSScriptRoot "..\HouseholdLedger.EndToEndTests") `
            --configuration Release `
            --no-restore `
            --filter "FullyQualifiedName~BrowserCalendarJourneyTests"
        if ($LASTEXITCODE -ne 0) {
            throw "The hosted transaction browser journey failed."
        }
    }
}
finally {
    [Environment]::SetEnvironmentVariable($connectionVariable, $null, "Process")
    foreach ($variable in @(
        "HOUSEHOLDLEDGER_API_ARTIFACT",
        "HOUSEHOLDLEDGER_FIREFOX_BINARY",
        "HOUSEHOLDLEDGER_GECKODRIVER",
        "HOUSEHOLDLEDGER_E2E_PROFILE_ROOT",
        "HOUSEHOLDLEDGER_E2E_OUTPUT_DIR",
        "HOUSEHOLDLEDGER_E2E_API_PORT")) {
        [Environment]::SetEnvironmentVariable($variable, $null, "Process")
    }

    if ($null -ne $publishRoot) {
        Remove-Item $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    & podman container exists $containerName
    if ($LASTEXITCODE -eq 0) {
        & podman rm --force $containerName | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to remove owned container $containerName."
        }
        else {
            Write-Output "PostgreSQL cleanup: removed container=$containerName"
        }
    }

    & podman container exists $containerName
    if ($LASTEXITCODE -eq 0) {
        Write-Error "Owned container $containerName is still present after cleanup."
    }
}
