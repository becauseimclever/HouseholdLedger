#Requires -Version 7.0

<#
.SYNOPSIS
Builds the API and compares its runtime OpenAPI document with the checked artifact.

.DESCRIPTION
Starts an isolated API process on loopback without database configuration. By default,
the script reports contract drift without changing files. Use -Update explicitly to
atomically replace the selected artifact with deterministic UTF-8 JSON.

.PARAMETER Port
Loopback port for the API. Omit or pass 0 to select an available ephemeral port.

.PARAMETER ArtifactPath
Artifact to compare or update. Defaults to src/HouseholdLedger.Api/openapi/v1.json.

.PARAMETER Update
Atomically replaces the selected artifact when runtime OpenAPI is valid.

.EXAMPLE
./scripts/openapi/Sync-OpenApi.ps1

.EXAMPLE
./scripts/openapi/Sync-OpenApi.ps1 -Port 43127 -Update
#>
[CmdletBinding()]
param(
    [ValidateRange(0, 65535)]
    [int]$Port = 0,

    [string]$ArtifactPath = (Join-Path $PSScriptRoot '..\..\src\HouseholdLedger.Api\openapi\v1.json'),

    [switch]$Update
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$projectPath = Join-Path $repositoryRoot 'src\HouseholdLedger.Api\HouseholdLedger.Api.csproj'
$resolvedArtifactPath = [System.IO.Path]::GetFullPath($ArtifactPath, $repositoryRoot)
$apiProcess = $null
$httpClient = $null
$standardOutputTask = $null
$standardErrorTask = $null
$temporaryArtifactPath = $null

function Get-AvailableLoopbackPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Assert-LoopbackPortAvailable {
    param([int]$CandidatePort)

    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $CandidatePort)
    try {
        $listener.Start()
    }
    catch {
        throw "Loopback port $CandidatePort is unavailable. Choose another port or omit -Port."
    }
    finally {
        $listener.Stop()
    }
}

