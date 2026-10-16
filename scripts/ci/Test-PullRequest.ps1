#Requires -Version 7.0
[CmdletBinding()]
param()

throw 'Intentional branch-protection verification failure. Do not merge this PR.'

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot)
Push-Location $root
try {
    dotnet restore HouseholdLedger.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { throw "Locked restore failed." }
    dotnet build HouseholdLedger.slnx --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
    $projects = @(
        "HouseholdLedger.Domain.UnitTests",
        "HouseholdLedger.Application.UnitTests",
        "HouseholdLedger.Api.Contracts.Tests",
        "HouseholdLedger.Client.ComponentTests",
        "HouseholdLedger.Api.IntegrationTests"
    )
    foreach ($project in $projects) {
        dotnet test (Join-Path "tests" $project) --configuration Release --no-build --no-restore --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw "$project failed." }
    }
    & (Join-Path $root "scripts\openapi\Sync-OpenApi.ps1") -Configuration Release
    if ($LASTEXITCODE -ne 0) { throw "OpenAPI comparison failed." }
}
finally {
    Pop-Location
}
