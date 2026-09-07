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

using Xunit;
using Xunit.Sdk;

/// <summary>
/// Verifies the desktop calendar workspace through its single hosted API URL.
/// </summary>
[Collection("End-to-end process resources")]
public sealed class BrowserCalendarJourneyTests
{
    private const string ApiArtifactEnvironmentVariable = "HOUSEHOLDLEDGER_API_ARTIFACT";
    private const string FirefoxEnvironmentVariable = "HOUSEHOLDLEDGER_FIREFOX_BINARY";
    private const string GeckodriverEnvironmentVariable = "HOUSEHOLDLEDGER_GECKODRIVER";
    private const string ProfileRootEnvironmentVariable = "HOUSEHOLDLEDGER_E2E_PROFILE_ROOT";
    private const string OutputDirectoryEnvironmentVariable = "HOUSEHOLDLEDGER_E2E_OUTPUT_DIR";
    private const string ApiPortEnvironmentVariable = "HOUSEHOLDLEDGER_E2E_API_PORT";
    private const string PostgreSqlConnectionEnvironmentVariable =
        "HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING";

    private const string FirefoxSha256 = "79f01d224fe7f31795f2d4edcb31f497c96e11e9d0770704ed8495861f70d1c1";
    private const string GeckodriverSha256 = "e95b4eac7960ffcd5acbfd92bb7d49d48f99c1d01a20ddd297fef8c80821020d";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Verifies that the API root renders the accessible desktop calendar workspace.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ApiHostRendersAccessibleDesktopCalendarWorkspace()
    {
        var inputs = BrowserInputs.Load();
        var apiOrigin = new Uri($"https://localhost:{inputs.ApiPort}");
        var driverPort = ReserveLoopbackPort(inputs.ApiPort);
        var driverOrigin = new Uri($"http://127.0.0.1:{driverPort}");
        var runId = $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Environment.ProcessId}";
        var accountName = $"Browser account {runId}";
        var emptyAccountName = $"Empty account {runId}";
        var profilePath = Path.Combine(inputs.ProfileRoot, runId, "profile");
        var screenshotPath = Path.Combine(inputs.OutputDirectory, $"workspace-shell-{runId}.png");
        var mobileScreenshotPath = Path.Combine(inputs.OutputDirectory, $"workspace-shell-{runId}-mobile.png");
        Process? apiProcess = null;
        Process? driverProcess = null;
        W3cWebDriver? browser = null;
        int? firefoxProcessId = null;

        Directory.CreateDirectory(profilePath);
        Directory.CreateDirectory(inputs.OutputDirectory);

        try
        {
            apiProcess = StartApi(inputs.ApiAssemblyPath, apiOrigin, inputs.PostgreSqlConnectionString);
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
            await browser.ExecuteScriptAsync(
                "window.__householdLedgerTestErrors = []; window.addEventListener('error', event => window.__householdLedgerTestErrors.push(event.error?.stack ?? event.message)); window.addEventListener('unhandledrejection', event => window.__householdLedgerTestErrors.push(event.reason?.stack ?? String(event.reason)));",
                null,
                timeout.Token);
            await AssertCalendarFeatureAsync(browser, timeout.Token);
            await AssertAccountJourneyAsync(browser, accountName, emptyAccountName, timeout.Token);
            await SetDesktopViewportAsync(browser, timeout.Token);
            await AssertTransactionFeatureAsync(browser, accountName, emptyAccountName, timeout.Token);
            await AssertWorkspaceShellAsync(browser, apiOrigin, timeout.Token);
            await AssertDesignSystemAsync(browser, timeout.Token);

            var screenshot = await browser.TakeScreenshotAsync(timeout.Token);
            await File.WriteAllBytesAsync(screenshotPath, screenshot, timeout.Token);
            var screenshotHash = Convert.ToHexString(SHA256.HashData(screenshot)).ToLowerInvariant();
            await SetViewportAsync(browser, 500, 844, timeout.Token);
            var mobileState = await GetDesignSystemStateAsync(browser, timeout.Token);
            Assert.Multiple(
                () => Assert.True(mobileState.GetProperty("scrollWidth").GetInt32() <= 500),
                () => Assert.True(mobileState.GetProperty("navigation").GetProperty("bottom").GetDouble()
                    <= mobileState.GetProperty("main").GetProperty("top").GetDouble()),
                () => Assert.True(mobileState.GetProperty("main").GetProperty("bottom").GetDouble()
                    <= mobileState.GetProperty("inspector").GetProperty("top").GetDouble()));
            var mobileScreenshot = await browser.TakeScreenshotAsync(timeout.Token);
            await File.WriteAllBytesAsync(mobileScreenshotPath, mobileScreenshot, timeout.Token);
            var mobileScreenshotHash = Convert.ToHexString(SHA256.HashData(mobileScreenshot)).ToLowerInvariant();
            await SetDesktopViewportAsync(browser, timeout.Token);
            await AssertAccountNavigationAsync(browser, accountName, 1440, timeout.Token);
            await SetViewportAsync(browser, 500, 844, timeout.Token);
            await AssertAccountNavigationAsync(browser, accountName, 500, timeout.Token);
            TestContext.Current.TestOutputHelper?.WriteLine($"Feature003 browser api={apiOrigin} apiPid={apiProcess.Id} firefoxPid={firefoxProcessId} geckodriverPid={driverProcess.Id}");
            TestContext.Current.TestOutputHelper?.WriteLine($"Feature003 runtime hashes firefox={inputs.FirefoxHash} geckodriver={inputs.GeckodriverHash}");
            TestContext.Current.TestOutputHelper?.WriteLine($"Feature003 screenshot bytes={screenshot.Length} sha256={screenshotHash}");
            TestContext.Current.TestOutputHelper?.WriteLine($"Feature009 mobile screenshot bytes={mobileScreenshot.Length} sha256={mobileScreenshotHash}");
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
        await SetViewportAsync(browser, 1440, 900, cancellationToken);
    }

    private static async Task SetViewportAsync(
        W3cWebDriver browser,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        var chrome = await browser.ExecuteScriptAsync(
            "return { width: window.outerWidth - window.innerWidth, height: window.outerHeight - window.innerHeight };",
            null,
            cancellationToken);
        await browser.SetWindowRectAsync(
            width + chrome.GetProperty("width").GetInt32(),
            height + chrome.GetProperty("height").GetInt32(),
            cancellationToken);
    }

    private static async Task AssertDesignSystemAsync(
        W3cWebDriver browser,
        CancellationToken cancellationToken)
    {
        var defaultState = await GetDesignSystemStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.Equal("workbench-dark", defaultState.GetProperty("theme").GetString()),
            () => Assert.Equal("dark", defaultState.GetProperty("colorScheme").GetString()),
            () => Assert.True(defaultState.GetProperty("textContrast").GetDouble() >= 4.5),
            () => Assert.True(defaultState.GetProperty("secondaryContrast").GetDouble() >= 4.5),
            () => Assert.True(defaultState.GetProperty("focusContrast").GetDouble() >= 3),
            () => Assert.True(defaultState.GetProperty("inputBorderContrast").GetDouble() >= 3));

