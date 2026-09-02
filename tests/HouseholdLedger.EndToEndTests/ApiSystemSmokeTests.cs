// <copyright file="ApiSystemSmokeTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.EndToEndTests;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

using Xunit;
using Xunit.Sdk;

/// <summary>
/// Verifies the published API through its language-neutral HTTP boundary.
/// </summary>
[CollectionDefinition("End-to-end process resources", DisableParallelization = true)]
[Collection("End-to-end process resources")]
public sealed class ApiSystemSmokeTests
{
    private const string ApiArtifactEnvironmentVariable = "HOUSEHOLDLEDGER_API_ARTIFACT";
    private const string ApiAssemblyFileName = "HouseholdLedger.Api.dll";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Verifies that a separate API process serves health and its OpenAPI contract.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SeparateApiProcessServesHealthAndOpenApiContract()
    {
        var apiAssemblyPath = GetRequiredApiArtifact();
        var port = ReserveLoopbackPort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}");
        var diagnostics = new ProcessDiagnostics();
        Process? process = null;

        try
        {
            process = StartApiProcess(apiAssemblyPath, baseAddress, diagnostics);
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"API system smoke process={process.Id} port={port} artifact={apiAssemblyPath}");

            using var client = new HttpClient
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromSeconds(5),
            };

            await WaitUntilHealthy(client, process, diagnostics);
            await AssertHealthResponse(client);
            await AssertOpenApiResponse(client);
        }
        catch (Exception exception)
        {
            var processDetails = process is null
                ? "The API process was not started."
                : diagnostics.Format(process);
            Assert.Fail($"{exception.Message}{Environment.NewLine}{processDetails}");
        }
        finally
        {
            if (process is not null)
            {
                await StopOwnedProcess(process);
                process.Dispose();
            }
        }
    }

    private static string GetRequiredApiArtifact()
    {
        var configuredPath = Environment.GetEnvironmentVariable(ApiArtifactEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new XunitException(
                $"Set {ApiArtifactEnvironmentVariable} to the normalized absolute path of a freshly built or published {ApiAssemblyFileName} before running this system test.");
        }

        if (!Path.IsPathFullyQualified(configuredPath))
        {
            throw new XunitException(
                $"{ApiArtifactEnvironmentVariable} must be an absolute path, but was '{configuredPath}'.");
        }

        var fullPath = Path.GetFullPath(configuredPath);
        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!string.Equals(configuredPath, fullPath, pathComparison))
        {
            throw new XunitException(
                $"{ApiArtifactEnvironmentVariable} must be a normalized absolute path. Configured '{configuredPath}', normalized '{fullPath}'.");
        }

        if (!string.Equals(Path.GetFileName(fullPath), ApiAssemblyFileName, StringComparison.Ordinal))
        {
            throw new XunitException(
                $"{ApiArtifactEnvironmentVariable} must name exactly '{ApiAssemblyFileName}', but was '{Path.GetFileName(fullPath)}'.");
        }

        if (!File.Exists(fullPath))
        {
            throw new XunitException(
                $"{ApiArtifactEnvironmentVariable} points to a file that does not exist: '{fullPath}'.");
        }

        return fullPath;
    }

    private static int ReserveLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static Process StartApiProcess(
        string apiAssemblyPath,
        Uri baseAddress,
        ProcessDiagnostics diagnostics)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = FindDotNetHost(),
            WorkingDirectory = Path.GetDirectoryName(apiAssemblyPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add(apiAssemblyPath);
        startInfo.Environment.Clear();
        CopyEnvironmentVariable(startInfo, "SystemRoot");
        CopyEnvironmentVariable(startInfo, "WINDIR");
        CopyEnvironmentVariable(startInfo, "DOTNET_ROOT");
        CopyEnvironmentVariable(startInfo, "DOTNET_ROOT(x86)");
        CopyEnvironmentVariable(startInfo, "TEMP");
        CopyEnvironmentVariable(startInfo, "TMP");
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = baseAddress.AbsoluteUri;
        startInfo.Environment["ConnectionStrings__HouseholdLedger"] = string.Empty;

        var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, eventArgs) => diagnostics.AppendOutput(eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => diagnostics.AppendError(eventArgs.Data);

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("The API process could not be started.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static string FindDotNetHost()
    {
        var fileName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        var hostPath = Path.GetFullPath(
            Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", fileName));

        return File.Exists(hostPath)
            ? hostPath
            : throw new InvalidOperationException($"Could not locate the dotnet host at '{hostPath}'.");
    }

    private static void CopyEnvironmentVariable(ProcessStartInfo startInfo, string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrEmpty(value))
        {
            startInfo.Environment[name] = value;
        }
    }

    private static async Task WaitUntilHealthy(
        HttpClient client,
        Process process,
        ProcessDiagnostics diagnostics)
    {
        using var timeout = new CancellationTokenSource(StartupTimeout);
        Exception? lastException = null;

        while (!timeout.IsCancellationRequested)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"The API process exited during startup with code {process.ExitCode}.{Environment.NewLine}{diagnostics.Format(process)}");
            }

            try
            {
                using var response = await client.GetAsync("/api/v1/health", timeout.Token);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }

                lastException = new HttpRequestException(
                    $"Health returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }
            catch (HttpRequestException exception)
            {
                lastException = exception;
            }
            catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
            {
                lastException = new TimeoutException("A health probe exceeded the HTTP client timeout.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException(
            $"The API did not become healthy within {StartupTimeout}. Last probe: {lastException?.Message}");
    }

    private static async Task AssertHealthResponse(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/health");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        Assert.Equal("available", document.RootElement.GetProperty("status").GetString());
    }

    private static async Task AssertOpenApiResponse(HttpClient client)
    {
        using var response = await client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var operation = root.GetProperty("paths").GetProperty("/api/v1/health").GetProperty("get");
        var successSchema = operation.GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");
        var healthSchema = root.GetProperty("components").GetProperty("schemas")
            .GetProperty("HealthResponse");
        var requiredProperties = healthSchema.GetProperty("required")
            .EnumerateArray()
            .Select(property => property.GetString())
            .ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("3.1.", root.GetProperty("openapi").GetString(), StringComparison.Ordinal);
        Assert.Equal("#/components/schemas/HealthResponse", successSchema.GetProperty("$ref").GetString());
        Assert.Equal("object", healthSchema.GetProperty("type").GetString());
        Assert.Contains("status", requiredProperties);
        Assert.Equal(
            "string",
            healthSchema.GetProperty("properties").GetProperty("status").GetProperty("type").GetString());
    }

    private static async Task StopOwnedProcess(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        process.Kill(entireProcessTree: true);
        using var timeout = new CancellationTokenSource(ShutdownTimeout);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"Test-owned API process {process.Id} did not exit within {ShutdownTimeout} after termination.");
        }
    }

    private sealed class ProcessDiagnostics
    {
        private readonly Lock sync = new();
        private readonly StringBuilder standardOutput = new();
        private readonly StringBuilder standardError = new();

        public void AppendOutput(string? line)
        {
            this.Append(this.standardOutput, line);
        }

        public void AppendError(string? line)
        {
            this.Append(this.standardError, line);
        }

        public string Format(Process process)
        {
            lock (this.sync)
            {
                var state = process.HasExited ? $"exited ({process.ExitCode})" : "running";
                return $"Process {process.Id} is {state}.{Environment.NewLine}"
                    + $"stdout:{Environment.NewLine}{this.standardOutput}"
                    + $"stderr:{Environment.NewLine}{this.standardError}";
            }
        }

        private void Append(StringBuilder target, string? line)
        {
            if (line is null)
            {
                return;
            }

            lock (this.sync)
            {
                target.AppendLine(line);
            }
        }
    }
}