function ConvertTo-CanonicalOpenApiJson {
    param(
        [Parameter(Mandatory)]
        [string]$Json,

        [switch]$ValidateShape
    )

    try {
        $document = [System.Text.Json.JsonDocument]::Parse($Json)
    }
    catch {
        throw "OpenAPI response is not valid JSON: $($_.Exception.Message)"
    }

    try {
        $root = $document.RootElement
        if ($ValidateShape) {
            if ($root.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
                throw 'OpenAPI root must be a JSON object.'
            }

            $openApiVersion = [System.Text.Json.JsonElement]::new()
            if (-not $root.TryGetProperty('openapi', [ref]$openApiVersion) -or
                $openApiVersion.ValueKind -ne [System.Text.Json.JsonValueKind]::String -or
                -not $openApiVersion.GetString().StartsWith('3.1.', [System.StringComparison]::Ordinal)) {
                throw 'OpenAPI document must declare an OpenAPI 3.1 version.'
            }

            foreach ($requiredObject in @('info', 'paths')) {
                $property = [System.Text.Json.JsonElement]::new()
                if (-not $root.TryGetProperty($requiredObject, [ref]$property) -or
                    $property.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
                    throw "OpenAPI document must contain an object-valued '$requiredObject' property."
                }
            }

            $servers = [System.Text.Json.JsonElement]::new()
            if ($root.TryGetProperty('servers', [ref]$servers) -and
                ($servers.ValueKind -ne [System.Text.Json.JsonValueKind]::Array -or $servers.GetArrayLength() -ne 0)) {
                throw "Runtime OpenAPI contains host-variant 'servers' data; generation must omit servers."
            }
        }

        $options = [System.Text.Json.JsonSerializerOptions]::new()
        $options.WriteIndented = $true
        $formatted = [System.Text.Json.JsonSerializer]::Serialize(
            $root,
            [System.Text.Json.JsonElement],
            $options)
        return $formatted.Replace("`r`n", "`n").TrimEnd([char[]]"`n") + "`n"
    }
    finally {
        $document.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "API project was not found: $projectPath"
}

if (-not $Update -and -not (Test-Path -LiteralPath $resolvedArtifactPath -PathType Leaf)) {
    throw "OpenAPI artifact was not found: $resolvedArtifactPath"
}

Push-Location $repositoryRoot
try {
    & dotnet build $projectPath --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "API build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if ($Port -eq 0) {
    $Port = Get-AvailableLoopbackPort
}
else {
    Assert-LoopbackPortAvailable -CandidatePort $Port
}

$endpoint = "http://127.0.0.1:$Port"
$failure = $null
$standardOutput = ''
$standardError = ''

try {
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in @('run', '--project', $projectPath, '--no-build', '--no-launch-profile', '--', '--urls', $endpoint)) {
        $startInfo.ArgumentList.Add($argument)
    }

    $startInfo.Environment['ASPNETCORE_ENVIRONMENT'] = 'Development'
    $startInfo.Environment['ConnectionStrings__HouseholdLedger'] = ''
    $startInfo.Environment['DOTNET_NOLOGO'] = '1'

    $apiProcess = [System.Diagnostics.Process]::new()
    $apiProcess.StartInfo = $startInfo
    if (-not $apiProcess.Start()) {
        throw 'The API process could not be started.'
    }

    $standardOutputTask = $apiProcess.StandardOutput.ReadToEndAsync()
    $standardErrorTask = $apiProcess.StandardError.ReadToEndAsync()

    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $httpClient = [System.Net.Http.HttpClient]::new($handler)
    $httpClient.Timeout = [TimeSpan]::FromSeconds(2)
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    $runtimeJson = $null
    $lastFetchError = $null

    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($apiProcess.HasExited) {
            throw "API process exited before OpenAPI was available (exit code $($apiProcess.ExitCode))."
        }

        try {
            $response = $httpClient.GetAsync("$endpoint/openapi/v1.json").GetAwaiter().GetResult()
            try {
                if (-not $response.IsSuccessStatusCode) {
                    throw "OpenAPI fetch returned HTTP $([int]$response.StatusCode) ($($response.ReasonPhrase))."
                }

                $runtimeJson = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                break
            }
            finally {
                $response.Dispose()
            }
        }
        catch {
            $lastFetchError = $_.Exception.Message
            [System.Threading.Tasks.Task]::Delay(200).GetAwaiter().GetResult()
        }
    }

    if ($null -eq $runtimeJson) {
        throw "Timed out fetching OpenAPI from $endpoint/openapi/v1.json. Last error: $lastFetchError"
    }

    $canonicalRuntimeJson = ConvertTo-CanonicalOpenApiJson -Json $runtimeJson -ValidateShape

    if ($Update) {
        $artifactDirectory = Split-Path -Parent $resolvedArtifactPath
        if (-not (Test-Path -LiteralPath $artifactDirectory -PathType Container)) {
            throw "Artifact directory was not found: $artifactDirectory"
        }

        $temporaryArtifactPath = Join-Path $artifactDirectory ".openapi-$([Guid]::NewGuid().ToString('N')).tmp"
        [System.IO.File]::WriteAllText(
            $temporaryArtifactPath,
            $canonicalRuntimeJson,
            [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::Move($temporaryArtifactPath, $resolvedArtifactPath, $true)
        $temporaryArtifactPath = $null
        Write-Output "Updated OpenAPI artifact from ${endpoint}: $resolvedArtifactPath"
    }
    else {
        $checkedJson = [System.IO.File]::ReadAllText($resolvedArtifactPath)
        $canonicalCheckedJson = ConvertTo-CanonicalOpenApiJson -Json $checkedJson
        if (-not [string]::Equals($canonicalRuntimeJson, $canonicalCheckedJson, [System.StringComparison]::Ordinal)) {
            throw "OpenAPI drift detected. Review the runtime contract, then run this script with -Update to replace: $resolvedArtifactPath"
        }

        Write-Output "OpenAPI artifact matches runtime generation at $endpoint."
    }
}
catch {
    $failure = $_
}
finally {
    if ($null -ne $httpClient) {
        $httpClient.Dispose()
    }

    if ($null -ne $apiProcess) {
        if (-not $apiProcess.HasExited) {
            $apiProcess.Kill($true)
        }

        $apiProcess.WaitForExit()
        if ($null -ne $standardOutputTask) {
            $standardOutput = $standardOutputTask.GetAwaiter().GetResult()
        }

        if ($null -ne $standardErrorTask) {
            $standardError = $standardErrorTask.GetAwaiter().GetResult()
        }

        $apiProcess.Dispose()
    }

    if ($null -ne $temporaryArtifactPath -and (Test-Path -LiteralPath $temporaryArtifactPath)) {
        Remove-Item -LiteralPath $temporaryArtifactPath -Force
    }
}

if ($null -ne $failure) {
    $diagnostics = @($standardError, $standardOutput) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_.Trim() }
    $diagnosticText = if ($diagnostics.Count -gt 0) {
        "`nAPI process diagnostics:`n" + ($diagnostics -join "`n")
    }
    else {
        ''
    }

    throw [System.InvalidOperationException]::new(
        "$($failure.Exception.Message)$diagnosticText",
        $failure.Exception)
}
