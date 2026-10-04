[CmdletBinding()]
param(
    [string] $TestFilter = "FullyQualifiedName~FreshSetupPersistsAccountsAndExpectationsAndReviewsOnlyExplicitlyConfirmedIncome"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$containerName = "householdledger-fresh-browser-$PID-$([Guid]::NewGuid().ToString('N'))"
$password = [Guid]::NewGuid().ToString("N")
$database = "fresh_browser"
$previousConnection = $env:ConnectionStrings__HouseholdLedger
$previousMarker = $env:HOUSEHOLDLEDGER_FRESH_SETUP_DATABASE
$clock = [Diagnostics.Stopwatch]::StartNew()

try {
    # No application connection string or User Secrets are read. Bind only a dynamically allocated loopback port.
    & docker run --detach --name $containerName --publish "127.0.0.1::5432" `
        --env "POSTGRES_DB=$database" --env "POSTGRES_USER=fresh_browser" `
        --env "POSTGRES_PASSWORD=$password" postgres:18
    if ($LASTEXITCODE -ne 0) { throw "Could not start disposable PostgreSQL 18." }

    $readyClock = [Diagnostics.Stopwatch]::StartNew()
    do {
        & docker exec $containerName pg_isready --username fresh_browser --dbname $database
        if ($LASTEXITCODE -eq 0) { break }
        if ($readyClock.Elapsed.TotalSeconds -gt 60) { throw "Disposable PostgreSQL did not become ready." }
        Start-Sleep -Milliseconds 200
    } while ($true)

    $binding = (& docker port $containerName "5432/tcp").Trim()
    if ($LASTEXITCODE -ne 0 -or $binding -notmatch '^127\.0\.0\.1:(\d+)$') {
        throw "Expected an isolated loopback PostgreSQL binding, got '$binding'."
    }
    $port = $Matches[1]
    $env:ConnectionStrings__HouseholdLedger = "Host=127.0.0.1;Port=$port;Database=$database;Username=fresh_browser;Password=$password"
    $env:HOUSEHOLDLEDGER_FRESH_SETUP_DATABASE = "postgres18-disposable"
    $infrastructure = Join-Path $PSScriptRoot "..\..\src\HouseholdLedger.Infrastructure"
    & dotnet build $infrastructure --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Infrastructure build failed." }
    & dotnet ef database update --project $infrastructure --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw "Disposable database migration failed." }

    $secureConnection = ConvertTo-SecureString $env:ConnectionStrings__HouseholdLedger -AsPlainText -Force
    & (Join-Path $PSScriptRoot "Run-BrowserEndToEndTests.ps1") `
        -PiDbConnectionString $secureConnection `
        -TestFilter $TestFilter
    Write-Host "Fresh setup browser regression passed in $([Math]::Round($clock.Elapsed.TotalSeconds, 2)) seconds; container=$containerName PostgreSQL=127.0.0.1:$port."
}
finally {
    $env:ConnectionStrings__HouseholdLedger = $previousConnection
    $env:HOUSEHOLDLEDGER_FRESH_SETUP_DATABASE = $previousMarker
    & docker rm --force --volumes $containerName
    if ($LASTEXITCODE -ne 0) { Write-Warning "Verify cleanup of owned container $containerName." }
    Remove-Variable password, secureConnection -ErrorAction SilentlyContinue
}
