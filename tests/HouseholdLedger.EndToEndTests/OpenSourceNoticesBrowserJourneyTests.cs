// <copyright file="OpenSourceNoticesBrowserJourneyTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.EndToEndTests;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

using Xunit;
using Xunit.Sdk;

/// <summary>
/// Verifies the published desktop notices journey through its single hosted API URL.
/// </summary>
[Collection("End-to-end process resources")]
public sealed class OpenSourceNoticesBrowserJourneyTests
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
    private static readonly string[] ExpectedPackageNames =
    [
        "Blazicons.Lucide",
        "Blazicons",
        "BlazorComponentUtilities",
        "Npgsql",
        "Npgsql.EntityFrameworkCore.PostgreSQL",
    ];

    /// <summary>
    /// Verifies keyboard navigation to the published notices page without changing the default calendar workspace.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PublishedDesktopClientNavigatesToAccessibleOpenSourceNoticesAndReturnsToCalendarRoot()
    {
        var inputs = BrowserInputs.Load();
        var apiOrigin = new Uri($"https://localhost:{inputs.ApiPort}");
        var driverPort = ReserveLoopbackPort(inputs.ApiPort);
        var driverOrigin = new Uri($"http://127.0.0.1:{driverPort}");
        var runId = $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Environment.ProcessId}";
        var profilePath = Path.Combine(inputs.ProfileRoot, runId, "profile");
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
            await WaitForHeadingAsync(browser, "Calendar", timeout.Token);
            await AssertCalendarWorkspaceAsync(browser, apiOrigin, timeout.Token);

            await FocusAndActivateNoticesLinkAsync(browser, timeout.Token);
            await WaitForHeadingAsync(browser, "Open-source notices", timeout.Token);
            await AssertNoticesPageAsync(browser, timeout.Token);

            await browser.NavigateAsync(apiOrigin, timeout.Token);
            await WaitForHeadingAsync(browser, "Calendar", timeout.Token);
            await AssertCalendarWorkspaceAsync(browser, apiOrigin, timeout.Token);

            TestContext.Current.TestOutputHelper?.WriteLine(
                $"Feature008 browser api={apiOrigin} apiPid={apiProcess.Id} firefoxPid={firefoxProcessId} geckodriverPid={driverProcess.Id}");
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"Feature008 runtime hashes firefox={inputs.FirefoxHash} geckodriver={inputs.GeckodriverHash}");
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
        Assert.Multiple(
            () => Assert.Equal("firefox", capabilities.GetProperty("browserName").GetString()),
            () => Assert.Equal("153.0.1", capabilities.GetProperty("browserVersion").GetString()),
            () => Assert.False(capabilities.GetProperty("acceptInsecureCerts").GetBoolean()),
            () => Assert.Equal("0.37.1", capabilities.GetProperty("moz:geckodriverVersion").GetString()));
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

    private static async Task WaitForHeadingAsync(W3cWebDriver browser, string expectedHeading, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var heading = await browser.ExecuteScriptAsync(
                "return document.querySelector('main h1')?.textContent?.trim() ?? '';",
                null,
                cancellationToken);
            if (heading.GetString() == expectedHeading)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for heading '{expectedHeading}'.");
    }

    private static async Task AssertCalendarWorkspaceAsync(
        W3cWebDriver browser,
        Uri apiOrigin,
        CancellationToken cancellationToken)
    {
        var state = await browser.ExecuteScriptAsync(
            "const rectangle = element => { const box = element?.getBoundingClientRect(); return box ? { left: box.left, right: box.right, top: box.top, bottom: box.bottom, width: box.width, height: box.height } : null; };"
            + " const navigation = document.querySelector('#workspace-navigation'); const calendar = document.querySelector('#calendar-workspace'); const main = document.querySelector('main.calendar-page'); const inspector = document.querySelector('#workspace-inspector');"
            + " return { origin: location.origin, width: innerWidth, height: innerHeight, scrollWidth: document.documentElement.scrollWidth, mainCount: document.querySelectorAll('main.calendar-page').length, headingCount: document.querySelectorAll('main.calendar-page h1').length, heading: main?.querySelector('h1')?.textContent?.trim(), navigationDestinationCount: navigation?.querySelectorAll('a, button').length ?? -1, navigation: rectangle(navigation), calendar: rectangle(calendar), inspector: rectangle(inspector) };",
            null,
            cancellationToken);

        var navigation = state.GetProperty("navigation");
        var calendar = state.GetProperty("calendar");
        var inspector = state.GetProperty("inspector");
        Assert.Multiple(
            () => Assert.Equal(apiOrigin.GetLeftPart(UriPartial.Authority), state.GetProperty("origin").GetString()),
            () => Assert.Equal(1440, state.GetProperty("width").GetInt32()),
            () => Assert.Equal(900, state.GetProperty("height").GetInt32()),
            () => Assert.True(state.GetProperty("scrollWidth").GetInt32() <= 1440),
            () => Assert.Equal(1, state.GetProperty("mainCount").GetInt32()),
            () => Assert.Equal(1, state.GetProperty("headingCount").GetInt32()),
            () => Assert.Equal("Calendar", state.GetProperty("heading").GetString()),
            () => Assert.Equal(0, state.GetProperty("navigationDestinationCount").GetInt32()),
            () => Assert.True(navigation.GetProperty("right").GetDouble() <= calendar.GetProperty("left").GetDouble()),
            () => Assert.True(calendar.GetProperty("right").GetDouble() <= inspector.GetProperty("left").GetDouble()));
    }

    private static async Task FocusAndActivateNoticesLinkAsync(W3cWebDriver browser, CancellationToken cancellationToken)
    {
        var link = await browser.FindElementAsync(".workspace-auxiliary-link", cancellationToken);
        var beforeActivation = await browser.ExecuteScriptAsync(
            "const link = document.querySelector('.workspace-auxiliary-link'); const grid = document.querySelector('.workspace-grid'); const navigation = document.querySelector('#workspace-navigation'); link?.focus(); const linkBox = link?.getBoundingClientRect(); const gridBox = grid?.getBoundingClientRect(); const style = link ? getComputedStyle(link) : null; return { linkCount: document.querySelectorAll('.workspace-auxiliary-link').length, isNativeAnchor: link?.tagName === 'A', href: link?.getAttribute('href'), isFocused: document.activeElement === link, outlineStyle: style?.outlineStyle, current: link?.getAttribute('aria-current'), primaryLinkCount: navigation?.querySelectorAll('a').length ?? -1, belowGrid: (linkBox?.top ?? 0) >= (gridBox?.bottom ?? Number.POSITIVE_INFINITY), position: style?.position };",
            null,
            cancellationToken);

        Assert.Multiple(
            () => Assert.Equal(1, beforeActivation.GetProperty("linkCount").GetInt32()),
            () => Assert.True(beforeActivation.GetProperty("isNativeAnchor").GetBoolean()),
            () => Assert.Equal("/open-source-notices", beforeActivation.GetProperty("href").GetString()),
            () => Assert.True(beforeActivation.GetProperty("isFocused").GetBoolean()),
            () => Assert.Equal("solid", beforeActivation.GetProperty("outlineStyle").GetString()),
            () => Assert.Equal(JsonValueKind.Null, beforeActivation.GetProperty("current").ValueKind),
            () => Assert.Equal(0, beforeActivation.GetProperty("primaryLinkCount").GetInt32()),
            () => Assert.True(beforeActivation.GetProperty("belowGrid").GetBoolean()),
            () => Assert.NotEqual("absolute", beforeActivation.GetProperty("position").GetString()),
            () => Assert.NotEqual("fixed", beforeActivation.GetProperty("position").GetString()));

        await browser.SendKeysAsync(link, "\uE007", cancellationToken);
    }

    private static async Task AssertNoticesPageAsync(W3cWebDriver browser, CancellationToken cancellationToken)
    {
        var state = await browser.ExecuteScriptAsync(
            "const visible = element => { const box = element?.getBoundingClientRect(); const style = element ? getComputedStyle(element) : null; return Boolean(box && box.width > 0 && box.height > 0 && style?.visibility === 'visible' && style.display !== 'none'); };"
            + " const main = document.querySelector('main.open-source-notices-page'); const link = document.querySelector('.workspace-auxiliary-link'); const entries = [...document.querySelectorAll('.open-source-notice-entry')]; const pre = [...document.querySelectorAll('.open-source-license-notice pre')]; const externalLinks = [...document.querySelectorAll('.open-source-notice-entry a[target=_blank]')];"
            + " return { mainCount: document.querySelectorAll('main.open-source-notices-page').length, headingCount: document.querySelectorAll('main.open-source-notices-page h1').length, heading: main?.querySelector('h1')?.textContent?.trim(), focusedHeading: document.activeElement === main?.querySelector('h1'), current: link?.getAttribute('aria-current'), entryNames: entries.map(entry => entry.querySelector('h3')?.textContent?.trim() ?? ''), entryVisible: entries.every(visible), preCount: pre.length, preVisible: pre.every(visible), preReadable: pre.every(item => item.textContent.trim().length > 500 && getComputedStyle(item).whiteSpace === 'pre-wrap' && getComputedStyle(item).maxHeight === 'none'), externalLinkCount: externalLinks.length, externalLinksAccessible: externalLinks.every(item => visible(item) && item.textContent.trim().length > 0 && item.getAttribute('rel') === 'noopener noreferrer'), scrollWidth: document.documentElement.scrollWidth };",
            null,
            cancellationToken);

        Assert.Multiple(
            () => Assert.Equal(1, state.GetProperty("mainCount").GetInt32()),
            () => Assert.Equal(1, state.GetProperty("headingCount").GetInt32()),
            () => Assert.Equal("Open-source notices", state.GetProperty("heading").GetString()),
            () => Assert.True(state.GetProperty("focusedHeading").GetBoolean()),
            () => Assert.Equal("page", state.GetProperty("current").GetString()),
            () => Assert.Equivalent(
                ExpectedPackageNames,
                state.GetProperty("entryNames").EnumerateArray().Select(name => name.GetString())),
            () => Assert.True(state.GetProperty("entryVisible").GetBoolean()),
            () => Assert.Equal(7, state.GetProperty("preCount").GetInt32()),
            () => Assert.True(state.GetProperty("preVisible").GetBoolean()),
            () => Assert.True(state.GetProperty("preReadable").GetBoolean()),
            () => Assert.Equal(12, state.GetProperty("externalLinkCount").GetInt32()),
            () => Assert.True(state.GetProperty("externalLinksAccessible").GetBoolean()),
            () => Assert.True(state.GetProperty("scrollWidth").GetInt32() <= 1440));
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

    private static int ReserveLoopbackPort(params int[] excludedPorts)
    {
        while (true)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            if (!excludedPorts.Contains(port))
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

        Assert.False(IsProcessRunning(processId), $"Owned Firefox process {processId} must exit.");
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
                throw new XunitException($"{variableName} must name existing '{exactFileName}'.");
            }

            return path;
        }

        private static string GetRequiredDirectory(string variableName)
        {
            var path = GetNormalizedAbsolutePath(variableName);
            if (!Directory.Exists(path))
            {
                throw new XunitException($"{variableName} must name an existing directory.");
            }

            return path;
        }

        private static int GetRequiredAvailablePort(string variableName)
        {
            if (!int.TryParse(Environment.GetEnvironmentVariable(variableName), out var port) || port is < 1 or > 65535)
            {
                throw new XunitException($"{variableName} must be a valid TCP port.");
            }

            AssertPortsReleased(port);
            return port;
        }

        private static string GetNormalizedAbsolutePath(string variableName)
        {
            var configuredPath = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(configuredPath) || !Path.IsPathFullyQualified(configuredPath))
            {
                throw new XunitException($"{variableName} must be an absolute path.");
            }

            var fullPath = Path.GetFullPath(configuredPath);
            if (!string.Equals(configuredPath, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new XunitException($"{variableName} must be normalized.");
            }

            return fullPath;
        }

        private static string RequireApprovedHash(string variableName, string path, string approvedHash)
        {
            using var stream = File.OpenRead(path);
            var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!string.Equals(actualHash, approvedHash, StringComparison.Ordinal))
            {
                throw new XunitException($"{variableName} SHA-256 mismatch. No browser process was launched.");
            }

            return actualHash;
        }
    }
}
