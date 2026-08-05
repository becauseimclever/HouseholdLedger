#Requires -Version 7.0

<#
.SYNOPSIS
Publishes a Release API package with a verified standalone Client artifact at its web root.

.DESCRIPTION
The default invocation performs locked restores, independently publishes the Client into
an isolated staging directory, verifies its declared manifest, publishes the API, and
copies the verified Client wwwroot contents into the API package root. It never adds a
CLR reference from API to Client and does not affect ordinary API build, test, publish,
or startup commands.

Both output paths must not already exist. This prevents an invocation from deleting or
overwriting unrelated artifacts. Use -ClientPublishDirectory only to verify a separately
published artifact, such as a controlled failure test.

.EXAMPLE
pwsh ./scripts/Compose-HostedClientPackage.ps1 `
  -OutputDirectory ./artifacts/feature-001-compose/package `
  -WorkingDirectory ./artifacts/feature-001-compose/work
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputDirectory,

    [string]$WorkingDirectory,

    [string]$ClientPublishDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$apiProjectPath = Join-Path $repositoryRoot 'src\HouseholdLedger.Api\HouseholdLedger.Api.csproj'
$clientProjectPath = Join-Path $repositoryRoot 'src\HouseholdLedger.Client\HouseholdLedger.Client.csproj'
$expectedContractVersion = '1'
$expectedArtifactIdentity = 'household-ledger-client@1.0.0'
$clientManifestFileName = 'household-ledger-client.manifest'
$compositionManifestFileName = 'household-ledger-client.composition.manifest'
$packageStagingDirectory = $null
$createdWorkingDirectory = $false

function Resolve-RepositoryPath {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    return [System.IO.Path]::GetFullPath($Path, $repositoryRoot)
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-Sha256 {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    return [System.Convert]::ToHexString(
        [System.Security.Cryptography.SHA256]::HashData([System.IO.File]::ReadAllBytes($Path)))
}

function ConvertTo-SafeRelativePath {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path) -or $Path.IndexOf([char]0) -ge 0) {
        throw "Artifact manifest path is not relative: $Path"
    }

    $segments = $Path -split '[\\/]'
    if ($segments.Count -eq 0 -or $segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -in @('.', '..') }) {
        throw "Artifact manifest path is unsafe: $Path"
    }

    return ($segments -join [System.IO.Path]::DirectorySeparatorChar)
}

function Assert-ChildPath {
    param(
        [Parameter(Mandatory)]
        [string]$Root,

        [Parameter(Mandatory)]
        [string]$RelativePath
    )

    $rootWithSeparator = $Root.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $candidatePath = [System.IO.Path]::GetFullPath((Join-Path $Root $RelativePath))
    if (-not $candidatePath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Artifact manifest path escapes its content root: $RelativePath"
    }

    return $candidatePath
}

function Get-VerifiedClientArtifact {
    param(
        [Parameter(Mandatory)]
        [string]$PublishDirectory
    )

    $contentRoot = Join-Path $PublishDirectory 'wwwroot'
    $manifestPath = Join-Path $contentRoot $clientManifestFileName
    if (-not (Test-Path -LiteralPath $contentRoot -PathType Container)) {
        throw "Client publish content root was not found: $contentRoot"
    }

    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Client artifact manifest was not found: $manifestPath"
    }

    $manifestLines = [System.IO.File]::ReadAllText($manifestPath).Replace("`r`n", "`n").Split("`n")
    while ($manifestLines.Count -gt 0 -and $manifestLines[$manifestLines.Count - 1].Length -eq 0) {
        $manifestLines = $manifestLines[0..($manifestLines.Count - 2)]
    }

    $requiredHeader = @(
        "contract-version=$expectedContractVersion",
        "artifact-identity=$expectedArtifactIdentity",
        'content-root=wwwroot',
        'entry-point=index.html',
        'file-sha256')
    if ($manifestLines.Count -le $requiredHeader.Count) {
        throw 'Client artifact manifest does not declare an asset inventory.'
    }

    for ($index = 0; $index -lt $requiredHeader.Count; $index++) {
        if (-not [string]::Equals($manifestLines[$index], $requiredHeader[$index], [System.StringComparison]::Ordinal)) {
            throw "Client artifact manifest header is invalid at line $($index + 1)."
        }
    }

    $inventory = [System.Collections.Generic.List[object]]::new()
    $knownPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    for ($index = $requiredHeader.Count; $index -lt $manifestLines.Count; $index++) {
        $parts = $manifestLines[$index].Split('|')
        if ($parts.Count -ne 2 -or $parts[1] -notmatch '^[A-Fa-f0-9]{64}$') {
            throw "Client artifact manifest inventory is malformed at line $($index + 1)."
        }

        $relativePath = ConvertTo-SafeRelativePath -Path $parts[0]
        if (-not $knownPaths.Add($relativePath)) {
            throw "Client artifact manifest declares duplicate path: $($parts[0])"
        }

        $sourcePath = Assert-ChildPath -Root $contentRoot -RelativePath $relativePath
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw "Client artifact inventory file was not found: $($parts[0])"
        }

        $actualHash = Get-Sha256 -Path $sourcePath
        if (-not [string]::Equals($actualHash, $parts[1], [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Client artifact hash mismatch: $($parts[0])"
        }

        $inventory.Add([PSCustomObject]@{
                RelativePath = $relativePath
                Sha256 = $actualHash
                SourcePath = $sourcePath
            })
    }

    if (-not $knownPaths.Contains('index.html')) {
        throw 'Client artifact inventory does not include the declared entry point: index.html'
    }

    return [PSCustomObject]@{
        ContentRoot = $contentRoot
        ManifestPath = $manifestPath
        ManifestSha256 = Get-Sha256 -Path $manifestPath
        Inventory = $inventory
    }
}

