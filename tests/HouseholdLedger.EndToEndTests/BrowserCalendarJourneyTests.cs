// <copyright file="BrowserCalendarJourneyTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.EndToEndTests;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

using NUnit.Framework;

/// <summary>
/// Verifies the temporary sample shell through its single hosted API URL.
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class BrowserCalendarJourneyTests
{
    private const string ApiArtifactEnvironmentVariable = "HOUSEHOLDLEDGER_API_ARTIFACT";
    private const string FirefoxEnvironmentVariable = "HOUSEHOLDLEDGER_FIREFOX_BINARY";
    private const string GeckodriverEnvironmentVariable = "HOUSEHOLDLEDGER_GECKODRIVER";
    private const string ProfileRootEnvironmentVariable = "HOUSEHOLDLEDGER_E2E_PROFILE_ROOT";
    private const string OutputDirectoryEnvironmentVariable = "HOUSEHOLDLEDGER_E2E_OUTPUT_DIR";
    private const string ApiPortEnvironmentVariable = "HOUSEHOLDLEDGER_E2E_API_PORT";
    private const string FirefoxSha256 = "79f01d224fe7f31795f2d4edcb31f497c96e11e9d0770704ed8495861f70d1c1";
    private const string GeckodriverSha256 = "e95b4eac7960ffcd5acbfd92bb7d49d48f99c1d01a20ddd297fef8c80821020d";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Verifies that the API root renders the accessible temporary sample shell.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ApiHostRendersAccessibleTemporarySampleShell()
    {
        var inputs = BrowserInputs.Load();
        var apiOrigin = new Uri($"https://localhost:{inputs.ApiPort}");
        var driverPort = ReserveLoopbackPort(inputs.ApiPort);
        var driverOrigin = new Uri($"http://127.0.0.1:{driverPort}");
        var runId = $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Environment.ProcessId}";
        var profilePath = Path.Combine(inputs.ProfileRoot, runId, "profile");
        var screenshotPath = Path.Combine(inputs.OutputDirectory, $"temporary-sample-{runId}.png");
        Process? apiProcess = null;
        Process? driverProcess = null;
        W3cWebDriver? browser = null;
        int? firefoxProcessId = null;

        Directory.CreateDirectory(profilePath);
        Directory.CreateDirectory(inputs.OutputDirectory);

        try
        {
            apiProcess = StartApi(inputs.ApiAssemblyPath, apiOrigin);
            await WaitForOkAsync(apiOrigin, "/api/v1/health", apiProcess);
            await AssertSupportingEndpointsAsync(apiOrigin);

            driverProcess = StartGeckodriver(inputs.GeckodriverPath, driverPort);
            browser = new W3cWebDriver(driverOrigin);
            await WaitForDriverAsync(browser, driverProcess);

            using var timeout = new CancellationTokenSource(StartupTimeout);
            var capabilities = await browser.CreateSessionAsync(
                inputs.FirefoxBinaryPath,
                profilePath,
                CreateFirefoxPreferences(),
                timeout.Token);
            AssertRuntimeCapabilities(capabilities);
            firefoxProcessId = capabilities.GetProperty("moz:processID").GetInt32();

            await SetDesktopViewportAsync(browser, timeout.Token);
            await browser.NavigateAsync(apiOrigin, timeout.Token);
            await WaitForShellAsync(browser, timeout.Token);
            await AssertShellAsync(browser, apiOrigin, timeout.Token);

            var screenshot = await browser.TakeScreenshotAsync(timeout.Token);
            await File.WriteAllBytesAsync(screenshotPath, screenshot, timeout.Token);
            var screenshotHash = Convert.ToHexString(SHA256.HashData(screenshot)).ToLowerInvariant();
            TestContext.Progress.WriteLine($"Feature002 browser api={apiOrigin} apiPid={apiProcess.Id} firefoxPid={firefoxProcessId} geckodriverPid={driverProcess.Id}");
            TestContext.Progress.WriteLine($"Feature002 runtime hashes firefox={inputs.FirefoxHash} geckodriver={inputs.GeckodriverHash}");
            TestContext.Progress.WriteLine($"Feature002 screenshot bytes={screenshot.Length} sha256={screenshotHash}");
        }
        finally
        {
            if (browser is not null)
            {
                await browser.DisposeAsync();
            }

            if (firefoxProcessId is not null)
            {
                await AssertProcessExitedAsync(firefoxProcessId.Value);
            }

            if (driverProcess is not null)
            {
                await StopOwnedProcessAsync(driverProcess);
                driverProcess.Dispose();
            }

            if (apiProcess is not null)
            {
                await StopOwnedProcessAsync(apiProcess);
                apiProcess.Dispose();
            }

            DeleteDirectory(Path.GetDirectoryName(profilePath)!);
            DeleteDirectory(inputs.OutputDirectory);
            AssertPortsReleased(inputs.ApiPort, driverPort);
        }
    }

    private static Dictionary<string, object> CreateFirefoxPreferences()
    {
        return new Dictionary<string, object>
        {
            ["app.update.auto"] = false,
            ["app.update.enabled"] = false,
            ["browser.shell.checkDefaultBrowser"] = false,
            ["datareporting.healthreport.uploadEnabled"] = false,
            ["toolkit.telemetry.enabled"] = false,
            ["webdriver_accept_untrusted_certs"] = false,
            ["webdriver_assume_untrusted_issuer"] = false,
        };
    }

    private static void AssertRuntimeCapabilities(JsonElement capabilities)
    {
        Assert.Multiple(() =>
        {
            Assert.That(capabilities.GetProperty("browserName").GetString(), Is.EqualTo("firefox"));
            Assert.That(capabilities.GetProperty("browserVersion").GetString(), Is.EqualTo("153.0.1"));
            Assert.That(capabilities.GetProperty("acceptInsecureCerts").GetBoolean(), Is.False);
            Assert.That(capabilities.GetProperty("moz:geckodriverVersion").GetString(), Is.EqualTo("0.37.1"));
        });
    }

    private static async Task SetDesktopViewportAsync(W3cWebDriver browser, CancellationToken cancellationToken)
    {
        const int width = 1440;
        const int height = 900;
        var chrome = await browser.ExecuteScriptAsync(
            "return { width: window.outerWidth - window.innerWidth, height: window.outerHeight - window.innerHeight };",
            null,
            cancellationToken);
        await browser.SetWindowRectAsync(
            width + chrome.GetProperty("width").GetInt32(),
            height + chrome.GetProperty("height").GetInt32(),
            cancellationToken);
    }

    private static async Task WaitForShellAsync(W3cWebDriver browser, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var heading = await browser.ExecuteScriptAsync(
                "return document.querySelector('main h1')?.textContent?.trim() ?? '';",
                null,
                cancellationToken);
            if (heading.GetString() == "Temporary sample content")
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException("Timed out waiting for the temporary sample shell.");
    }

    private static async Task AssertShellAsync(W3cWebDriver browser, Uri apiOrigin, CancellationToken cancellationToken)
    {
        var result = await browser.ExecuteScriptAsync(
            "const main = document.querySelector('main'); const headings = document.querySelectorAll('main h1');"
            + " return { title: document.title, mainCount: document.querySelectorAll('main').length, headingCount: headings.length, heading: headings[0]?.textContent?.trim(), text: main?.textContent?.trim(), origin: location.origin, width: window.innerWidth, height: window.innerHeight };",
            null,
            cancellationToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetProperty("title").GetString(), Is.EqualTo("Temporary sample"));
            Assert.That(result.GetProperty("mainCount").GetInt32(), Is.EqualTo(1));
            Assert.That(result.GetProperty("headingCount").GetInt32(), Is.EqualTo(1));
            Assert.That(result.GetProperty("heading").GetString(), Is.EqualTo("Temporary sample content"));
            Assert.That(result.GetProperty("text").GetString(), Does.Contain("This is a temporary sample."));
            Assert.That(result.GetProperty("origin").GetString(), Is.EqualTo(apiOrigin.GetLeftPart(UriPartial.Authority)));
            Assert.That(result.GetProperty("width").GetInt32(), Is.EqualTo(1440));
            Assert.That(result.GetProperty("height").GetInt32(), Is.EqualTo(900));
        });
    }

    private static async Task AssertSupportingEndpointsAsync(Uri apiOrigin)
    {
        using var client = new HttpClient { BaseAddress = apiOrigin, Timeout = TimeSpan.FromSeconds(5) };
        using var health = await client.GetAsync("/api/v1/health");
        using var openApi = await client.GetAsync("/openapi/v1.json");
        Assert.That(health.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(openApi.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private static Process StartApi(string assemblyPath, Uri origin)
    {
        var startInfo = CreateStartInfo(FindDotNetHost(), Path.GetDirectoryName(assemblyPath)!);
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = origin.AbsoluteUri;
        startInfo.Environment["ConnectionStrings__HouseholdLedger"] = string.Empty;
        return StartProcess(startInfo);
    }

    private static Process StartGeckodriver(string executablePath, int port)
    {
        var startInfo = CreateStartInfo(executablePath, Path.GetDirectoryName(executablePath)!);
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add("127.0.0.1");
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return StartProcess(startInfo);
    }

    private static ProcessStartInfo CreateStartInfo(string fileName, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        CopyEnvironmentVariable(startInfo, "SystemRoot");
        CopyEnvironmentVariable(startInfo, "WINDIR");
        CopyEnvironmentVariable(startInfo, "DOTNET_ROOT");
        CopyEnvironmentVariable(startInfo, "DOTNET_ROOT(x86)");
        CopyEnvironmentVariable(startInfo, "TEMP");
        CopyEnvironmentVariable(startInfo, "TMP");
        return startInfo;
    }

    private static Process StartProcess(ProcessStartInfo startInfo)
    {
        var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException($"Could not start '{startInfo.FileName}'.");
        }

        return process;
    }

    private static async Task WaitForOkAsync(Uri origin, string path, Process process)
    {
        using var client = new HttpClient { BaseAddress = origin, Timeout = TimeSpan.FromSeconds(2) };
        using var timeout = new CancellationTokenSource(StartupTimeout);
        while (!timeout.IsCancellationRequested)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"API process exited with code {process.ExitCode}.");
            }

            try
            {
                using var response = await client.GetAsync(path, timeout.Token);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }

        throw new TimeoutException($"Timed out waiting for '{path}'.");
    }

    private static async Task WaitForDriverAsync(W3cWebDriver browser, Process process)
    {
        using var timeout = new CancellationTokenSource(StartupTimeout);
        while (!timeout.IsCancellationRequested)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"geckodriver exited with code {process.ExitCode}.");
            }

            try
            {
                if ((await browser.GetStatusAsync(timeout.Token)).GetProperty("ready").GetBoolean())
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }

        throw new TimeoutException("Timed out waiting for geckodriver.");
    }

    private static int ReserveLoopbackPort(int excludedPort)
    {
        while (true)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            if (port != excludedPort)
            {
                return port;
            }
        }
    }

    private static async Task StopOwnedProcessAsync(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            using var timeout = new CancellationTokenSource(ShutdownTimeout);
            await process.WaitForExitAsync(timeout.Token);
        }
    }

    private static async Task AssertProcessExitedAsync(int processId)
    {
        using var timeout = new CancellationTokenSource(ShutdownTimeout);
        while (IsProcessRunning(processId) && !timeout.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }

        Assert.That(IsProcessRunning(processId), Is.False, $"Owned Firefox process {processId} must exit.");
    }

    private static bool IsProcessRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static void AssertPortsReleased(params int[] ports)
    {
        foreach (var port in ports)
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
        }
    }

    private static string FindDotNetHost()
    {
        var fileName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        var hostPath = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", fileName));
        return File.Exists(hostPath)
            ? hostPath
            : throw new InvalidOperationException($"Could not locate dotnet at '{hostPath}'.");
    }

    private static void CopyEnvironmentVariable(ProcessStartInfo startInfo, string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrEmpty(value))
        {
            startInfo.Environment[name] = value;
        }
    }

    private sealed record BrowserInputs(
        string ApiAssemblyPath,
        string FirefoxBinaryPath,
        string GeckodriverPath,
        string ProfileRoot,
        string OutputDirectory,
        int ApiPort,
        string FirefoxHash,
        string GeckodriverHash)
    {
        public static BrowserInputs Load()
        {
            var apiPath = GetRequiredFile(ApiArtifactEnvironmentVariable, "HouseholdLedger.Api.dll");
            var firefoxPath = GetRequiredFile(FirefoxEnvironmentVariable, "firefox.exe");
            var geckodriverPath = GetRequiredFile(GeckodriverEnvironmentVariable, "geckodriver.exe");
            return new BrowserInputs(
                apiPath,
                firefoxPath,
                geckodriverPath,
                GetRequiredDirectory(ProfileRootEnvironmentVariable),
                GetRequiredDirectory(OutputDirectoryEnvironmentVariable),
                GetRequiredAvailablePort(ApiPortEnvironmentVariable),
                RequireApprovedHash(FirefoxEnvironmentVariable, firefoxPath, FirefoxSha256),
                RequireApprovedHash(GeckodriverEnvironmentVariable, geckodriverPath, GeckodriverSha256));
        }

        private static string GetRequiredFile(string variableName, string exactFileName)
        {
            var path = GetNormalizedAbsolutePath(variableName);
            if (!string.Equals(Path.GetFileName(path), exactFileName, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            {
                throw new AssertionException($"{variableName} must name existing '{exactFileName}'.");
            }

            return path;
        }

        private static string GetRequiredDirectory(string variableName)
        {
            var path = GetNormalizedAbsolutePath(variableName);
            if (!Directory.Exists(path))
            {
                throw new AssertionException($"{variableName} must name an existing directory.");
            }

            return path;
        }

        private static int GetRequiredAvailablePort(string variableName)
        {
            if (!int.TryParse(Environment.GetEnvironmentVariable(variableName), out var port) || port is < 1 or > 65535)
            {
                throw new AssertionException($"{variableName} must be a valid TCP port.");
            }

            AssertPortsReleased(port);
            return port;
        }

        private static string GetNormalizedAbsolutePath(string variableName)
        {
            var configuredPath = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(configuredPath) || !Path.IsPathFullyQualified(configuredPath))
            {
                throw new AssertionException($"{variableName} must be an absolute path.");
            }

            var fullPath = Path.GetFullPath(configuredPath);
            if (!string.Equals(configuredPath, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new AssertionException($"{variableName} must be normalized.");
            }

            return fullPath;
        }

        private static string RequireApprovedHash(string variableName, string path, string approvedHash)
        {
            using var stream = File.OpenRead(path);
            var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!string.Equals(actualHash, approvedHash, StringComparison.Ordinal))
            {
                throw new AssertionException($"{variableName} SHA-256 mismatch. No browser process was launched.");
            }

            return actualHash;
        }
    }
}
