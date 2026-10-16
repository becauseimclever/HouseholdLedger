#Requires -Version 7.0
[CmdletBinding()]
param(
    [string] $Image = "householdledger:baseline",
    [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot)
$id = "hl-baseline-$([Guid]::NewGuid().ToString('N').Substring(0, 12))"
$database = "$id-db"
$application = "$id-app"
$migration = "$id-migrate"
$seed = "$id-seed"
$volume = "$id-data"
$keyVolume = "$id-keys"
$temporary = Join-Path ([IO.Path]::GetTempPath()) $id
$null = New-Item -ItemType Directory -Path $temporary
$password = [Guid]::NewGuid().ToString("N")
$databaseEnvironment = Join-Path $temporary "database.env"
$applicationEnvironment = Join-Path $temporary "application.env"
$client = [Net.Http.HttpClient]::new([Net.Http.HttpClientHandler]@{ AllowAutoRedirect = $false })
$client.DefaultRequestHeaders.Host = "ledger.example"

function Invoke-Docker {
    param([string[]] $Arguments)
    $result = & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Owned container operation failed: $($Arguments[0])." }
    return $result
}

function Wait-Status {
    param([string] $Url, [int] $Status)
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            $response = $client.GetAsync($Url).GetAwaiter().GetResult()
            try { if ([int]$response.StatusCode -eq $Status) { return } }
            finally { $response.Dispose() }
        }
        catch [Net.Http.HttpRequestException] {
            if ($attempt -eq 59) { throw "Owned application did not respond." }
        }
        Start-Sleep -Seconds 1
    }
    throw "Expected HTTP $Status was not observed at $Url."
}

function Start-OwnedApplication {
    Invoke-Docker -Arguments @(
        "run", "--detach", "--name", $application, "--network", $id,
        "--env-file", $applicationEnvironment, "--publish", "127.0.0.1::8080",
        "--volume", "${keyVolume}:/var/lib/householdledger/keys", $Image
    ) | Out-Null
    $binding = (Invoke-Docker -Arguments @("port", $application, "8080/tcp")).Trim()
    return "http://$binding"
}