if (-not (Test-Path -LiteralPath $apiProjectPath -PathType Leaf) -or -not (Test-Path -LiteralPath $clientProjectPath -PathType Leaf)) {
    throw 'API or Client project was not found beneath the repository root.'
}

$apiProjectXml = [xml](Get-Content -LiteralPath $apiProjectPath -Raw)
if ($apiProjectXml.Project.ItemGroup.ProjectReference | Where-Object { $_.Include -match '(^|[\\/])HouseholdLedger\.Client([\\/]|$)' }) {
    throw 'API project must not reference HouseholdLedger.Client.'
}

$resolvedOutputDirectory = Resolve-RepositoryPath -Path $OutputDirectory
if (Test-Path -LiteralPath $resolvedOutputDirectory) {
    throw "OutputDirectory must not already exist: $resolvedOutputDirectory"
}

if ([string]::IsNullOrWhiteSpace($ClientPublishDirectory)) {
    if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        throw 'WorkingDirectory is required when ClientPublishDirectory is not supplied.'
    }

    $resolvedWorkingDirectory = Resolve-RepositoryPath -Path $WorkingDirectory
    if (Test-Path -LiteralPath $resolvedWorkingDirectory) {
        throw "WorkingDirectory must not already exist: $resolvedWorkingDirectory"
    }

    [System.IO.Directory]::CreateDirectory($resolvedWorkingDirectory) | Out-Null
    $createdWorkingDirectory = $true
    $resolvedClientPublishDirectory = Join-Path $resolvedWorkingDirectory 'client-publish'

    Invoke-DotNet -Arguments @('restore', $clientProjectPath, '--locked-mode', '--nologo')
    Invoke-DotNet -Arguments @('publish', $clientProjectPath, '--configuration', 'Release', '--no-restore', '--nologo', '--output', $resolvedClientPublishDirectory)
}
else {
    $resolvedClientPublishDirectory = Resolve-RepositoryPath -Path $ClientPublishDirectory
}

try {
    $clientArtifact = Get-VerifiedClientArtifact -PublishDirectory $resolvedClientPublishDirectory

    Invoke-DotNet -Arguments @('restore', $apiProjectPath, '--locked-mode', '--nologo')
    $packageStagingDirectory = "$resolvedOutputDirectory.staging-$([Guid]::NewGuid().ToString('N'))"
    Invoke-DotNet -Arguments @('publish', $apiProjectPath, '--configuration', 'Release', '--no-restore', '--nologo', '--output', $packageStagingDirectory)

    if (Test-Path -LiteralPath (Join-Path $packageStagingDirectory 'wwwroot')) {
        throw 'API publish output unexpectedly contains a nested wwwroot directory.'
    }

    foreach ($asset in $clientArtifact.Inventory) {
        $destinationPath = Assert-ChildPath -Root $packageStagingDirectory -RelativePath $asset.RelativePath
        if (Test-Path -LiteralPath $destinationPath) {
            if (-not [string]::Equals($asset.RelativePath, 'appsettings.json', [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Client artifact would overwrite API publish output: $($asset.RelativePath)"
            }
        }

        [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($destinationPath)) | Out-Null
        [System.IO.File]::Copy($asset.SourcePath, $destinationPath, $true)
    }

    $copiedClientManifestPath = Join-Path $packageStagingDirectory $clientManifestFileName
    if (Test-Path -LiteralPath $copiedClientManifestPath) {
        throw "Client artifact manifest would overwrite API publish output: $clientManifestFileName"
    }

    [System.IO.File]::Copy($clientArtifact.ManifestPath, $copiedClientManifestPath, $false)
    $compositionManifestPath = Join-Path $packageStagingDirectory $compositionManifestFileName
    $compositionManifestLines = @(
        'composition-contract-version=1',
        'composition-command-version=1',
        "client-artifact-identity=$expectedArtifactIdentity",
        "client-contract-version=$expectedContractVersion",
        "client-manifest-path=$clientManifestFileName",
        "client-manifest-sha256=$($clientArtifact.ManifestSha256)",
        'client-content-root=wwwroot',
        'client-entry-point=index.html',
        'destination=/',
        'file-sha256')
    $compositionManifestLines += $clientArtifact.Inventory | ForEach-Object {
        "$(($_.RelativePath -replace '\\', '/'))|$($_.Sha256)"
    }
    [System.IO.File]::WriteAllLines(
        $compositionManifestPath,
        [string[]]$compositionManifestLines,
        [System.Text.UTF8Encoding]::new($false))

    if (Test-Path -LiteralPath (Join-Path $packageStagingDirectory 'wwwroot')) {
        throw 'Composed API package contains a nested wwwroot directory.'
    }

    [System.IO.Directory]::Move($packageStagingDirectory, $resolvedOutputDirectory)
    $packageStagingDirectory = $null
    Write-Output "Composed API package: $resolvedOutputDirectory"
    Write-Output "Verified Client artifact: $expectedArtifactIdentity ($($clientArtifact.Inventory.Count) files)"
    Write-Output "Composition manifest: $(Join-Path $resolvedOutputDirectory $compositionManifestFileName)"
}
finally {
    if ($null -ne $packageStagingDirectory -and (Test-Path -LiteralPath $packageStagingDirectory)) {
        Remove-Item -LiteralPath $packageStagingDirectory -Recurse -Force
    }

    if ($createdWorkingDirectory -and (Test-Path -LiteralPath $resolvedWorkingDirectory)) {
        Remove-Item -LiteralPath $resolvedWorkingDirectory -Recurse -Force
    }
}