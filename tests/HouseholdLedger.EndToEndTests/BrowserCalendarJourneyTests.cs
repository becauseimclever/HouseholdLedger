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
/// Verifies the desktop calendar workspace through its single hosted API URL.
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
    /// Verifies that the API root renders the accessible desktop calendar workspace.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ApiHostRendersAccessibleDesktopCalendarWorkspace()
    {
        var inputs = BrowserInputs.Load();
        var apiOrigin = new Uri($"https://localhost:{inputs.ApiPort}");
        var driverPort = ReserveLoopbackPort(inputs.ApiPort);
        var driverOrigin = new Uri($"http://127.0.0.1:{driverPort}");
        var runId = $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Environment.ProcessId}";
        var profilePath = Path.Combine(inputs.ProfileRoot, runId, "profile");
        var screenshotPath = Path.Combine(inputs.OutputDirectory, $"workspace-shell-{runId}.png");
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
            await WaitForWorkspaceShellAsync(browser, timeout.Token);
            await AssertCalendarFeatureAsync(browser, timeout.Token);
            await AssertWorkspaceShellAsync(browser, apiOrigin, timeout.Token);

            var screenshot = await browser.TakeScreenshotAsync(timeout.Token);
            await File.WriteAllBytesAsync(screenshotPath, screenshot, timeout.Token);
            var screenshotHash = Convert.ToHexString(SHA256.HashData(screenshot)).ToLowerInvariant();
            TestContext.Progress.WriteLine($"Feature003 browser api={apiOrigin} apiPid={apiProcess.Id} firefoxPid={firefoxProcessId} geckodriverPid={driverProcess.Id}");
            TestContext.Progress.WriteLine($"Feature003 runtime hashes firefox={inputs.FirefoxHash} geckodriver={inputs.GeckodriverHash}");
            TestContext.Progress.WriteLine($"Feature003 screenshot bytes={screenshot.Length} sha256={screenshotHash}");
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

    private static async Task WaitForWorkspaceShellAsync(W3cWebDriver browser, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var heading = await browser.ExecuteScriptAsync(
                "return document.querySelector('main h1')?.textContent?.trim() ?? '';",
                null,
                cancellationToken);
            if (heading.GetString() == "Calendar")
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException("Timed out waiting for the calendar workspace shell.");
    }

    private static async Task AssertCalendarFeatureAsync(W3cWebDriver browser, CancellationToken cancellationToken)
    {
        var today = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Multiple(() =>
        {
            Assert.That(today.GetProperty("modeCount").GetInt32(), Is.EqualTo(3));
            Assert.That(today.GetProperty("activeMode").GetString(), Is.EqualTo("Today"));
            Assert.That(today.GetProperty("gridCount").GetInt32(), Is.Zero);
            Assert.That(today.GetProperty("selectedCount").GetInt32(), Is.EqualTo(1));
            Assert.That(today.GetProperty("tabStopCount").GetInt32(), Is.EqualTo(1));
            Assert.That(today.GetProperty("selectedPressed").GetString(), Is.EqualTo("true"));
            Assert.That(today.GetProperty("inspectorText").GetString(), Does.Contain("No calendar item selected"));
        });

        await ClickCalendarControlAsync(browser, "fieldset.calendar-mode-picker label:nth-of-type(2) input", cancellationToken);
        var week = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Multiple(() =>
        {
            Assert.That(week.GetProperty("activeMode").GetString(), Is.EqualTo("This Week"));
            Assert.That(week.GetProperty("gridCount").GetInt32(), Is.EqualTo(1));
            Assert.That(week.GetProperty("weekdayCount").GetInt32(), Is.EqualTo(7));
            Assert.That(week.GetProperty("dayCount").GetInt32(), Is.EqualTo(7));
            Assert.That(week.GetProperty("selectedCount").GetInt32(), Is.EqualTo(1));
            Assert.That(week.GetProperty("tabStopCount").GetInt32(), Is.EqualTo(1));
        });

        await ClickCalendarControlAsync(browser, "fieldset.calendar-mode-picker label:nth-of-type(3) input", cancellationToken);
        var month = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Multiple(() =>
        {
            Assert.That(month.GetProperty("activeMode").GetString(), Is.EqualTo("This Month"));
            Assert.That(month.GetProperty("gridCount").GetInt32(), Is.EqualTo(1));
            Assert.That(month.GetProperty("weekdayCount").GetInt32(), Is.EqualTo(7));
            Assert.That(month.GetProperty("dayCount").GetInt32(), Is.GreaterThanOrEqualTo(28));
            Assert.That(month.GetProperty("selectedCount").GetInt32(), Is.EqualTo(1));
            Assert.That(month.GetProperty("tabStopCount").GetInt32(), Is.EqualTo(1));
        });

        var directTarget = await browser.FindElementAsync(".calendar-grid .calendar-day[aria-pressed='false']", cancellationToken);
        await browser.ClickAsync(directTarget, cancellationToken);
        var activated = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.That(activated.GetProperty("focusedLabel").GetString(), Is.EqualTo(activated.GetProperty("selectedLabel").GetString()));

        var activeDate = await browser.FindElementAsync(".calendar-grid .calendar-day[aria-pressed='true']", cancellationToken);
        await browser.SendKeysAsync(activeDate, "\uE014", cancellationToken);
        var arrowMoved = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Multiple(() =>
        {
            Assert.That(arrowMoved.GetProperty("selectedLabel").GetString(), Is.Not.EqualTo(activated.GetProperty("selectedLabel").GetString()));
            Assert.That(arrowMoved.GetProperty("focusedLabel").GetString(), Is.EqualTo(arrowMoved.GetProperty("selectedLabel").GetString()));
            Assert.That(arrowMoved.GetProperty("selectedCount").GetInt32(), Is.EqualTo(1));
            Assert.That(arrowMoved.GetProperty("tabStopCount").GetInt32(), Is.EqualTo(1));
        });

        var headingBeforeNext = arrowMoved.GetProperty("heading").GetString();
        await ClickCalendarControlAsync(browser, "button[aria-label='Next period']", cancellationToken);
        var nextPeriod = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Multiple(() =>
        {
            Assert.That(nextPeriod.GetProperty("heading").GetString(), Is.Not.EqualTo(headingBeforeNext));
            Assert.That(nextPeriod.GetProperty("periodStatus").GetString(), Does.StartWith("Showing"));
            Assert.That(nextPeriod.GetProperty("calendarWidth").GetDouble(), Is.GreaterThan(0));
            Assert.That(nextPeriod.GetProperty("calendarRight").GetDouble(), Is.LessThanOrEqualTo(nextPeriod.GetProperty("inspectorLeft").GetDouble()));
            Assert.That(nextPeriod.GetProperty("scrollWidth").GetInt32(), Is.LessThanOrEqualTo(1440));
            Assert.That(nextPeriod.GetProperty("inspectorText").GetString(), Does.Contain("No calendar item selected"));
        });
    }

    private static async Task ClickCalendarControlAsync(W3cWebDriver browser, string selector, CancellationToken cancellationToken)
    {
        var element = await browser.FindElementAsync(selector, cancellationToken);
        await browser.ClickAsync(element, cancellationToken);
    }

    private static async Task<JsonElement> GetCalendarStateAsync(W3cWebDriver browser, CancellationToken cancellationToken)
    {
        return await browser.ExecuteScriptAsync(
            "const calendar = document.querySelector('#calendar-workspace'); const inspector = document.querySelector('#workspace-inspector'); const selected = calendar?.querySelector(\".calendar-day[aria-pressed='true']\"); const box = calendar?.getBoundingClientRect(); const inspectorBox = inspector?.getBoundingClientRect();"
            + " return { modeCount: calendar?.querySelectorAll(\"input[name='calendar-mode']\").length ?? 0, activeMode: calendar?.querySelector(\"input[name='calendar-mode']:checked\")?.parentElement?.textContent?.trim() ?? '', gridCount: calendar?.querySelectorAll('table.calendar-grid').length ?? 0, weekdayCount: calendar?.querySelectorAll('table.calendar-grid th[scope=col]').length ?? 0, dayCount: calendar?.querySelectorAll('.calendar-grid .calendar-day').length ?? 0, selectedCount: calendar?.querySelectorAll(\".calendar-day[aria-pressed='true']\").length ?? 0, selectedLabel: selected?.getAttribute('aria-label') ?? '', selectedPressed: selected?.getAttribute('aria-pressed') ?? '', tabStopCount: calendar?.querySelectorAll(\".calendar-day[tabindex='0']\").length ?? 0, focusedLabel: document.activeElement?.getAttribute('aria-label') ?? '', heading: calendar?.querySelector('#calendar-period-heading')?.textContent?.trim() ?? '', periodStatus: calendar?.querySelector('.calendar-period-status')?.textContent?.trim() ?? '', inspectorText: inspector?.textContent?.trim() ?? '', calendarWidth: box?.width ?? 0, calendarRight: box?.right ?? 0, inspectorLeft: inspectorBox?.left ?? 0, scrollWidth: document.documentElement.scrollWidth };",
            null,
            cancellationToken);
    }

    private static async Task AssertWorkspaceShellAsync(W3cWebDriver browser, Uri apiOrigin, CancellationToken cancellationToken)
    {
        var state = await GetWorkspaceStateAsync(browser, cancellationToken);
        AssertExpandedWorkspace(state, apiOrigin);

        await ClickAndWaitForToggleStateAsync(browser, "workspace-navigation", false, cancellationToken);
        state = await GetWorkspaceStateAsync(browser, cancellationToken);
        AssertNavigationCollapsedWorkspace(state);

        await ClickAndWaitForToggleStateAsync(browser, "workspace-inspector", false, cancellationToken);
        state = await GetWorkspaceStateAsync(browser, cancellationToken);
        AssertBothPanesCollapsedWorkspace(state);

        await ClickAndWaitForToggleStateAsync(browser, "workspace-navigation", true, cancellationToken);
        state = await GetWorkspaceStateAsync(browser, cancellationToken);
        AssertInspectorCollapsedWorkspace(state);

        await ClickAndWaitForToggleStateAsync(browser, "workspace-inspector", true, cancellationToken);
        state = await GetWorkspaceStateAsync(browser, cancellationToken);
        AssertExpandedWorkspace(state, apiOrigin);
    }

    private static async Task ClickAndWaitForToggleStateAsync(
        W3cWebDriver browser,
        string paneId,
        bool expanded,
        CancellationToken cancellationToken)
    {
        var toggle = await browser.FindElementAsync($"button[aria-controls='{paneId}']", cancellationToken);
        await browser.ClickAsync(toggle, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var state = await GetWorkspaceStateAsync(browser, cancellationToken);
            var toggleState = state.GetProperty(paneId == "workspace-navigation" ? "navigationToggle" : "inspectorToggle");
            if (toggleState.GetProperty("expanded").GetBoolean() == expanded)
            {
                Assert.That(state.GetProperty("focusedPane").GetString(), Is.EqualTo(paneId));
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for '{paneId}' to become expanded={expanded}.");
    }

    private static async Task<JsonElement> GetWorkspaceStateAsync(W3cWebDriver browser, CancellationToken cancellationToken)
    {
        return await browser.ExecuteScriptAsync(
            "const rectangle = element => { const box = element?.getBoundingClientRect(); return box ? { left: box.left, right: box.right, top: box.top, bottom: box.bottom, width: box.width, height: box.height } : null; };"
            + " const navigation = document.querySelector('#workspace-navigation'); const calendar = document.querySelector('#calendar-workspace'); const main = document.querySelector('main.calendar-page'); const inspector = document.querySelector('#workspace-inspector');"
            + " const navigationToggle = document.querySelector(\"button[aria-controls='workspace-navigation']\"); const inspectorToggle = document.querySelector(\"button[aria-controls='workspace-inspector']\");"
            + " return { title: document.title, origin: location.origin, width: window.innerWidth, height: window.innerHeight, scrollWidth: document.documentElement.scrollWidth, mainCount: document.querySelectorAll('#calendar-workspace > main.calendar-page').length, headingCount: document.querySelectorAll('main.calendar-page h1').length, heading: main?.querySelector('h1')?.textContent?.trim(), navigationDestinationCount: navigation?.querySelectorAll('a, button').length, inspectorText: inspector?.textContent?.trim(), navigation: { hidden: navigation?.hidden, rectangle: rectangle(navigation) }, calendar: { rectangle: rectangle(calendar) }, main: { rectangle: rectangle(main) }, inspector: { hidden: inspector?.hidden, rectangle: rectangle(inspector) }, navigationToggle: { expanded: navigationToggle?.getAttribute('aria-expanded') === 'true', label: navigationToggle?.getAttribute('aria-label') }, inspectorToggle: { expanded: inspectorToggle?.getAttribute('aria-expanded') === 'true', label: inspectorToggle?.getAttribute('aria-label') }, focusedPane: document.activeElement?.getAttribute('aria-controls') };",
            null,
            cancellationToken);
    }

    private static void AssertExpandedWorkspace(JsonElement state, Uri apiOrigin)
    {
        AssertWorkspaceSemantics(state, apiOrigin);
        Assert.Multiple(() =>
        {
            Assert.That(state.GetProperty("navigation").GetProperty("hidden").GetBoolean(), Is.False);
            Assert.That(state.GetProperty("inspector").GetProperty("hidden").GetBoolean(), Is.False);
            Assert.That(state.GetProperty("navigationToggle").GetProperty("expanded").GetBoolean(), Is.True);
            Assert.That(state.GetProperty("navigationToggle").GetProperty("label").GetString(), Is.EqualTo("Collapse navigation"));
            Assert.That(state.GetProperty("inspectorToggle").GetProperty("expanded").GetBoolean(), Is.True);
            Assert.That(state.GetProperty("inspectorToggle").GetProperty("label").GetString(), Is.EqualTo("Collapse inspector"));
        });
        AssertNormalFlow(state, includeNavigation: true, includeInspector: true);
    }

    private static void AssertNavigationCollapsedWorkspace(JsonElement state)
    {
        Assert.That(state.GetProperty("navigation").GetProperty("hidden").GetBoolean(), Is.True);
        Assert.That(state.GetProperty("navigationToggle").GetProperty("expanded").GetBoolean(), Is.False);
        Assert.That(state.GetProperty("inspector").GetProperty("hidden").GetBoolean(), Is.False);
        Assert.That(state.GetProperty("inspectorToggle").GetProperty("expanded").GetBoolean(), Is.True);
        AssertNormalFlow(state, includeNavigation: false, includeInspector: true);
    }

    private static void AssertBothPanesCollapsedWorkspace(JsonElement state)
    {
        Assert.That(state.GetProperty("navigation").GetProperty("hidden").GetBoolean(), Is.True);
        Assert.That(state.GetProperty("inspector").GetProperty("hidden").GetBoolean(), Is.True);
        Assert.That(state.GetProperty("navigationToggle").GetProperty("expanded").GetBoolean(), Is.False);
        Assert.That(state.GetProperty("inspectorToggle").GetProperty("expanded").GetBoolean(), Is.False);
        AssertNormalFlow(state, includeNavigation: false, includeInspector: false);
    }

    private static void AssertInspectorCollapsedWorkspace(JsonElement state)
    {
        Assert.That(state.GetProperty("navigation").GetProperty("hidden").GetBoolean(), Is.False);
        Assert.That(state.GetProperty("inspector").GetProperty("hidden").GetBoolean(), Is.True);
        Assert.That(state.GetProperty("navigationToggle").GetProperty("expanded").GetBoolean(), Is.True);
        Assert.That(state.GetProperty("inspectorToggle").GetProperty("expanded").GetBoolean(), Is.False);
        AssertNormalFlow(state, includeNavigation: true, includeInspector: false);
    }

    private static void AssertWorkspaceSemantics(JsonElement state, Uri apiOrigin)
    {
        Assert.Multiple(() =>
        {
            Assert.That(state.GetProperty("title").GetString(), Is.EqualTo("Calendar"));
            Assert.That(state.GetProperty("origin").GetString(), Is.EqualTo(apiOrigin.GetLeftPart(UriPartial.Authority)));
            Assert.That(state.GetProperty("width").GetInt32(), Is.EqualTo(1440));
            Assert.That(state.GetProperty("height").GetInt32(), Is.EqualTo(900));
            Assert.That(state.GetProperty("scrollWidth").GetInt32(), Is.LessThanOrEqualTo(1440));
            Assert.That(state.GetProperty("mainCount").GetInt32(), Is.EqualTo(1));
            Assert.That(state.GetProperty("headingCount").GetInt32(), Is.EqualTo(1));
            Assert.That(state.GetProperty("heading").GetString(), Is.EqualTo("Calendar"));
            Assert.That(state.GetProperty("navigationDestinationCount").GetInt32(), Is.Zero);
            Assert.That(state.GetProperty("inspectorText").GetString(), Does.Contain("No calendar item selected"));
        });
    }

    private static void AssertNormalFlow(JsonElement state, bool includeNavigation, bool includeInspector)
    {
        var calendar = state.GetProperty("calendar").GetProperty("rectangle");
        var main = state.GetProperty("main").GetProperty("rectangle");

        Assert.Multiple(() =>
        {
            Assert.That(calendar.GetProperty("width").GetDouble(), Is.GreaterThan(0));
            Assert.That(calendar.GetProperty("height").GetDouble(), Is.GreaterThan(0));
            Assert.That(main.GetProperty("width").GetDouble(), Is.GreaterThan(0));
            Assert.That(main.GetProperty("height").GetDouble(), Is.GreaterThan(0));
            Assert.That(main.GetProperty("top").GetDouble(), Is.LessThan(900));
            Assert.That(main.GetProperty("bottom").GetDouble(), Is.GreaterThan(0));
            Assert.That(state.GetProperty("scrollWidth").GetInt32(), Is.LessThanOrEqualTo(1440));
        });

        if (includeNavigation)
        {
            var navigation = state.GetProperty("navigation").GetProperty("rectangle");
            Assert.That(navigation.GetProperty("right").GetDouble(), Is.LessThanOrEqualTo(calendar.GetProperty("left").GetDouble()));
        }

        if (includeInspector)
        {
            var inspector = state.GetProperty("inspector").GetProperty("rectangle");
            Assert.That(calendar.GetProperty("right").GetDouble(), Is.LessThanOrEqualTo(inspector.GetProperty("left").GetDouble()));
        }
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