try {
    if (-not $SkipBuild) {
        Invoke-Docker -Arguments @("build", "--tag", $Image, $root) | Out-Null
    }
    # Only generated disposable credentials are written; never use a manual-development database.
    [IO.File]::WriteAllLines($databaseEnvironment, @(
        "POSTGRES_DB=householdledger",
        "POSTGRES_USER=householdledger_test",
        "POSTGRES_PASSWORD=$password"
    ))
    [IO.File]::WriteAllLines($applicationEnvironment, @(
        "ASPNETCORE_ENVIRONMENT=Production",
        "AllowedHosts=ledger.example",
        "ConnectionStrings__HouseholdLedger=Host=$database;Database=householdledger;Username=householdledger_test;Password=$password",
        "Authentication__Authority=https://identity.example/application/o/ledger/",
        "Authentication__ClientId=ledger",
        "Authentication__ClientSecret=disposable-protocol-placeholder",
        "Authentication__OwnerSubject=owner"
    ))
    Invoke-Docker -Arguments @("network", "create", $id) | Out-Null
    Invoke-Docker -Arguments @("volume", "create", $volume) | Out-Null
    Invoke-Docker -Arguments @("volume", "create", $keyVolume) | Out-Null
    Invoke-Docker -Arguments @(
        "run", "--detach", "--name", $database, "--network", $id,
        "--env-file", $databaseEnvironment, "--volume", "${volume}:/var/lib/postgresql", "postgres:18"
    ) | Out-Null
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        & docker exec $database pg_isready -U householdledger_test -d householdledger 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw "Owned PostgreSQL was not ready." }

    $origin = Start-OwnedApplication
    Wait-Status "$origin/api/v1/health" 200
    Wait-Status "$origin/health/ready" 503
    Wait-Status "$origin/api/v1/accounts" 401
    Invoke-Docker -Arguments @(
        "run", "--rm", "--name", $migration, "--network", $id,
        "--env-file", $applicationEnvironment, $Image, "--migrate-database"
    ) | Out-Null
    Wait-Status "$origin/health/ready" 200
    # Populate only the owned test database to prove record survival across replacement.
    Invoke-Docker -Arguments @(
        "run", "--rm", "--name", $seed, "--network", $id,
        "--env-file", $applicationEnvironment, "--env", "ASPNETCORE_ENVIRONMENT=Development",
        $Image, "--initialize-development-database"
    ) | Out-Null
    $count = (Invoke-Docker -Arguments @(
        "exec", $database, "psql", "-U", "householdledger_test", "-d", "householdledger",
        "-tAc", "SELECT count(*) FROM expense_transactions"
    )).Trim()
    if ($count -ne "7") { throw "Owned fixture did not contain the expected transactions." }

    Invoke-Docker -Arguments @("rm", "--force", $application) | Out-Null
    Invoke-Docker -Arguments @("rm", "--force", $database) | Out-Null
    Invoke-Docker -Arguments @(
        "run", "--detach", "--name", $database, "--network", $id,
        "--env-file", $databaseEnvironment, "--volume", "${volume}:/var/lib/postgresql", "postgres:18"
    ) | Out-Null
    $origin = Start-OwnedApplication
    Wait-Status "$origin/health/ready" 200
    $count = (Invoke-Docker -Arguments @(
        "exec", $database, "psql", "-U", "householdledger_test", "-d", "householdledger",
        "-tAc", "SELECT count(*) FROM expense_transactions"
    )).Trim()
    if ($count -ne "7") { throw "Records did not survive container replacement." }
    Invoke-Docker -Arguments @("exec", $application, "dotnet", "HouseholdLedger.Api.dll", "--check-readiness") | Out-Null
    Invoke-Docker -Arguments @(
        "exec", $database, "pg_dump", "-U", "householdledger_test", "-d", "householdledger",
        "--format=custom", "--file=/tmp/ledger-backup.dump"
    ) | Out-Null
    Invoke-Docker -Arguments @("exec", $database, "createdb", "-U", "householdledger_test", "householdledger_restored") | Out-Null
    Invoke-Docker -Arguments @(
        "exec", $database, "pg_restore", "-U", "householdledger_test", "-d", "householdledger_restored",
        "--exit-on-error", "/tmp/ledger-backup.dump"
    ) | Out-Null
    $restoredCount = (Invoke-Docker -Arguments @(
        "exec", $database, "psql", "-U", "householdledger_test", "-d", "householdledger_restored",
        "-tAc", "SELECT count(*) FROM expense_transactions"
    )).Trim()
    if ($restoredCount -ne "7") { throw "Backup restore did not recover the recorded transactions." }
    Invoke-Docker -Arguments @("rm", "--force", $application) | Out-Null
    $restoredEnvironment = [IO.File]::ReadAllText($applicationEnvironment).Replace(
        "Database=householdledger;", "Database=householdledger_restored;")
    [IO.File]::WriteAllText($applicationEnvironment, $restoredEnvironment)
    $origin = Start-OwnedApplication
    Wait-Status "$origin/health/ready" 200
    Wait-Status "$origin/api/v1/accounts" 401
    Write-Output "PASS: explicit migration, readiness, owner gate, 7 records after app/database replacement, and PostgreSQL backup/restore with restored-host readiness."
    Write-Output "Not covered: real Authentik login, browser workflows, or Pi hardware/resource qualification."
}
finally {
    $client.Dispose()
    $cleanupFailures = [Collections.Generic.List[string]]::new()
    foreach ($name in @($application, $database, $migration, $seed)) {
        $exists = & docker ps --all --filter "name=^/$name$" --format "{{.Names}}"
        if ($LASTEXITCODE -ne 0) { $cleanupFailures.Add("inspect $name"); continue }
        if ($exists -eq $name) {
            & docker rm --force $name | Out-Null
            if ($LASTEXITCODE -ne 0) { $cleanupFailures.Add("remove $name") }
        }
    }
    foreach ($name in @($volume, $keyVolume)) {
        $exists = & docker volume ls --filter "name=^${name}$" --format "{{.Name}}"
        if ($LASTEXITCODE -ne 0) { $cleanupFailures.Add("inspect $name"); continue }
        if ($exists -eq $name) {
            & docker volume rm $name | Out-Null
            if ($LASTEXITCODE -ne 0) { $cleanupFailures.Add("remove $name") }
        }
    }
    $exists = & docker network ls --filter "name=^${id}$" --format "{{.Name}}"
    if ($LASTEXITCODE -ne 0) { $cleanupFailures.Add("inspect network") }
    elseif ($exists -eq $id) {
        & docker network rm $id | Out-Null
        if ($LASTEXITCODE -ne 0) { $cleanupFailures.Add("remove network") }
    }
    foreach ($file in @($databaseEnvironment, $applicationEnvironment)) {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force }
    }
    Remove-Item -LiteralPath $temporary
    if ($cleanupFailures.Count -gt 0) { throw "Cleanup failed: $($cleanupFailures -join ', ')." }
}