        await SetThemeOverridesAsync(
            browser,
            new Dictionary<string, string>
            {
                ["--hl-surface-chrome"] = "rgb(20, 70, 48)",
                ["--hl-action-primary"] = "rgb(140, 65, 20)",
            },
            cancellationToken);
        var visualOverride = await GetDesignSystemStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.NotEqual(defaultState.GetProperty("chrome").GetString(), visualOverride.GetProperty("chrome").GetString()),
            () => Assert.NotEqual(defaultState.GetProperty("primaryAction").GetString(), visualOverride.GetProperty("primaryAction").GetString()));

        await ClearThemeOverridesAsync(browser, cancellationToken);
        var restoredVisual = await GetDesignSystemStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.Equal(defaultState.GetProperty("chrome").GetString(), restoredVisual.GetProperty("chrome").GetString()),
            () => Assert.Equal(defaultState.GetProperty("primaryAction").GetString(), restoredVisual.GetProperty("primaryAction").GetString()));

        await SetThemeOverridesAsync(
            browser,
            new Dictionary<string, string>
            {
                ["--hl-workspace-areas"] = "\"inspector main navigation\"",
                ["--hl-workspace-columns"] = "var(--hl-workspace-inspector-extent) minmax(0, 1fr) var(--hl-workspace-navigation-extent)",
            },
            cancellationToken);
        var swapped = await GetDesignSystemStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.True(swapped.GetProperty("inspector").GetProperty("right").GetDouble()
                <= swapped.GetProperty("main").GetProperty("left").GetDouble()),
            () => Assert.True(swapped.GetProperty("main").GetProperty("right").GetDouble()
                <= swapped.GetProperty("navigation").GetProperty("left").GetDouble()));

        await SetThemeOverridesAsync(browser, CreateVerticalLayoutOverrides(), cancellationToken);
        await AssertVerticalLayoutAsync(browser, includeNavigation: true, includeInspector: true, cancellationToken);
        await ClickAndWaitForToggleStateAsync(browser, "workspace-navigation", false, cancellationToken);
        await AssertVerticalLayoutAsync(browser, includeNavigation: true, includeInspector: true, cancellationToken);
        await ClickAndWaitForToggleStateAsync(browser, "workspace-inspector", false, cancellationToken);
        await AssertVerticalLayoutAsync(browser, includeNavigation: true, includeInspector: true, cancellationToken);
        await ClickAndWaitForToggleStateAsync(browser, "workspace-navigation", true, cancellationToken);
        await AssertVerticalLayoutAsync(browser, includeNavigation: true, includeInspector: true, cancellationToken);
        await ClickAndWaitForToggleStateAsync(browser, "workspace-inspector", true, cancellationToken);
        await AssertVerticalLayoutAsync(browser, includeNavigation: true, includeInspector: true, cancellationToken);

        await ClearThemeOverridesAsync(browser, cancellationToken);
        var restoredLayout = await GetDesignSystemStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.True(restoredLayout.GetProperty("navigation").GetProperty("right").GetDouble()
                <= restoredLayout.GetProperty("main").GetProperty("left").GetDouble()),
            () => Assert.True(restoredLayout.GetProperty("main").GetProperty("right").GetDouble()
                <= restoredLayout.GetProperty("inspector").GetProperty("left").GetDouble()));
    }

    private static Dictionary<string, string> CreateVerticalLayoutOverrides()
    {
        return new Dictionary<string, string>
        {
            ["--hl-workspace-areas"] = "\"navigation\" \"main\" \"inspector\"",
            ["--hl-workspace-columns"] = "minmax(0, 1fr)",
            ["--hl-workspace-rows"] = "auto minmax(20rem, 1fr) auto",
            ["--hl-workspace-no-navigation-areas"] = "\"main\" \"inspector\"",
            ["--hl-workspace-no-navigation-columns"] = "minmax(0, 1fr)",
            ["--hl-workspace-no-navigation-rows"] = "minmax(20rem, 1fr) auto",
            ["--hl-workspace-collapsed-navigation-areas"] = "\"navigation\" \"main\" \"inspector\"",
            ["--hl-workspace-collapsed-navigation-columns"] = "minmax(0, 1fr)",
            ["--hl-workspace-collapsed-navigation-rows"] = "auto minmax(20rem, 1fr) auto",
            ["--hl-workspace-collapsed-navigation-no-inspector-areas"] = "\"navigation\" \"main\"",
            ["--hl-workspace-collapsed-navigation-no-inspector-columns"] = "minmax(0, 1fr)",
            ["--hl-workspace-collapsed-navigation-no-inspector-rows"] = "auto minmax(20rem, 1fr)",
            ["--hl-workspace-collapsed-inspector-areas"] = "\"navigation\" \"main\" \"inspector\"",
            ["--hl-workspace-collapsed-inspector-columns"] = "minmax(0, 1fr)",
            ["--hl-workspace-collapsed-inspector-rows"] = "auto minmax(20rem, 1fr) auto",
            ["--hl-workspace-collapsed-navigation-and-inspector-areas"] = "\"navigation\" \"main\" \"inspector\"",
            ["--hl-workspace-collapsed-navigation-and-inspector-columns"] = "minmax(0, 1fr)",
            ["--hl-workspace-collapsed-navigation-and-inspector-rows"] = "auto minmax(20rem, 1fr) auto",
            ["--hl-workspace-no-inspector-areas"] = "\"navigation\" \"main\"",
            ["--hl-workspace-no-inspector-columns"] = "minmax(0, 1fr)",
            ["--hl-workspace-no-inspector-rows"] = "auto minmax(20rem, 1fr)",
            ["--hl-workspace-main-only-areas"] = "\"main\"",
            ["--hl-workspace-main-only-columns"] = "minmax(0, 1fr)",
            ["--hl-workspace-main-only-rows"] = "minmax(20rem, 1fr)",
        };
    }

    private static async Task AssertVerticalLayoutAsync(
        W3cWebDriver browser,
        bool includeNavigation,
        bool includeInspector,
        CancellationToken cancellationToken)
    {
        var state = await GetDesignSystemStateAsync(browser, cancellationToken);
        var grid = state.GetProperty("grid");
        var main = state.GetProperty("main");
        Assert.True(main.GetProperty("height").GetDouble() >= 320);
        if (includeNavigation)
        {
            Assert.True(state.GetProperty("navigation").GetProperty("bottom").GetDouble()
                <= main.GetProperty("top").GetDouble());
        }
        else
        {
            Assert.Equal(grid.GetProperty("top").GetDouble(), main.GetProperty("top").GetDouble(), 1);
        }

        if (includeInspector)
        {
            Assert.True(main.GetProperty("bottom").GetDouble()
                <= state.GetProperty("inspector").GetProperty("top").GetDouble());
        }
        else
        {
            Assert.Equal(grid.GetProperty("bottom").GetDouble(), main.GetProperty("bottom").GetDouble(), 1);
        }
    }

    private static async Task SetThemeOverridesAsync(
        W3cWebDriver browser,
        IReadOnlyDictionary<string, string> overrides,
        CancellationToken cancellationToken)
    {
        await browser.ExecuteScriptAsync(
            "for (const [name, value] of Object.entries(arguments[0])) document.documentElement.style.setProperty(name, value);",
            [overrides],
            cancellationToken);
    }

    private static async Task ClearThemeOverridesAsync(
        W3cWebDriver browser,
        CancellationToken cancellationToken)
    {
        await browser.ExecuteScriptAsync(
            "for (const name of [...document.documentElement.style]) if (name.startsWith('--hl-')) document.documentElement.style.removeProperty(name);",
            null,
            cancellationToken);
    }

    private static async Task<JsonElement> GetDesignSystemStateAsync(
        W3cWebDriver browser,
        CancellationToken cancellationToken)
    {
        return await browser.ExecuteScriptAsync(
            "const rect = element => { const box = element.getBoundingClientRect(); return { left: box.left, right: box.right, top: box.top, bottom: box.bottom, width: box.width, height: box.height }; };"
            + " const rgb = value => value.match(/[\\d.]+/g).slice(0, 3).map(Number); const luminance = value => { const channels = rgb(value).map(item => { item /= 255; return item <= .04045 ? item / 12.92 : Math.pow((item + .055) / 1.055, 2.4); }); return .2126 * channels[0] + .7152 * channels[1] + .0722 * channels[2]; }; const contrast = (a, b) => { const first = luminance(a); const second = luminance(b); return (Math.max(first, second) + .05) / (Math.min(first, second) + .05); };"
            + " const paneElement = document.querySelector('.workspace-pane'); const toggle = document.querySelector('.pane-toggle'); const root = getComputedStyle(document.documentElement); const toolbar = getComputedStyle(document.querySelector('.workspace-toolbar')); const paneHeading = getComputedStyle(document.querySelector('.workspace-pane h2') ?? paneElement); const pane = getComputedStyle(paneElement); const input = getComputedStyle(document.querySelector('#transaction-amount') ?? toggle); const action = getComputedStyle(document.querySelector('#workspace-inspector form button') ?? toggle); toggle.focus(); const toggleStyle = getComputedStyle(toggle);"
            + " return { theme: document.documentElement.dataset.theme, colorScheme: root.colorScheme, chrome: toolbar.backgroundColor, primaryAction: action.backgroundColor, textContrast: contrast(root.color, root.backgroundColor), secondaryContrast: contrast(paneHeading.color, pane.backgroundColor), focusContrast: contrast(toggleStyle.outlineColor, toolbar.backgroundColor), inputBorderContrast: contrast(input.borderColor, input.backgroundColor), scrollWidth: document.documentElement.scrollWidth, grid: rect(document.querySelector('.workspace-grid')), navigation: rect(document.querySelector('#workspace-navigation')), main: rect(document.querySelector('#workspace-main')), inspector: rect(document.querySelector('#workspace-inspector')) };",
            null,
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
        var initialMonth = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.Equal(3, initialMonth.GetProperty("modeCount").GetInt32()),
            () => Assert.Equal("This Month", initialMonth.GetProperty("activeMode").GetString()),
            () => Assert.Equal(1, initialMonth.GetProperty("gridCount").GetInt32()),
            () => Assert.Equal(1, initialMonth.GetProperty("selectedCount").GetInt32()),
            () => Assert.Equal(1, initialMonth.GetProperty("tabStopCount").GetInt32()),
            () => Assert.Equal("true", initialMonth.GetProperty("selectedPressed").GetString()),
            () => Assert.Equal("date", initialMonth.GetProperty("selectedCurrent").GetString()),
            () => Assert.Contains("Expenses in USD", initialMonth.GetProperty("inspectorText").GetString(), StringComparison.Ordinal));

        await ClickCalendarControlAsync(browser, "fieldset.calendar-mode-picker label:nth-of-type(2) input", cancellationToken);
        var week = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.Equal("This Week", week.GetProperty("activeMode").GetString()),
            () => Assert.Equal(1, week.GetProperty("gridCount").GetInt32()),
            () => Assert.Equal(7, week.GetProperty("weekdayCount").GetInt32()),
            () => Assert.Equal(7, week.GetProperty("dayCount").GetInt32()),
            () => Assert.Equal(1, week.GetProperty("selectedCount").GetInt32()),
            () => Assert.Equal(1, week.GetProperty("tabStopCount").GetInt32()));

        await ClickCalendarControlAsync(browser, "fieldset.calendar-mode-picker label:nth-of-type(3) input", cancellationToken);
        var month = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.Equal("This Month", month.GetProperty("activeMode").GetString()),
            () => Assert.Equal(1, month.GetProperty("gridCount").GetInt32()),
            () => Assert.Equal(7, month.GetProperty("weekdayCount").GetInt32()),
            () => Assert.True(month.GetProperty("dayCount").GetInt32() >= 28),
            () => Assert.Equal(1, month.GetProperty("selectedCount").GetInt32()),
            () => Assert.Equal(1, month.GetProperty("tabStopCount").GetInt32()));

        var directTarget = await browser.FindElementAsync(".calendar-grid .calendar-day[aria-pressed='false']", cancellationToken);
        await browser.ClickAsync(directTarget, cancellationToken);
        var activated = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Equal(activated.GetProperty("selectedLabel").GetString(), activated.GetProperty("focusedLabel").GetString());

        var activeDate = await browser.FindElementAsync(".calendar-grid .calendar-day[aria-pressed='true']", cancellationToken);
        await browser.SendKeysAsync(activeDate, "\uE014", cancellationToken);
        var arrowMoved = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.NotEqual(activated.GetProperty("selectedLabel").GetString(), arrowMoved.GetProperty("selectedLabel").GetString()),
            () => Assert.Equal(arrowMoved.GetProperty("selectedLabel").GetString(), arrowMoved.GetProperty("focusedLabel").GetString()),
            () => Assert.Equal(1, arrowMoved.GetProperty("selectedCount").GetInt32()),
            () => Assert.Equal(1, arrowMoved.GetProperty("tabStopCount").GetInt32()));

        var headingBeforeNext = arrowMoved.GetProperty("heading").GetString();
        await ClickCalendarControlAsync(browser, "button[aria-label='Next period']", cancellationToken);
        var nextPeriod = await GetCalendarStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.NotEqual(headingBeforeNext, nextPeriod.GetProperty("heading").GetString()),
            () => Assert.StartsWith("Showing", nextPeriod.GetProperty("periodStatus").GetString(), StringComparison.Ordinal),
            () => Assert.True(nextPeriod.GetProperty("calendarWidth").GetDouble() > 0),
            () => Assert.True(nextPeriod.GetProperty("calendarRight").GetDouble() <= nextPeriod.GetProperty("inspectorLeft").GetDouble()),
            () => Assert.True(nextPeriod.GetProperty("scrollWidth").GetInt32() <= 1440),
            () => Assert.Contains("Expenses in USD", nextPeriod.GetProperty("inspectorText").GetString(), StringComparison.Ordinal));
    }

    private static async Task AssertAccountJourneyAsync(
        W3cWebDriver browser,
        string accountName,
        string emptyAccountName,
        CancellationToken cancellationToken)
    {
        var accountsLink = await browser.FindElementAsync(
            "#workspace-navigation a[href='/accounts']",
            cancellationToken);
        await browser.ClickAsync(accountsLink, cancellationToken);
        await WaitForHeadingAsync(browser, "Accounts", cancellationToken);

        var accountsNavigation = await GetNavigationStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.Equal("/accounts", accountsNavigation.GetProperty("path").GetString()),
            () => Assert.Equal(1, accountsNavigation.GetProperty("currentCount").GetInt32()),
            () => Assert.Equal("Accounts", accountsNavigation.GetProperty("currentLabel").GetString()));

        using var accountLoadTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        accountLoadTimeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            await WaitForElementAsync(browser, "#account-name", accountLoadTimeout.Token);
        }
        catch (OperationCanceledException) when (accountLoadTimeout.IsCancellationRequested)
        {
            var diagnostics = await browser.ExecuteScriptAsync(
                "return { body: document.body.innerText.slice(0, 2000), error: document.querySelector('#blazor-error-ui')?.textContent?.trim() ?? '', clientErrors: window.__householdLedgerTestErrors ?? [], apiResources: performance.getEntriesByType('resource').filter(item => item.name.includes('/api/')).map(item => ({ name: item.name, duration: item.duration, responseEnd: item.responseEnd, responseStatus: item.responseStatus ?? 0, transferSize: item.transferSize })) };",
                null,
                CancellationToken.None);
            throw new TimeoutException($"Accounts page did not finish loading. Browser state: {diagnostics}");
        }

        await SetFormValueAsync(browser, "#account-name", accountName, "input", cancellationToken);
        var createButton = await browser.FindElementAsync(
            ".new-account-form button[type='submit']",
            cancellationToken);
        await browser.ClickAsync(createButton, cancellationToken);
        await WaitForAccountAsync(browser, accountName, cancellationToken);
        await SetFormValueAsync(browser, "#account-name", emptyAccountName, "input", cancellationToken);
        createButton = await browser.FindElementAsync(
            ".new-account-form button[type='submit']",
            cancellationToken);
        await browser.ClickAsync(createButton, cancellationToken);
        await WaitForAccountAsync(browser, emptyAccountName, cancellationToken);

        await SetViewportAsync(browser, 500, 844, cancellationToken);
        var accountLayout = await browser.ExecuteScriptAsync(
            "const rect = selector => { const box = document.querySelector(selector)?.getBoundingClientRect(); return box ? { left: box.left, right: box.right, top: box.top, bottom: box.bottom, width: box.width, height: box.height } : null; }; return { width: window.innerWidth, scrollWidth: document.documentElement.scrollWidth, navigation: rect('#workspace-navigation'), main: rect('#workspace-main'), inspector: rect('#workspace-inspector'), account: rect('article.account-card'), creator: rect('.new-account-card') };",
            null,
            cancellationToken);
        var accountLayoutDetails = accountLayout.ToString();
        Assert.Multiple(
            () => Assert.True(
                accountLayout.GetProperty("scrollWidth").GetInt32()
                    <= accountLayout.GetProperty("width").GetInt32(),
                accountLayoutDetails),
            () => Assert.True(
                accountLayout.GetProperty("navigation").GetProperty("bottom").GetDouble()
                    <= accountLayout.GetProperty("main").GetProperty("top").GetDouble() + 1,
                accountLayoutDetails),
            () => Assert.True(accountLayout.GetProperty("account").GetProperty("width").GetDouble() > 0, accountLayoutDetails),
            () => Assert.True(accountLayout.GetProperty("account").GetProperty("height").GetDouble() > 0, accountLayoutDetails),
            () => Assert.True(accountLayout.GetProperty("creator").GetProperty("width").GetDouble() > 0, accountLayoutDetails),
            () => Assert.True(accountLayout.GetProperty("creator").GetProperty("height").GetDouble() > 0, accountLayoutDetails));

        var homeLink = await browser.FindElementAsync("#workspace-navigation a[href='/']", cancellationToken);
        await browser.ClickAsync(homeLink, cancellationToken);
        await WaitForHeadingAsync(browser, "Calendar", cancellationToken);
        var homeNavigation = await GetNavigationStateAsync(browser, cancellationToken);
        Assert.Multiple(
            () => Assert.Equal("/", homeNavigation.GetProperty("path").GetString()),
            () => Assert.Equal(1, homeNavigation.GetProperty("currentCount").GetInt32()),
            () => Assert.Equal("Home", homeNavigation.GetProperty("currentLabel").GetString()));
    }

    private static async Task AssertAccountNavigationAsync(
        W3cWebDriver browser,
        string accountName,
        int expectedViewportWidth,
        CancellationToken cancellationToken)
    {
        var disclosureState = await browser.ExecuteScriptAsync(
            "return document.querySelector(\"button[aria-controls='accounts-navigation-list']\")?.getAttribute('aria-expanded') === 'true';",
            null,
            cancellationToken);
        if (disclosureState.GetBoolean())
        {
            var collapseButton = await browser.FindElementAsync(
                "button[aria-controls='accounts-navigation-list']",
                cancellationToken);
            await browser.ClickAsync(collapseButton, cancellationToken);
        }

        var collapsed = await browser.ExecuteScriptAsync(
            "return document.querySelector('#accounts-navigation-list') === null;",
            null,
            cancellationToken);
        Assert.True(collapsed.GetBoolean());

        var expandButton = await browser.FindElementAsync(
            "button[aria-controls='accounts-navigation-list']",
            cancellationToken);
        await browser.ClickAsync(expandButton, cancellationToken);
        await WaitForTextAsync(
            browser,
            "#accounts-navigation-list",
            accountName,
            cancellationToken);
        var accountPath = (await browser.ExecuteScriptAsync(
            "const name = arguments[0]; const link = [...document.querySelectorAll('.account-navigation-link')].find(item => item.getAttribute('aria-label') === name); if (!link) throw new Error(`Missing navigation account ${name}`); const path = link.getAttribute('href'); link.click(); return path;",
            [accountName],
            cancellationToken)).GetString();
        await WaitForTextAsync(browser, "#account-history-heading", accountName, cancellationToken);
        await WaitForTextAsync(
            browser,
            ".account-history .history-state",
            "No transactions recorded for this account.",
            cancellationToken);

        var selected = await browser.ExecuteScriptAsync(
            "const navigation = document.querySelector('#workspace-navigation').getBoundingClientRect(); const link = document.querySelector(\".account-navigation-link[aria-current='page']\"); const box = link?.getBoundingClientRect(); const marker = link?.querySelector('.current-account-marker'); return { path: location.pathname, expanded: document.querySelector(\"button[aria-controls='accounts-navigation-list']\")?.getAttribute('aria-expanded'), branchCurrent: document.querySelector('.accounts-navigation-branch')?.classList.contains('current-navigation-branch'), accountCurrent: link?.getAttribute('aria-current'), markerVisible: marker ? getComputedStyle(marker).display !== 'none' : false, emptyHistory: document.querySelector('.account-history .history-state')?.textContent?.trim() ?? '', width: innerWidth, scrollWidth: document.documentElement.scrollWidth, linkInsideNavigation: Boolean(box && box.left >= navigation.left && box.right <= navigation.right) };",
            null,
            cancellationToken);
        Assert.Multiple(
            () => Assert.Equal(accountPath, selected.GetProperty("path").GetString()),
            () => Assert.Equal("true", selected.GetProperty("expanded").GetString()),
            () => Assert.True(selected.GetProperty("branchCurrent").GetBoolean()),
            () => Assert.Equal("page", selected.GetProperty("accountCurrent").GetString()),
            () => Assert.True(selected.GetProperty("markerVisible").GetBoolean()),
            () => Assert.Equal("No transactions recorded for this account.", selected.GetProperty("emptyHistory").GetString()),
            () => Assert.Equal(expectedViewportWidth, selected.GetProperty("width").GetInt32()),
            () => Assert.True(selected.GetProperty("scrollWidth").GetInt32() <= expectedViewportWidth),
            () => Assert.True(selected.GetProperty("linkInsideNavigation").GetBoolean()));

        var accountsLink = await browser.FindElementAsync(
            "#workspace-navigation a[href='/accounts']",
            cancellationToken);
        await browser.ClickAsync(accountsLink, cancellationToken);
        await WaitForPathAsync(browser, "/accounts", cancellationToken);
        var returned = await browser.ExecuteScriptAsync(
            "return { parentCurrent: document.querySelector(\"#workspace-navigation a[href='/accounts']\")?.getAttribute('aria-current'), accountCurrentCount: document.querySelectorAll(\".account-navigation-link[aria-current='page']\").length, expanded: document.querySelector(\"button[aria-controls='accounts-navigation-list']\")?.getAttribute('aria-expanded') };",
            null,
            cancellationToken);
        Assert.Multiple(
            () => Assert.Equal("page", returned.GetProperty("parentCurrent").GetString()),
            () => Assert.Equal(0, returned.GetProperty("accountCurrentCount").GetInt32()),
            () => Assert.Equal("true", returned.GetProperty("expanded").GetString()));
    }

    private static async Task<JsonElement> GetNavigationStateAsync(
        W3cWebDriver browser,
        CancellationToken cancellationToken)
    {
        return await browser.ExecuteScriptAsync(
            "const current = document.querySelector(\"#workspace-navigation a[aria-current='page']\"); return { path: location.pathname, currentCount: document.querySelectorAll(\"#workspace-navigation a[aria-current='page']\").length, currentLabel: current?.textContent?.trim() ?? '' };",
            null,
            cancellationToken);
    }

    private static async Task WaitForAccountAsync(
        W3cWebDriver browser,
        string accountName,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var state = await browser.ExecuteScriptAsync(
                "const name = arguments[0]; return { matches: [...document.querySelectorAll('article.account-card h2')].filter(item => item.textContent.trim() === name).length, status: document.querySelector('.success-state')?.textContent?.trim() ?? '', input: document.querySelector('#account-name')?.value ?? '' };",
                [accountName],
                cancellationToken);
            if (state.GetProperty("matches").GetInt32() == 1
                && state.GetProperty("status").GetString() == "Account created."
                && state.GetProperty("input").GetString() == string.Empty)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for account '{accountName}'.");
    }

    private static async Task WaitForHeadingAsync(
        W3cWebDriver browser,
        string expectedHeading,
        CancellationToken cancellationToken)
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

    private static async Task WaitForPathAsync(
        W3cWebDriver browser,
        string expectedPath,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var path = await browser.ExecuteScriptAsync("return location.pathname;", null, cancellationToken);
            if (path.GetString() == expectedPath)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for path '{expectedPath}'.");
    }

    private static async Task WaitForElementAsync(
        W3cWebDriver browser,
        string selector,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var found = await browser.ExecuteScriptAsync(
                "return document.querySelector(arguments[0]) !== null;",
                [selector],
                cancellationToken);
            if (found.GetBoolean())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for element '{selector}'.");
    }

    private static async Task ClickCalendarControlAsync(W3cWebDriver browser, string selector, CancellationToken cancellationToken)
    {
        var element = await browser.FindElementAsync(selector, cancellationToken);
        await browser.ClickAsync(element, cancellationToken);
    }

    private static async Task AssertTransactionFeatureAsync(
        W3cWebDriver browser,
        string accountName,
        string emptyAccountName,
        CancellationToken cancellationToken)
    {
        await ClickCalendarControlAsync(
            browser,
            ".calendar-grid .calendar-day[aria-pressed='false']",
            cancellationToken);
        await WaitForInspectorTextAsync(browser, "No transactions recorded", cancellationToken);
        await SetSelectOptionByTextAsync(browser, "#transaction-account", accountName, cancellationToken);
        await SetFormValueAsync(browser, "#transaction-amount", "12.34", "input", cancellationToken);
        await SetFormValueAsync(
            browser,
            "#transaction-classification",
            "Necessities",
            "change",
            cancellationToken);
        await ClickButtonByTextAsync(browser, "Save expense", cancellationToken);
        await WaitForTransactionSummaryAsync(browser, accountName, "Necessities", "$12.34", cancellationToken);
        await WaitForSelectedCalendarSummaryAsync(browser, "$12.34", cancellationToken);

        await ClickButtonByTextAsync(browser, "Edit", cancellationToken);
        await SetFormValueAsync(
            browser,
            ".transaction-edit-form input",
            "19.75",
            "input",
            cancellationToken);
        await SetFormValueAsync(
            browser,
            ".transaction-edit-form select[id^='edit-classification']",
            "Culture",
            "change",
            cancellationToken);
        await ClickButtonByTextAsync(browser, "Save changes", cancellationToken);
        await WaitForInspectorTextAsync(browser, "Expense updated", cancellationToken);
        await WaitForInspectorTextAsync(browser, "$19.75", cancellationToken);
        await WaitForSelectedCalendarSummaryAsync(browser, "$19.75", cancellationToken);
        var selectedDateLabel = (await browser.ExecuteScriptAsync(
            "return document.querySelector(\".calendar-day[aria-pressed='true']\")?.getAttribute('aria-label') ?? '';",
            null,
            cancellationToken)).GetString()!;

        await AssertAccountHistoryJourneyAsync(
            browser,
            accountName,
            emptyAccountName,
            selectedDateLabel,
            cancellationToken);
        await WaitForInspectorTextAsync(browser, "$19.75", cancellationToken);

        await ClickButtonByTextAsync(browser, "Remove", cancellationToken);
        await ClickButtonByTextAsync(browser, "Cancel", cancellationToken);
        await WaitForInspectorTextAsync(browser, "$19.75", cancellationToken);
        await ClickButtonByTextAsync(browser, "Remove", cancellationToken);
        await ClickButtonByTextAsync(browser, "Remove permanently", cancellationToken);
        await WaitForInspectorTextAsync(browser, "Expense removed", cancellationToken);
        await WaitForInspectorTextAsync(browser, "No transactions recorded", cancellationToken);
        await WaitForSelectedCalendarSummaryAsync(browser, null, cancellationToken);
    }

    private static async Task AssertAccountHistoryJourneyAsync(
        W3cWebDriver browser,
        string accountName,
        string emptyAccountName,
        string selectedDateLabel,
        CancellationToken cancellationToken)
    {
        var accountsLink = await browser.FindElementAsync(
            "#workspace-navigation a[href='/accounts']",
            cancellationToken);
        await browser.ClickAsync(accountsLink, cancellationToken);
        await WaitForHeadingAsync(browser, "Accounts", cancellationToken);
        await ClickAccountLinkAsync(browser, accountName, cancellationToken);
        await WaitForTextAsync(browser, "#account-history-heading", accountName, cancellationToken);
        var populated = await browser.ExecuteScriptAsync(
            "const selected = document.querySelector(\"article.account-card a[aria-current='page']\"); const rows = [...document.querySelectorAll('.account-history tbody tr')]; return { path: location.pathname, selected: selected?.textContent?.trim() ?? '', rows: rows.length, row: rows[0]?.textContent ?? '', scrollWidth: document.documentElement.scrollWidth, width: window.innerWidth };",
            null,
            cancellationToken);
        Assert.Multiple(
            () => Assert.StartsWith("/accounts/", populated.GetProperty("path").GetString(), StringComparison.Ordinal),
            () => Assert.Equal(accountName, populated.GetProperty("selected").GetString()),
            () => Assert.Equal(1, populated.GetProperty("rows").GetInt32()),
            () => Assert.Contains("$19.75", populated.GetProperty("row").GetString(), StringComparison.Ordinal),
            () => Assert.Contains("Culture", populated.GetProperty("row").GetString(), StringComparison.Ordinal));

        await ClickAccountLinkAsync(browser, emptyAccountName, cancellationToken);
        await WaitForTextAsync(
            browser,
            ".account-history .history-state",
            "No transactions recorded for this account.",
            cancellationToken);
        await SetViewportAsync(browser, 500, 844, cancellationToken);
        var empty = await browser.ExecuteScriptAsync(
            "return { selected: document.querySelector(\"article.account-card a[aria-current='page']\")?.textContent?.trim() ?? '', rows: document.querySelectorAll('.account-history tbody tr').length, scrollWidth: document.documentElement.scrollWidth, width: window.innerWidth };",
            null,
            cancellationToken);
        Assert.Multiple(
            () => Assert.Equal(emptyAccountName, empty.GetProperty("selected").GetString()),
            () => Assert.Equal(0, empty.GetProperty("rows").GetInt32()),
            () => Assert.True(empty.GetProperty("scrollWidth").GetInt32() <= empty.GetProperty("width").GetInt32()));

        await browser.ExecuteScriptAsync("history.back();", null, cancellationToken);
        await WaitForTextAsync(browser, "#account-history-heading", accountName, cancellationToken);
        var selectedAfterBack = await browser.ExecuteScriptAsync(
            "return document.querySelector(\"article.account-card a[aria-current='page']\")?.textContent?.trim() ?? '';",
            null,
            cancellationToken);
        Assert.Equal(
            accountName,
            selectedAfterBack.GetString());

        var homeLink = await browser.FindElementAsync("#workspace-navigation a[href='/']", cancellationToken);
        await browser.ClickAsync(homeLink, cancellationToken);
        await WaitForHeadingAsync(browser, "Calendar", cancellationToken);
        await SetDesktopViewportAsync(browser, cancellationToken);
        await browser.ExecuteScriptAsync(
            "const label = arguments[0]; const day = [...document.querySelectorAll('.calendar-day')].find(item => item.getAttribute('aria-label') === label); if (!day) throw new Error(`Missing calendar day ${label}`); day.click();",
            [selectedDateLabel],
            cancellationToken);
    }

    private static async Task ClickAccountLinkAsync(
        W3cWebDriver browser,
        string accountName,
        CancellationToken cancellationToken)
    {
        await browser.ExecuteScriptAsync(
            "const name = arguments[0]; const link = [...document.querySelectorAll('article.account-card a')].find(item => item.textContent.trim() === name); if (!link) throw new Error(`Missing account ${name}`); link.click();",
            [accountName],
            cancellationToken);
    }

    private static async Task WaitForTextAsync(
        W3cWebDriver browser,
        string selector,
        string expectedText,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var text = await browser.ExecuteScriptAsync(
                "return document.querySelector(arguments[0])?.textContent?.trim() ?? '';",
                [selector],
                cancellationToken);
            if (text.GetString()?.Contains(expectedText, StringComparison.Ordinal) == true)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for '{expectedText}' in '{selector}'.");
    }

    private static async Task SetSelectOptionByTextAsync(
        W3cWebDriver browser,
        string selector,
        string optionText,
        CancellationToken cancellationToken)
    {
        await browser.ExecuteScriptAsync(
            "const select = document.querySelector(arguments[0]); if (!select) throw new Error(`Missing ${arguments[0]}`); const option = [...select.options].find(item => item.textContent.trim() === arguments[1]); if (!option) throw new Error(`Missing option ${arguments[1]}`); select.value = option.value; select.dispatchEvent(new Event('change', { bubbles: true }));",
            [selector, optionText],
            cancellationToken);
    }

    private static async Task SetFormValueAsync(
        W3cWebDriver browser,
        string selector,
        string value,
        string eventName,
        CancellationToken cancellationToken)
    {
        await browser.ExecuteScriptAsync(
            "const element = document.querySelector(arguments[0]); if (!element) throw new Error(`Missing ${arguments[0]}`); element.value = arguments[1]; element.dispatchEvent(new Event(arguments[2], { bubbles: true }));",
            [selector, value, eventName],
            cancellationToken);
    }

    private static async Task ClickButtonByTextAsync(
        W3cWebDriver browser,
        string text,
        CancellationToken cancellationToken)
    {
        await browser.ExecuteScriptAsync(
            "const button = [...document.querySelectorAll('#workspace-inspector button')].find(item => item.textContent.trim() === arguments[0]); if (!button) throw new Error(`Missing button ${arguments[0]}`); button.click();",
            [text],
            cancellationToken);
    }

    private static async Task WaitForInspectorTextAsync(
        W3cWebDriver browser,
        string expectedText,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var text = await browser.ExecuteScriptAsync(
                "return document.querySelector('#workspace-inspector')?.textContent ?? '';",
                null,
                cancellationToken);
            if (text.GetString()?.Contains(expectedText, StringComparison.Ordinal) == true)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for inspector text '{expectedText}'.");
    }

    private static async Task WaitForTransactionSummaryAsync(
        W3cWebDriver browser,
        string accountName,
        string classification,
        string amount,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var matches = await browser.ExecuteScriptAsync(
                "return [...document.querySelectorAll('.transaction-summary')].filter(item => item.querySelector('small')?.textContent?.trim() === arguments[0] && item.querySelector('span > span')?.textContent?.trim() === arguments[1] && item.querySelector('strong')?.textContent?.trim() === arguments[2]).length;",
                [accountName, classification, amount],
                cancellationToken);
            if (matches.GetInt32() == 1)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException(
            $"Timed out waiting for {classification} {amount} transaction owned by '{accountName}'.");
    }

    private static async Task WaitForSelectedCalendarSummaryAsync(
        W3cWebDriver browser,
        string? expectedAmount,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var summary = await browser.ExecuteScriptAsync(
                "return document.querySelector(\".calendar-day[aria-pressed='true'] .calendar-day-summary\")?.textContent ?? '';",
                null,
                cancellationToken);
            var text = summary.GetString() ?? string.Empty;
            if (expectedAmount is null ? text.Length == 0 : text.Contains(expectedAmount, StringComparison.Ordinal))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException(expectedAmount is null
            ? "Timed out waiting for the selected calendar summary to clear."
            : $"Timed out waiting for selected calendar summary '{expectedAmount}'.");
    }

    private static async Task<JsonElement> GetCalendarStateAsync(W3cWebDriver browser, CancellationToken cancellationToken)
    {
        return await browser.ExecuteScriptAsync(
            "const calendar = document.querySelector('#workspace-main'); const inspector = document.querySelector('#workspace-inspector'); const selected = calendar?.querySelector(\".calendar-day[aria-pressed='true']\"); const box = calendar?.getBoundingClientRect(); const inspectorBox = inspector?.getBoundingClientRect();"
            + " return { modeCount: calendar?.querySelectorAll(\"input[name='calendar-mode']\").length ?? 0, activeMode: calendar?.querySelector(\"input[name='calendar-mode']:checked\")?.parentElement?.textContent?.trim() ?? '', gridCount: calendar?.querySelectorAll('table.calendar-grid').length ?? 0, weekdayCount: calendar?.querySelectorAll('table.calendar-grid th[scope=col]').length ?? 0, dayCount: calendar?.querySelectorAll('.calendar-grid .calendar-day').length ?? 0, selectedCount: calendar?.querySelectorAll(\".calendar-day[aria-pressed='true']\").length ?? 0, selectedLabel: selected?.getAttribute('aria-label') ?? '', selectedPressed: selected?.getAttribute('aria-pressed') ?? '', selectedCurrent: selected?.getAttribute('aria-current') ?? '', tabStopCount: calendar?.querySelectorAll(\".calendar-day[tabindex='0']\").length ?? 0, focusedLabel: document.activeElement?.getAttribute('aria-label') ?? '', heading: calendar?.querySelector('#calendar-period-heading')?.textContent?.trim() ?? '', periodStatus: calendar?.querySelector('.calendar-period-status')?.textContent?.trim() ?? '', inspectorText: inspector?.textContent?.trim() ?? '', calendarWidth: box?.width ?? 0, calendarRight: box?.right ?? 0, inspectorLeft: inspectorBox?.left ?? 0, scrollWidth: document.documentElement.scrollWidth };",
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
                Assert.Equal(paneId, state.GetProperty("focusedPane").GetString());
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
            + " const navigation = document.querySelector('#workspace-navigation'); const calendar = document.querySelector('#workspace-main'); const main = document.querySelector('main.calendar-page'); const inspector = document.querySelector('#workspace-inspector');"
            + " const navigationToggle = document.querySelector(\"button[aria-controls='workspace-navigation']\"); const inspectorToggle = document.querySelector(\"button[aria-controls='workspace-inspector']\");"
            + " const currentNavigation = navigation?.querySelector(\"a[aria-current='page']\"); return { title: document.title, origin: location.origin, width: window.innerWidth, height: window.innerHeight, scrollWidth: document.documentElement.scrollWidth, mainCount: document.querySelectorAll('#workspace-main > main.calendar-page').length, headingCount: document.querySelectorAll('main.calendar-page h1').length, heading: main?.querySelector('h1')?.textContent?.trim(), primaryNavigationDestinationCount: navigation?.querySelectorAll('a.workspace-navigation-link').length, currentNavigationCount: navigation?.querySelectorAll(\"a[aria-current='page']\").length, currentNavigationLabel: currentNavigation?.getAttribute('aria-label') ?? '', inspectorText: inspector?.textContent?.trim(), navigation: { hidden: navigation?.hidden, collapsed: navigation?.classList.contains('navigation-pane-collapsed'), rectangle: rectangle(navigation) }, calendar: { rectangle: rectangle(calendar) }, main: { rectangle: rectangle(main) }, inspector: { hidden: inspector?.hidden, collapsed: inspector?.classList.contains('inspector-pane-collapsed'), rectangle: rectangle(inspector) }, navigationToggle: { expanded: navigationToggle?.getAttribute('aria-expanded') === 'true', label: navigationToggle?.getAttribute('aria-label') }, inspectorToggle: { expanded: inspectorToggle?.getAttribute('aria-expanded') === 'true', label: inspectorToggle?.getAttribute('aria-label') }, focusedPane: document.activeElement?.getAttribute('aria-controls') };",
            null,
            cancellationToken);
    }

    private static void AssertExpandedWorkspace(JsonElement state, Uri apiOrigin)
    {
        AssertWorkspaceSemantics(state, apiOrigin);
        Assert.Multiple(
            () => Assert.False(state.GetProperty("navigation").GetProperty("hidden").GetBoolean()),
            () => Assert.False(state.GetProperty("navigation").GetProperty("collapsed").GetBoolean()),
            () => Assert.False(state.GetProperty("inspector").GetProperty("hidden").GetBoolean()),
            () => Assert.False(state.GetProperty("inspector").GetProperty("collapsed").GetBoolean()),
            () => Assert.True(state.GetProperty("navigationToggle").GetProperty("expanded").GetBoolean()),
            () => Assert.Equal("Collapse navigation", state.GetProperty("navigationToggle").GetProperty("label").GetString()),
            () => Assert.True(state.GetProperty("inspectorToggle").GetProperty("expanded").GetBoolean()),
            () => Assert.Equal("Collapse inspector", state.GetProperty("inspectorToggle").GetProperty("label").GetString()));
        AssertNormalFlow(state, includeNavigation: true, includeInspector: true);
    }

    private static void AssertNavigationCollapsedWorkspace(JsonElement state)
    {
        Assert.False(state.GetProperty("navigation").GetProperty("hidden").GetBoolean());
        Assert.True(state.GetProperty("navigation").GetProperty("collapsed").GetBoolean());
        Assert.Equal(3, state.GetProperty("primaryNavigationDestinationCount").GetInt32());
        Assert.False(state.GetProperty("navigationToggle").GetProperty("expanded").GetBoolean());
        Assert.Equal("Expand navigation", state.GetProperty("navigationToggle").GetProperty("label").GetString());
        Assert.False(state.GetProperty("inspector").GetProperty("hidden").GetBoolean());
        Assert.True(state.GetProperty("inspectorToggle").GetProperty("expanded").GetBoolean());
        AssertNormalFlow(state, includeNavigation: true, includeInspector: true);
    }

    private static void AssertBothPanesCollapsedWorkspace(JsonElement state)
    {
        Assert.False(state.GetProperty("navigation").GetProperty("hidden").GetBoolean());
        Assert.True(state.GetProperty("navigation").GetProperty("collapsed").GetBoolean());
        Assert.Equal(3, state.GetProperty("primaryNavigationDestinationCount").GetInt32());
        Assert.False(state.GetProperty("inspector").GetProperty("hidden").GetBoolean());
        Assert.True(state.GetProperty("inspector").GetProperty("collapsed").GetBoolean());
        Assert.False(state.GetProperty("navigationToggle").GetProperty("expanded").GetBoolean());
        Assert.False(state.GetProperty("inspectorToggle").GetProperty("expanded").GetBoolean());
        AssertNormalFlow(state, includeNavigation: true, includeInspector: true);
    }

    private static void AssertInspectorCollapsedWorkspace(JsonElement state)
    {
        Assert.False(state.GetProperty("navigation").GetProperty("hidden").GetBoolean());
        Assert.False(state.GetProperty("navigation").GetProperty("collapsed").GetBoolean());
        Assert.False(state.GetProperty("inspector").GetProperty("hidden").GetBoolean());
        Assert.True(state.GetProperty("inspector").GetProperty("collapsed").GetBoolean());
        Assert.True(state.GetProperty("navigationToggle").GetProperty("expanded").GetBoolean());
        Assert.False(state.GetProperty("inspectorToggle").GetProperty("expanded").GetBoolean());
        AssertNormalFlow(state, includeNavigation: true, includeInspector: true);
    }

    private static void AssertWorkspaceSemantics(JsonElement state, Uri apiOrigin)
    {
        Assert.Multiple(
            () => Assert.Equal("Calendar", state.GetProperty("title").GetString()),
            () => Assert.Equal(apiOrigin.GetLeftPart(UriPartial.Authority), state.GetProperty("origin").GetString()),
            () => Assert.Equal(1440, state.GetProperty("width").GetInt32()),
            () => Assert.Equal(900, state.GetProperty("height").GetInt32()),
            () => Assert.True(state.GetProperty("scrollWidth").GetInt32() <= 1440),
            () => Assert.Equal(1, state.GetProperty("mainCount").GetInt32()),
            () => Assert.Equal(1, state.GetProperty("headingCount").GetInt32()),
            () => Assert.Equal("Calendar", state.GetProperty("heading").GetString()),
            () => Assert.Equal(3, state.GetProperty("primaryNavigationDestinationCount").GetInt32()),
            () => Assert.Equal(1, state.GetProperty("currentNavigationCount").GetInt32()),
            () => Assert.Equal("Home", state.GetProperty("currentNavigationLabel").GetString()),
            () => Assert.Contains("Expenses in USD", state.GetProperty("inspectorText").GetString(), StringComparison.Ordinal));
    }

    private static void AssertNormalFlow(JsonElement state, bool includeNavigation, bool includeInspector)
    {
        var calendar = state.GetProperty("calendar").GetProperty("rectangle");
        var main = state.GetProperty("main").GetProperty("rectangle");

        Assert.Multiple(
            () => Assert.True(calendar.GetProperty("width").GetDouble() > 0),
            () => Assert.True(calendar.GetProperty("height").GetDouble() > 0),
            () => Assert.True(main.GetProperty("width").GetDouble() > 0),
            () => Assert.True(main.GetProperty("height").GetDouble() > 0),
            () => Assert.True(main.GetProperty("top").GetDouble() < 900),
            () => Assert.True(main.GetProperty("bottom").GetDouble() > 0),
            () => Assert.True(state.GetProperty("scrollWidth").GetInt32() <= 1440));

        if (includeNavigation)
        {
            var navigation = state.GetProperty("navigation").GetProperty("rectangle");
            Assert.True(navigation.GetProperty("right").GetDouble() <= calendar.GetProperty("left").GetDouble());
        }

        if (includeInspector)
        {
            var inspector = state.GetProperty("inspector").GetProperty("rectangle");
            Assert.True(calendar.GetProperty("right").GetDouble() <= inspector.GetProperty("left").GetDouble());
        }
    }

    private static async Task AssertSupportingEndpointsAsync(Uri apiOrigin)
    {
        using var client = new HttpClient { BaseAddress = apiOrigin, Timeout = TimeSpan.FromSeconds(5) };
        using var health = await client.GetAsync("/api/v1/health");
        using var openApi = await client.GetAsync("/openapi/v1.json");
        using var accounts = await client.GetAsync("/api/v1/accounts");
        var accountsBody = await accounts.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        Assert.True(accounts.IsSuccessStatusCode, accountsBody);
    }

    private static Process StartApi(string assemblyPath, Uri origin, string postgreSqlConnectionString)
    {
        var startInfo = CreateStartInfo(FindDotNetHost(), Path.GetDirectoryName(assemblyPath)!);
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = origin.AbsoluteUri;
        startInfo.Environment["ConnectionStrings__HouseholdLedger"] = postgreSqlConnectionString;
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

        process.OutputDataReceived += static (_, _) => { };
        process.ErrorDataReceived += static (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

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
        string PostgreSqlConnectionString,
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
                GetRequiredValue(PostgreSqlConnectionEnvironmentVariable),
                GetRequiredDirectory(ProfileRootEnvironmentVariable),
                GetRequiredDirectory(OutputDirectoryEnvironmentVariable),
                GetRequiredAvailablePort(ApiPortEnvironmentVariable),
                RequireApprovedHash(FirefoxEnvironmentVariable, firefoxPath, FirefoxSha256),
                RequireApprovedHash(GeckodriverEnvironmentVariable, geckodriverPath, GeckodriverSha256));
        }

        private static string GetRequiredValue(string variableName)
        {
            var value = Environment.GetEnvironmentVariable(variableName);
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new XunitException($"{variableName} must be configured.");
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
