// <copyright file="BrowserCalendarJourneyTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.EndToEndTests;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using NUnit.Framework;

/// <summary>
/// Verifies the published standalone client against a separate published API.
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class BrowserCalendarJourneyTests
{
    private const string ApiArtifactEnvironmentVariable = "HOUSEHOLDLEDGER_API_ARTIFACT";
    private const string ClientPublishEnvironmentVariable = "HOUSEHOLDLEDGER_CLIENT_PUBLISH_DIR";
    private const string FirefoxEnvironmentVariable = "HOUSEHOLDLEDGER_FIREFOX_BINARY";
    private const string GeckodriverEnvironmentVariable = "HOUSEHOLDLEDGER_GECKODRIVER";
    private const string ApprovedFirefoxSha256 = "79f01d224fe7f31795f2d4edcb31f497c96e11e9d0770704ed8495861f70d1c1";
    private const string ApprovedGeckodriverSha256 = "e95b4eac7960ffcd5acbfd92bb7d49d48f99c1d01a20ddd297fef8c80821020d";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Verifies the critical calendar journey at representative desktop and mobile widths.
    /// </summary>
    /// <param name="viewportName">The human-readable viewport name.</param>
    /// <param name="width">The required inner viewport width.</param>
    /// <param name="height">The required inner viewport height.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestCase("desktop", 1440, 900)]
    [TestCase("mobile", 500, 844)]
    public async Task PublishedClientCompletesAccessibleCalendarJourney(
        string viewportName,
        int width,
        int height)
    {
        var inputs = BrowserInputs.Load();
        var apiPort = ReserveLoopbackPort();
        var clientPort = ReserveLoopbackPort(apiPort);
        var driverPort = ReserveLoopbackPort(apiPort, clientPort);
        var apiOrigin = new Uri($"http://127.0.0.1:{apiPort}");
        var clientOrigin = new Uri($"http://127.0.0.1:{clientPort}");
        var driverOrigin = new Uri($"http://127.0.0.1:{driverPort}");
        var runId = $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Environment.ProcessId}-{viewportName}";
        var profilePath = Path.Combine(Path.GetTempPath(), "HouseholdLedger-E2E", runId, "profile");
        var screenshotDirectory = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestResults",
            "browser-e2e");
        var apiDiagnostics = new ProcessDiagnostics("API");
        var driverDiagnostics = new ProcessDiagnostics("geckodriver");
        Process? apiProcess = null;
        Process? driverProcess = null;
        StaticClientHost? clientHost = null;
        W3cWebDriver? browser = null;
        int? firefoxProcessId = null;
        Exception? cleanupFailure = null;

        Directory.CreateDirectory(profilePath);
        Directory.CreateDirectory(screenshotDirectory);

        try
        {
            apiProcess = StartApi(inputs.ApiAssemblyPath, apiOrigin, clientOrigin, apiDiagnostics);
            clientHost = new StaticClientHost(inputs.ClientPublishDirectory, clientOrigin, apiOrigin);
            await clientHost.StartAsync();
            driverProcess = StartGeckodriver(inputs.GeckodriverPath, driverPort, driverDiagnostics);
            browser = new W3cWebDriver(driverOrigin);

            await WaitForApiAsync(apiOrigin, apiProcess, apiDiagnostics);
            await WaitForGeckodriverAsync(browser, driverProcess, driverDiagnostics);

            using var timeout = new CancellationTokenSource(StartupTimeout);
            var capabilities = await browser.CreateSessionAsync(
                inputs.FirefoxBinaryPath,
                profilePath,
                CreateFirefoxPreferences(),
                timeout.Token);
            AssertRuntimeCapabilities(capabilities, inputs.FirefoxBinaryPath);
            firefoxProcessId = capabilities.GetProperty("moz:processID").GetInt32();

            await SetExactViewportAsync(browser, width, height, timeout.Token);
            await browser.NavigateAsync(clientOrigin, timeout.Token);
            await InstallPageDiagnosticsAsync(browser, timeout.Token);
            await WaitForCalendarAsync(browser, apiOrigin, timeout.Token);
            await AssertCalendarAsync(browser, apiOrigin, width, height, timeout.Token);
            await AssertSkipNavigationAsync(browser, timeout.Token);
            await AssertPageDiagnosticsAsync(browser, clientOrigin, apiOrigin, timeout.Token);

            var unknownAddress = new Uri(clientOrigin, "/route-that-does-not-exist");
            await browser.NavigateAsync(unknownAddress, timeout.Token);
            await InstallPageDiagnosticsAsync(browser, timeout.Token);
            await WaitForTextAsync(browser, "h1", "Page not found", timeout.Token);
            await AssertNotFoundAsync(browser, timeout.Token);
            await AssertPageDiagnosticsAsync(browser, clientOrigin, apiOrigin, timeout.Token);

            var returnLink = await browser.FindElementAsync("main.not-found a", timeout.Token);
            await browser.ClickAsync(returnLink, timeout.Token);
            await WaitForCalendarAsync(browser, apiOrigin, timeout.Token);
            await AssertPageDiagnosticsAsync(browser, clientOrigin, apiOrigin, timeout.Token);
            clientHost.AssertNoFailedCriticalRequests();

            var screenshotPath = Path.Combine(screenshotDirectory, $"calendar-{runId}.png");
            var screenshot = await browser.TakeScreenshotAsync(timeout.Token);
            await File.WriteAllBytesAsync(screenshotPath, screenshot, timeout.Token);
            var screenshotHash = Convert.ToHexString(SHA256.HashData(screenshot)).ToLowerInvariant();

            TestContext.Progress.WriteLine(
                $"Browser E2E viewport={viewportName} inner={width}x{height} "
                + $"apiPid={apiProcess.Id} apiPort={apiPort} clientPort={clientPort} "
                + $"geckodriverPid={driverProcess.Id} geckodriverPort={driverPort}");
            TestContext.Progress.WriteLine(
                $"Browser capabilities={capabilities.GetRawText()}");
            TestContext.Progress.WriteLine(
                $"Screenshot path={screenshotPath} bytes={screenshot.Length} sha256={screenshotHash}");
            TestContext.Progress.WriteLine(
                "Browser diagnostics cover post-document error/unhandledrejection events, critical same-origin resource timing, explicit API health status, and static-host response status. "
                + "Residual limitation: W3C WebDriver Classic provides neither pre-navigation script injection nor Firefox console-log retrieval, so errors raised before the post-navigation listener is installed are not observable through this package-free driver.");
        }
        catch (Exception exception)
        {
            var diagnostics = FormatDiagnostics(apiProcess, apiDiagnostics, driverProcess, driverDiagnostics, clientHost);
            Assert.Fail($"{exception.Message}{Environment.NewLine}{diagnostics}");
        }
        finally
        {
            if (browser is not null)
            {
                await browser.DisposeAsync();
            }

            if (firefoxProcessId is not null)
            {
                cleanupFailure = await CaptureCleanupFailureAsync(
                    () => AssertProcessExitedAsync(firefoxProcessId.Value),
                    cleanupFailure);
            }

            if (driverProcess is not null)
            {
                cleanupFailure = await CaptureCleanupFailureAsync(
                    () => StopOwnedProcessAsync(driverProcess),
                    cleanupFailure);
                driverProcess.Dispose();
            }

            if (clientHost is not null)
            {
                cleanupFailure = await CaptureCleanupFailureAsync(
                    async () => await clientHost.DisposeAsync(),
                    cleanupFailure);
            }

            if (apiProcess is not null)
            {
                cleanupFailure = await CaptureCleanupFailureAsync(
                    () => StopOwnedProcessAsync(apiProcess),
                    cleanupFailure);
                apiProcess.Dispose();
            }

            cleanupFailure = await CaptureCleanupFailureAsync(
                () => DeleteDirectoryAsync(Path.GetDirectoryName(profilePath)!),
                cleanupFailure);
            cleanupFailure = await CaptureCleanupFailureAsync(
                () => AssertPortsReleasedAsync(apiPort, clientPort, driverPort),
                cleanupFailure);

            if (cleanupFailure is not null)
            {
                Assert.Fail($"Browser E2E cleanup failed: {cleanupFailure}");
            }
        }
    }

    private static Dictionary<string, object> CreateFirefoxPreferences()
    {
        return new Dictionary<string, object>
        {
            ["app.normandy.enabled"] = false,
            ["app.shield.optoutstudies.enabled"] = false,
            ["app.update.auto"] = false,
            ["app.update.enabled"] = false,
            ["breakpad.reportURL"] = string.Empty,
            ["browser.crashReports.unsubmittedCheck.autoSubmit2"] = false,
            ["browser.newtabpage.activity-stream.feeds.telemetry"] = false,
            ["browser.newtabpage.activity-stream.telemetry"] = false,
            ["browser.ping-centre.telemetry"] = false,
            ["browser.shell.checkDefaultBrowser"] = false,
            ["datareporting.healthreport.uploadEnabled"] = false,
            ["datareporting.policy.dataSubmissionEnabled"] = false,
            ["dom.security.https_only_mode"] = false,
            ["extensions.getAddons.cache.enabled"] = false,
            ["extensions.systemAddon.update.enabled"] = false,
            ["extensions.update.enabled"] = false,
            ["media.gmp-manager.updateEnabled"] = false,
            ["media.gmp-provider.enabled"] = false,
            ["network.captive-portal-service.enabled"] = false,
            ["network.connectivity-service.enabled"] = false,
            ["security.remote_settings.crlite_filters.enabled"] = false,
            ["services.settings.server"] = "data:,",
            ["toolkit.telemetry.archive.enabled"] = false,
            ["toolkit.telemetry.enabled"] = false,
            ["toolkit.telemetry.server"] = "data:,",
            ["toolkit.telemetry.unified"] = false,
            ["webdriver_accept_untrusted_certs"] = false,
            ["webdriver_assume_untrusted_issuer"] = false,
        };
    }

    private static void AssertRuntimeCapabilities(JsonElement capabilities, string firefoxBinaryPath)
    {
        Assert.Multiple(() =>
        {
            Assert.That(capabilities.GetProperty("browserName").GetString(), Is.EqualTo("firefox"));
            Assert.That(capabilities.GetProperty("browserVersion").GetString(), Is.EqualTo("153.0.1"));
            Assert.That(capabilities.GetProperty("acceptInsecureCerts").GetBoolean(), Is.False);
            Assert.That(
                capabilities.GetProperty("moz:geckodriverVersion").GetString(),
                Is.EqualTo("0.37.1"));
            Assert.That(
                capabilities.GetProperty("moz:processID").GetInt32(),
                Is.GreaterThan(0));
            Assert.That(firefoxBinaryPath, Does.EndWith("firefox.exe").IgnoreCase);
        });
    }

    private static async Task SetExactViewportAsync(
        W3cWebDriver browser,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        var chromeSize = await browser.ExecuteScriptAsync(
            "return { width: window.outerWidth - window.innerWidth, height: window.outerHeight - window.innerHeight };",
            null,
            cancellationToken);
        var outerWidth = width + chromeSize.GetProperty("width").GetInt32();
        var outerHeight = height + chromeSize.GetProperty("height").GetInt32();
        await browser.SetWindowRectAsync(outerWidth, outerHeight, cancellationToken);
        var viewport = await browser.ExecuteScriptAsync(
            "return { width: window.innerWidth, height: window.innerHeight };",
            null,
            cancellationToken);

        Assert.That(viewport.GetProperty("width").GetInt32(), Is.EqualTo(width));
        Assert.That(viewport.GetProperty("height").GetInt32(), Is.EqualTo(height));
    }

    private static async Task InstallPageDiagnosticsAsync(
        W3cWebDriver browser,
        CancellationToken cancellationToken)
    {
        var result = await browser.ExecuteScriptAsync(
            "if (!window.__householdLedgerE2eDiagnostics) {"
            + " window.__householdLedgerE2eDiagnostics = { errors: [] };"
            + " window.addEventListener('error', event => { const target = event.target; window.__householdLedgerE2eDiagnostics.errors.push({ type: 'error', message: event.message || 'Resource load failed', source: event.filename || target?.src || target?.href || '', line: event.lineno || 0, column: event.colno || 0 }); }, true);"
            + " window.addEventListener('unhandledrejection', event => { const reason = event.reason; window.__householdLedgerE2eDiagnostics.errors.push({ type: 'unhandledrejection', message: reason?.stack || reason?.message || String(reason), source: '', line: 0, column: 0 }); });"
            + "} return true;",
            null,
            cancellationToken);

        Assert.That(result.GetBoolean(), Is.True, "Page diagnostics must be installed after navigation.");
    }

    private static async Task AssertPageDiagnosticsAsync(
        W3cWebDriver browser,
        Uri clientOrigin,
        Uri apiOrigin,
        CancellationToken cancellationToken)
    {
        var healthUrl = new Uri(apiOrigin, "/api/v1/health").AbsoluteUri;
        var result = await browser.ExecuteAsyncScriptAsync(
            "const healthUrl = arguments[0]; const done = arguments[arguments.length - 1];"
            + " fetch(healthUrl, { cache: 'no-store' }).then(response => done({ apiStatus: response.status, apiOk: response.ok, apiUrl: response.url, errors: window.__householdLedgerE2eDiagnostics?.errors ?? [], resources: performance.getEntriesByType('resource').map(entry => ({ name: entry.name, initiatorType: entry.initiatorType, responseStatus: typeof entry.responseStatus === 'number' ? entry.responseStatus : null, transferSize: entry.transferSize, encodedBodySize: entry.encodedBodySize, decodedBodySize: entry.decodedBodySize })) })).catch(error => done({ apiStatus: 0, apiOk: false, apiUrl: healthUrl, apiError: error?.stack || error?.message || String(error), errors: window.__householdLedgerE2eDiagnostics?.errors ?? [], resources: performance.getEntriesByType('resource').map(entry => ({ name: entry.name, initiatorType: entry.initiatorType, responseStatus: typeof entry.responseStatus === 'number' ? entry.responseStatus : null, transferSize: entry.transferSize, encodedBodySize: entry.encodedBodySize, decodedBodySize: entry.decodedBodySize })) }));",
            new object[] { healthUrl },
            cancellationToken);
        var resources = result.GetProperty("resources").EnumerateArray().ToArray();
        var criticalResources = resources.Where(IsCriticalClientResource).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetProperty("errors").GetArrayLength(), Is.Zero, result.GetProperty("errors").GetRawText());
            Assert.That(result.GetProperty("apiStatus").GetInt32(), Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(result.GetProperty("apiOk").GetBoolean(), Is.True);
            Assert.That(result.GetProperty("apiUrl").GetString(), Is.EqualTo(healthUrl));
            Assert.That(
                criticalResources.Any(resource => GetResourcePath(resource) == "/appsettings.json"),
                Is.True,
                "Resource timing must include the runtime client configuration.");
            Assert.That(
                criticalResources.Any(resource => GetResourcePath(resource) == "/_framework/blazor.webassembly.js"),
                Is.True,
                "Resource timing must include the Blazor WebAssembly loader.");
            Assert.That(
                criticalResources.Any(resource => GetResourcePath(resource) == "/_framework/dotnet.js"),
                Is.True,
                "Resource timing must include the .NET WebAssembly loader.");
            Assert.That(
                criticalResources.Any(resource => GetResourcePath(resource).Contains("dotnet.runtime", StringComparison.Ordinal)),
                Is.True,
                "Resource timing must include the .NET WebAssembly runtime.");
            Assert.That(
                criticalResources.Any(resource => GetResourcePath(resource).Contains("HouseholdLedger.Client", StringComparison.Ordinal)),
                Is.True,
                "Resource timing must include the client application assembly.");

            foreach (var resource in criticalResources)
            {
                AssertSuccessfulResourceTiming(resource, clientOrigin);
            }
        });
    }

    private static bool IsCriticalClientResource(JsonElement resource)
    {
        var path = GetResourcePath(resource);
        return path == "/appsettings.json"
            || path == "/css/app.css"
            || path == "/HouseholdLedger.Client.styles.css"
            || path == "/_framework/blazor.webassembly.js"
            || path == "/_framework/dotnet.js"
            || path.Contains("dotnet.runtime", StringComparison.Ordinal)
            || path.Contains("dotnet.native", StringComparison.Ordinal)
            || path.Contains("HouseholdLedger.Client", StringComparison.Ordinal);
    }

    private static string GetResourcePath(JsonElement resource)
    {
        return new Uri(resource.GetProperty("name").GetString()!).AbsolutePath;
    }

    private static void AssertSuccessfulResourceTiming(JsonElement resource, Uri clientOrigin)
    {
        var resourceUri = new Uri(resource.GetProperty("name").GetString()!);
        Assert.That(
            resourceUri.GetLeftPart(UriPartial.Authority),
            Is.EqualTo(clientOrigin.GetLeftPart(UriPartial.Authority)),
            $"Critical client resource must come from the isolated static host: {resourceUri}");

        var responseStatus = resource.GetProperty("responseStatus");
        if (responseStatus.ValueKind == JsonValueKind.Number && responseStatus.GetInt32() != 0)
        {
            Assert.That(
                responseStatus.GetInt32(),
                Is.InRange(200, 399),
                $"Critical client resource returned a non-success status: {resourceUri}");
            return;
        }

        Assert.That(
            resource.GetProperty("encodedBodySize").GetInt64(),
            Is.GreaterThan(0),
            $"Critical client resource has neither a browser-exposed success status nor a response body: {resourceUri}");
    }

    private static async Task WaitForCalendarAsync(
        W3cWebDriver browser,
        Uri apiOrigin,
        CancellationToken cancellationToken)
    {
        await PollAsync(
            async () =>
            {
                var state = await browser.ExecuteScriptAsync(
                    "return { title: document.title, status: document.querySelector('[role=status]')?.textContent?.trim() ?? '', resources: performance.getEntriesByType('resource').map(entry => entry.name) };",
                    null,
                    cancellationToken);
                return state.GetProperty("title").GetString() == "Calendar | Household Ledger"
                    && state.GetProperty("status").GetString() == "API available"
                    && state.GetProperty("resources").EnumerateArray().Any(
                        resource => resource.GetString() == new Uri(apiOrigin, "/api/v1/health").AbsoluteUri);
            },
            "the calendar, API available status, and cross-origin health resource",
            cancellationToken);
    }

    private static async Task AssertCalendarAsync(
        W3cWebDriver browser,
        Uri apiOrigin,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        var result = await browser.ExecuteScriptAsync(
            "const selectors = ['header.shell-header', 'main.calendar-page', '.period-toolbar > div:first-child', '.period-actions', '.calendar-scroll', '.empty-state', 'footer.shell-footer'];"
            + " const boxes = selectors.map(selector => { const element = document.querySelector(selector); const rect = element.getBoundingClientRect(); return { selector, left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom, scrollWidth: element.scrollWidth, clientWidth: element.clientWidth }; });"
            + " const overlap = (first, second) => first.left < second.right && first.right > second.left && first.top < second.bottom && first.bottom > second.top;"
            + " const heading = document.querySelector('h1'); const period = document.querySelector('[data-period-heading]'); const empty = document.querySelector('[data-empty-state]');"
            + " return { title: document.title, heading: heading?.textContent?.trim(), mainLabel: document.querySelector('main')?.getAttribute('aria-labelledby'), period: period?.textContent?.trim(), caption: document.querySelector('caption')?.textContent?.trim(), emptyHeading: empty?.querySelector('h3')?.textContent?.trim(), emptyText: empty?.querySelector('p')?.textContent?.trim(), navLabel: document.querySelector('nav')?.getAttribute('aria-label'), resources: performance.getEntriesByType('resource').map(entry => entry.name), boxes, overlaps: [overlap(boxes[0], boxes[1]), overlap(boxes[2], boxes[3]), overlap(boxes[4], boxes[5]), overlap(boxes[5], boxes[6])], overflowingText: [...document.querySelectorAll('h1,h2,h3,p,button,th')].filter(element => element.getClientRects().length > 0 && element.scrollWidth > element.clientWidth + 1).map(element => element.textContent.trim()), viewport: { width: window.innerWidth, height: window.innerHeight } };",
            null,
            cancellationToken);
        var period = result.GetProperty("period").GetString();
        var healthUrl = new Uri(apiOrigin, "/api/v1/health").AbsoluteUri;

        Assert.Multiple(() =>
        {
            Assert.That(result.GetProperty("title").GetString(), Is.EqualTo("Calendar | Household Ledger"));
            Assert.That(result.GetProperty("heading").GetString(), Is.EqualTo("Calendar"));
            Assert.That(result.GetProperty("mainLabel").GetString(), Is.EqualTo("calendar-heading"));
            Assert.That(result.GetProperty("navLabel").GetString(), Is.EqualTo("Primary navigation"));
            Assert.That(period, Is.Not.Null.And.Not.Empty);
            Assert.That(result.GetProperty("caption").GetString(), Is.EqualTo($"Calendar for {period}"));
            Assert.That(result.GetProperty("emptyHeading").GetString(), Is.EqualTo($"No ledger entries for {period}"));
            Assert.That(
                result.GetProperty("emptyText").GetString(),
                Is.EqualTo("This calendar is ready when there is something real to record."));
            Assert.That(
                result.GetProperty("resources").EnumerateArray().Select(item => item.GetString()),
                Does.Contain(healthUrl),
                "Resource timing must prove the browser called the configured API origin.");
            Assert.That(
                result.GetProperty("overlaps").EnumerateArray().Select(item => item.GetBoolean()),
                Is.All.False,
                "Key page regions must not overlap incoherently.");
            Assert.That(
                result.GetProperty("overflowingText").GetArrayLength(),
                Is.Zero,
                $"Visible key text must fit at {width}x{height}.");
            Assert.That(result.GetProperty("viewport").GetProperty("width").GetInt32(), Is.EqualTo(width));
            Assert.That(result.GetProperty("viewport").GetProperty("height").GetInt32(), Is.EqualTo(height));
        });
    }

    private static async Task AssertSkipNavigationAsync(
        W3cWebDriver browser,
        CancellationToken cancellationToken)
    {
        var firstTabbable = await browser.ExecuteScriptAsync(
            "const tabbable = [...document.querySelectorAll('a[href],button,[tabindex]')].filter(element => !element.disabled && element.tabIndex >= 0); document.querySelector('.skip-link').focus(); return { firstClass: tabbable[0]?.className, firstText: tabbable[0]?.textContent?.trim() };",
            null,
            cancellationToken);
        Assert.That(firstTabbable.GetProperty("firstClass").GetString(), Does.Contain("skip-link"));
        Assert.That(firstTabbable.GetProperty("firstText").GetString(), Is.EqualTo("Skip to main content"));

        var focused = await browser.ExecuteScriptAsync(
            "return { text: document.activeElement?.textContent?.trim(), visible: document.activeElement ? getComputedStyle(document.activeElement).display !== 'none' && document.activeElement.getBoundingClientRect().width > 0 : false };",
            null,
            cancellationToken);
        Assert.That(focused.GetProperty("text").GetString(), Is.EqualTo("Skip to main content"));
        Assert.That(focused.GetProperty("visible").GetBoolean(), Is.True);

        var skipLink = await browser.FindElementAsync(".skip-link", cancellationToken);
        await browser.SendKeysAsync(skipLink, "\uE007", cancellationToken);
        var target = await browser.ExecuteScriptAsync(
            "const target = document.querySelector('#main-content'); return { hash: location.hash, id: target?.id, tabindex: target?.getAttribute('tabindex') };",
            null,
            cancellationToken);
        Assert.That(target.GetProperty("hash").GetString(), Is.EqualTo("#main-content"));
        Assert.That(target.GetProperty("id").GetString(), Is.EqualTo("main-content"));
        Assert.That(target.GetProperty("tabindex").GetString(), Is.EqualTo("-1"));
    }

    private static async Task WaitForTextAsync(
        W3cWebDriver browser,
        string selector,
        string expected,
        CancellationToken cancellationToken)
    {
        await PollAsync(
            async () =>
            {
                var result = await browser.ExecuteScriptAsync(
                    "return document.querySelector(arguments[0])?.textContent?.trim() ?? '';",
                    new object[] { selector },
                    cancellationToken);
                return result.GetString() == expected;
            },
            $"'{selector}' to contain '{expected}'",
            cancellationToken);
    }

    private static async Task AssertNotFoundAsync(
        W3cWebDriver browser,
        CancellationToken cancellationToken)
    {
        var result = await browser.ExecuteScriptAsync(
            "return { title: document.title, heading: document.querySelector('h1')?.textContent?.trim(), label: document.querySelector('main')?.getAttribute('aria-labelledby'), link: document.querySelector('main a')?.textContent?.trim() };",
            null,
            cancellationToken);
        Assert.Multiple(() =>
        {
            Assert.That(result.GetProperty("title").GetString(), Is.EqualTo("Page not found | Household Ledger"));
            Assert.That(result.GetProperty("heading").GetString(), Is.EqualTo("Page not found"));
            Assert.That(result.GetProperty("label").GetString(), Is.EqualTo("not-found-heading"));
            Assert.That(result.GetProperty("link").GetString(), Is.EqualTo("Return to calendar"));
        });
    }

    private static Process StartApi(
        string assemblyPath,
        Uri apiOrigin,
        Uri clientOrigin,
        ProcessDiagnostics diagnostics)
    {
        var startInfo = CreateIsolatedStartInfo(FindDotNetHost(), Path.GetDirectoryName(assemblyPath)!);
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = apiOrigin.AbsoluteUri;
        startInfo.Environment["ConnectionStrings__HouseholdLedger"] = string.Empty;
        startInfo.Environment["Cors__AllowedOrigins__0"] = clientOrigin.GetLeftPart(UriPartial.Authority);
        return StartProcess(startInfo, diagnostics);
    }

    private static Process StartGeckodriver(
        string executablePath,
        int port,
        ProcessDiagnostics diagnostics)
    {
        var startInfo = CreateIsolatedStartInfo(executablePath, Path.GetDirectoryName(executablePath)!);
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add("127.0.0.1");
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.Environment["MOZ_CRASHREPORTER_DISABLE"] = "1";
        startInfo.Environment["MOZ_TELEMETRY_REPORTING"] = "0";
        return StartProcess(startInfo, diagnostics);
    }

    private static ProcessStartInfo CreateIsolatedStartInfo(string fileName, string workingDirectory)
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
        startInfo.Environment.Clear();
        CopyEnvironmentVariable(startInfo, "SystemRoot");
        CopyEnvironmentVariable(startInfo, "WINDIR");
        CopyEnvironmentVariable(startInfo, "DOTNET_ROOT");
        CopyEnvironmentVariable(startInfo, "DOTNET_ROOT(x86)");
        CopyEnvironmentVariable(startInfo, "TEMP");
        CopyEnvironmentVariable(startInfo, "TMP");
        return startInfo;
    }

    private static Process StartProcess(ProcessStartInfo startInfo, ProcessDiagnostics diagnostics)
    {
        var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, eventArgs) => diagnostics.AppendOutput(eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => diagnostics.AppendError(eventArgs.Data);
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException($"Could not start '{startInfo.FileName}'.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static async Task WaitForApiAsync(
        Uri apiOrigin,
        Process process,
        ProcessDiagnostics diagnostics)
    {
        using var client = new HttpClient { BaseAddress = apiOrigin, Timeout = TimeSpan.FromSeconds(2) };
        using var timeout = new CancellationTokenSource(StartupTimeout);
        await PollAsync(
            async () =>
            {
                ThrowIfExited(process, diagnostics);
                try
                {
                    using var response = await client.GetAsync("/api/v1/health", timeout.Token);
                    return response.StatusCode == HttpStatusCode.OK;
                }
                catch (HttpRequestException)
                {
                    return false;
                }
                catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
                {
                    return false;
                }
            },
            "the API health endpoint",
            timeout.Token);
    }

    private static async Task WaitForGeckodriverAsync(
        W3cWebDriver browser,
        Process process,
        ProcessDiagnostics diagnostics)
    {
        using var timeout = new CancellationTokenSource(StartupTimeout);
        await PollAsync(
            async () =>
            {
                ThrowIfExited(process, diagnostics);
                try
                {
                    var status = await browser.GetStatusAsync(timeout.Token);
                    return status.GetProperty("ready").GetBoolean();
                }
                catch (HttpRequestException)
                {
                    return false;
                }
            },
            "geckodriver readiness",
            timeout.Token);
    }

    private static async Task PollAsync(
        Func<Task<bool>> condition,
        string description,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for {description} within {StartupTimeout}.");
    }

    private static void ThrowIfExited(Process process, ProcessDiagnostics diagnostics)
    {
        if (process.HasExited)
        {
            throw new InvalidOperationException(
                $"{diagnostics.Name} process {process.Id} exited with code {process.ExitCode}.{Environment.NewLine}{diagnostics.Format(process)}");
        }
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

        Assert.That(process.HasExited, Is.True, $"Owned process {process.Id} must exit during cleanup.");
    }

    private static async Task AssertProcessExitedAsync(int processId)
    {
        try
        {
            using var timeout = new CancellationTokenSource(ShutdownTimeout);
            await PollAsync(
                () => Task.FromResult(!IsProcessRunning(processId)),
                $"Firefox process {processId} to exit",
                timeout.Token);
        }
        catch
        {
            using var process = Process.GetProcessById(processId);
            process.Kill(entireProcessTree: true);
            using var timeout = new CancellationTokenSource(ShutdownTimeout);
            await process.WaitForExitAsync(timeout.Token);
            throw;
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

    private static Task DeleteDirectoryAsync(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Assert.That(Directory.Exists(path), Is.False, $"Temporary profile root '{path}' must be removed.");
        return Task.CompletedTask;
    }

    private static Task AssertPortsReleasedAsync(params int[] ports)
    {
        foreach (var port in ports)
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
        }

        return Task.CompletedTask;
    }

    private static async Task<Exception?> CaptureCleanupFailureAsync(
        Func<Task> cleanup,
        Exception? existingFailure)
    {
        try
        {
            await cleanup();
            return existingFailure;
        }
        catch (Exception exception)
        {
            return existingFailure is null
                ? exception
                : new AggregateException(existingFailure, exception);
        }
    }

    private static string FormatDiagnostics(
        Process? apiProcess,
        ProcessDiagnostics apiDiagnostics,
        Process? driverProcess,
        ProcessDiagnostics driverDiagnostics,
        StaticClientHost? clientHost)
    {
        var builder = new StringBuilder();
        builder.AppendLine(apiProcess is null ? "API was not started." : apiDiagnostics.Format(apiProcess));
        builder.AppendLine(driverProcess is null ? "geckodriver was not started." : driverDiagnostics.Format(driverProcess));
        builder.AppendLine(clientHost is null ? "Client host was not started." : clientHost.FormatDiagnostics());
        return builder.ToString();
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

    private sealed record BrowserInputs(
        string ApiAssemblyPath,
        string ClientPublishDirectory,
        string FirefoxBinaryPath,
        string GeckodriverPath)
    {
        public static BrowserInputs Load()
        {
            var apiPath = GetRequiredFile(ApiArtifactEnvironmentVariable, "HouseholdLedger.Api.dll");
            var clientPath = GetRequiredDirectory(ClientPublishEnvironmentVariable);
            var firefoxPath = GetRequiredFile(FirefoxEnvironmentVariable, "firefox.exe");
            var geckodriverPath = GetRequiredFile(GeckodriverEnvironmentVariable, "geckodriver.exe");

            RequireApprovedSha256(FirefoxEnvironmentVariable, firefoxPath, ApprovedFirefoxSha256);
            RequireApprovedSha256(GeckodriverEnvironmentVariable, geckodriverPath, ApprovedGeckodriverSha256);
            RequirePublishedFile(clientPath, "index.html");
            RequirePublishedFile(clientPath, "appsettings.json");
            RequirePublishedFile(clientPath, Path.Combine("_framework", "blazor.webassembly.js"));
            return new BrowserInputs(apiPath, clientPath, firefoxPath, geckodriverPath);
        }

        private static string GetRequiredFile(string variableName, string exactFileName)
        {
            var path = GetNormalizedAbsolutePath(variableName);
            if (!string.Equals(Path.GetFileName(path), exactFileName, StringComparison.OrdinalIgnoreCase))
            {
                throw new AssertionException(
                    $"{variableName} must point exactly to '{exactFileName}', but was '{Path.GetFileName(path)}'.");
            }

            if (!File.Exists(path))
            {
                throw new AssertionException($"{variableName} points to a file that does not exist: '{path}'.");
            }

            return path;
        }

        private static string GetRequiredDirectory(string variableName)
        {
            var path = GetNormalizedAbsolutePath(variableName);
            if (!Directory.Exists(path))
            {
                throw new AssertionException($"{variableName} points to a directory that does not exist: '{path}'.");
            }

            return path;
        }

        private static string GetNormalizedAbsolutePath(string variableName)
        {
            var configuredPath = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                throw new AssertionException(
                    $"Set {variableName} to its audited normalized absolute artifact path before running browser E2E.");
            }

            if (!Path.IsPathFullyQualified(configuredPath))
            {
                throw new AssertionException($"{variableName} must be absolute, but was '{configuredPath}'.");
            }

            var fullPath = Path.GetFullPath(configuredPath);
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!string.Equals(configuredPath, fullPath, comparison))
            {
                throw new AssertionException(
                    $"{variableName} must be normalized. Configured '{configuredPath}', normalized '{fullPath}'.");
            }

            return fullPath;
        }

        private static void RequireApprovedSha256(
            string variableName,
            string path,
            string approvedSha256)
        {
            using var stream = File.OpenRead(path);
            var actualSha256 = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!string.Equals(actualSha256, approvedSha256, StringComparison.Ordinal))
            {
                throw new AssertionException(
                    $"{variableName} SHA-256 mismatch for '{path}'. "
                    + $"Expected '{approvedSha256}', actual '{actualSha256}'. No browser process was launched.");
            }
        }

        private static void RequirePublishedFile(string root, string relativePath)
        {
            var path = Path.Combine(root, relativePath);
            if (!File.Exists(path))
            {
                throw new AssertionException(
                    $"{ClientPublishEnvironmentVariable} is not a complete published wwwroot; missing '{relativePath}'.");
            }
        }
    }

    private sealed class StaticClientHost : IAsyncDisposable
    {
        private static readonly IReadOnlyDictionary<string, string> ContentTypes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".css"] = "text/css; charset=utf-8",
                [".dat"] = "application/octet-stream",
                [".dll"] = "application/octet-stream",
                [".html"] = "text/html; charset=utf-8",
                [".ico"] = "image/x-icon",
                [".js"] = "text/javascript; charset=utf-8",
                [".json"] = "application/json; charset=utf-8",
                [".map"] = "application/json; charset=utf-8",
                [".png"] = "image/png",
                [".svg"] = "image/svg+xml",
                [".wasm"] = "application/wasm",
                [".woff"] = "font/woff",
                [".woff2"] = "font/woff2",
            };

        private readonly string root;
        private readonly Uri origin;
        private readonly byte[] clientConfiguration;
        private readonly HttpListener listener = new();
        private readonly CancellationTokenSource cancellation = new();
        private readonly StringBuilder diagnostics = new();
        private readonly List<(string Method, string Path, int StatusCode)> responses = new();
        private Task? servingTask;

        public StaticClientHost(string root, Uri origin, Uri apiOrigin)
        {
            this.root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            this.origin = origin;
            this.clientConfiguration = JsonSerializer.SerializeToUtf8Bytes(
                new { Api = new { BaseUrl = apiOrigin.AbsoluteUri } });
            this.listener.Prefixes.Add(origin.GetLeftPart(UriPartial.Authority) + "/");
        }

        public async Task StartAsync()
        {
            this.listener.Start();
            this.servingTask = this.ServeAsync(this.cancellation.Token);
            using var client = new HttpClient { BaseAddress = this.origin, Timeout = TimeSpan.FromSeconds(2) };
            using var timeout = new CancellationTokenSource(StartupTimeout);
            await PollAsync(
                async () =>
                {
                    try
                    {
                        using var response = await client.GetAsync("/", timeout.Token);
                        return response.StatusCode == HttpStatusCode.OK;
                    }
                    catch (HttpRequestException)
                    {
                        return false;
                    }
                },
                "the static Client host",
                timeout.Token);
        }

        public string FormatDiagnostics()
        {
            lock (this.diagnostics)
            {
                return $"Client host origin={this.origin} running={this.listener.IsListening}{Environment.NewLine}{this.diagnostics}";
            }
        }

        public void AssertNoFailedCriticalRequests()
        {
            string[] failures;
            lock (this.diagnostics)
            {
                failures = this.responses
                    .Where(response => IsCriticalClientPath(response.Path))
                    .Where(response => response.StatusCode is < 200 or >= 400)
                    .Select(response => $"{response.Method} {response.Path} -> HTTP {response.StatusCode}")
                    .ToArray();
            }

            Assert.That(
                failures,
                Is.Empty,
                $"The static client host observed non-success critical responses:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        }

        public async ValueTask DisposeAsync()
        {
            this.cancellation.Cancel();
            if (this.listener.IsListening)
            {
                this.listener.Stop();
            }

            this.listener.Close();
            if (this.servingTask is not null)
            {
                await this.servingTask;
            }

            this.cancellation.Dispose();
        }

        private static bool IsCriticalClientPath(string path)
        {
            return path == "/"
                || path == "/appsettings.json"
                || path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/_framework/", StringComparison.Ordinal);
        }

        private async Task ServeAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await this.listener.GetContextAsync().WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await this.HandleAsync(context, cancellationToken);
            }
        }

        private async Task HandleAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var requestMethod = context.Request.HttpMethod;
            var requestPath = Uri.UnescapeDataString(context.Request.Url?.AbsolutePath ?? "/");
            var statusCode = (int)HttpStatusCode.InternalServerError;
            try
            {
                if (requestPath.Equals("/appsettings.json", StringComparison.OrdinalIgnoreCase))
                {
                    statusCode = (int)HttpStatusCode.OK;
                    await this.WriteResponseAsync(
                        context.Response,
                        "application/json; charset=utf-8",
                        cancellationToken);
                    return;
                }

                var relativePath = requestPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                if (relativePath.Length == 0 || !Path.HasExtension(relativePath))
                {
                    relativePath = "index.html";
                }

                var filePath = Path.GetFullPath(Path.Combine(this.root, relativePath));
                var comparison = OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                if (!filePath.StartsWith(this.root + Path.DirectorySeparatorChar, comparison)
                    || !File.Exists(filePath))
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    context.Response.StatusCode = statusCode;
                    context.Response.Close();
                    return;
                }

                var contentType = ContentTypes.GetValueOrDefault(
                    Path.GetExtension(filePath),
                    "application/octet-stream");
                statusCode = (int)HttpStatusCode.OK;
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = contentType;
                context.Response.ContentLength64 = new FileInfo(filePath).Length;
                await using var file = File.OpenRead(filePath);
                await file.CopyToAsync(context.Response.OutputStream, cancellationToken);
                context.Response.Close();
            }
            catch (Exception exception)
            {
                lock (this.diagnostics)
                {
                    this.diagnostics.AppendLine(exception.ToString());
                }

                if (context.Response.OutputStream.CanWrite)
                {
                    context.Response.Abort();
                }
            }
            finally
            {
                lock (this.diagnostics)
                {
                    this.responses.Add((requestMethod, requestPath, statusCode));
                    this.diagnostics.AppendLine(
                        System.Globalization.CultureInfo.InvariantCulture,
                        $"{requestMethod} {requestPath} -> HTTP {statusCode}");
                }
            }
        }

        private async Task WriteResponseAsync(
            HttpListenerResponse response,
            string contentType,
            CancellationToken cancellationToken)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = contentType;
            response.ContentLength64 = this.clientConfiguration.Length;
            await response.OutputStream.WriteAsync(this.clientConfiguration, cancellationToken);
            response.Close();
        }
    }

    private sealed class ProcessDiagnostics(string name)
    {
        private readonly Lock sync = new();
        private readonly StringBuilder standardOutput = new();
        private readonly StringBuilder standardError = new();

        public string Name { get; } = name;

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
                return $"{this.Name} process {process.Id} is {state}.{Environment.NewLine}"
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
