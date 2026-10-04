// <copyright file="BrowserFreshSetupJourneyTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.EndToEndTests;

using System.Diagnostics;
using Xunit;

/// <summary>Exercises the user's fresh household setup across browser and persistence boundaries.</summary>
public sealed partial class BrowserCalendarJourneyTests
{
    /// <summary>
    /// Proves fresh UI setup persists without mistaking schedule expectations or account destinations for actual income or savings.
    /// </summary>
    /// <returns>A task representing the browser journey.</returns>
    [Fact]
    public async Task FreshSetupPersistsAccountsAndExpectationsAndReviewsOnlyExplicitlyConfirmedIncome()
    {
        Assert.Equal("postgres18-disposable", Environment.GetEnvironmentVariable("HOUSEHOLDLEDGER_FRESH_SETUP_DATABASE"));
        var today = DateOnly.FromDateTime(DateTime.Now);
        var planMonth = new DateOnly(today.Year, today.Month, 1);
        var expectationMonth = planMonth.AddMonths(1);
        var firstExpectation = expectationMonth.AddDays(8);
        var secondExpectation = firstExpectation.AddDays(14);

        // Forty-two days keeps the anchor in a prior month even when today is the first of a month.
        var anchor = firstExpectation.AddDays(-42);
        var expenseDate = planMonth;
        var planPath = $"/months/{planMonth.Year}/{planMonth.Month}";
        var receiptMonthPath = $"/months/{anchor.Year}/{anchor.Month}";
        var receiptPath = $"/income-receipts/{anchor.Year}/{anchor.Month}/{anchor.Day}";
        var inputs = BrowserInputs.Load();
        var origin = new Uri($"https://localhost:{inputs.ApiPort}");
        var driverPort = ReserveLoopbackPort(inputs.ApiPort);
        var profile = Path.Combine(inputs.ProfileRoot, $"fresh-{Guid.NewGuid():N}");
        Process? api = null;
        Process? driver = null;
        W3cWebDriver? browser = null;
        int? firefoxPid = null;
        Directory.CreateDirectory(profile);
        try
        {
            // Production deliberately excludes User Secrets; the runner supplies only its disposable database.
            var start = CreateStartInfo(FindDotNetHost(), Path.GetDirectoryName(inputs.ApiAssemblyPath)!);
            start.ArgumentList.Add(inputs.ApiAssemblyPath);
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
            start.Environment["DOTNET_ENVIRONMENT"] = "Production";
            start.Environment["ASPNETCORE_URLS"] = origin.AbsoluteUri;
            start.Environment["ConnectionStrings__HouseholdLedger"] = inputs.PostgreSqlConnectionString;
            api = StartProcess(start);
            await WaitForOkAsync(origin, "/api/v1/health", api);
            driver = StartGeckodriver(inputs.GeckodriverPath, driverPort);
            browser = new W3cWebDriver(new Uri($"http://127.0.0.1:{driverPort}"));
            await WaitForDriverAsync(browser, driver);
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var token = timeout.Token;
            var capabilities = await browser.CreateSessionAsync(inputs.FirefoxBinaryPath, profile, CreateFirefoxPreferences(), token);
            AssertRuntimeCapabilities(capabilities);
            firefoxPid = capabilities.GetProperty("moz:processID").GetInt32();
            await SetDesktopViewportAsync(browser, token);

            await browser.NavigateAsync(new Uri(origin, "/accounts"), token);
            await WaitForElementAsync(browser, "#account-name", token);
            await WaitForTextAsync(browser, ".accounts-load-state", "No accounts yet.", token);
            string[] names = ["BoA Checking", "CapOne Checking", "UHCU Checking", "BoA Savings", "UHCU Savings"];
            foreach (var name in names)
            {
                await SetFormValueAsync(browser, "#account-name", name, "input", token);
                await FreshClickAsync(browser, ".new-account-form button[type=submit]", token);
                await WaitForAccountAsync(browser, name, token);
            }

            await browser.NavigateAsync(new Uri(origin, "/accounts"), token);
            await WaitForTextAsync(browser, ".accounts-gallery", names[^1], token);
            var accountNames = await browser.ExecuteScriptAsync(
                "return [...document.querySelectorAll('article.account-card h2')].map(x => x.textContent.trim()).sort();", null, token);
            Assert.Equal(names.Order().ToArray(), accountNames.EnumerateArray().Select(x => x.GetString()).ToArray());

            await browser.NavigateAsync(new Uri(origin, "/pay-schedules"), token);
            await WaitForElementAsync(browser, "#schedule-name", token);
            await WaitForTextAsync(browser, ".schedule-list-section", "No pay schedules yet.", token);
            await SetFormValueAsync(browser, "#schedule-name", "Biweekly take-home", "input", token);
            await SetFormValueAsync(browser, "#schedule-first-date", anchor.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), "change", token);
            await SetSelectOptionByTextAsync(browser, "#schedule-cadence", "Biweekly", token);
            await SetFormValueAsync(browser, "#schedule-net-income", "2750", "change", token);
            await FreshFillAllocationsAsync(browser, ".schedule-form", "change", token);
            await FreshClickAsync(browser, ".schedule-form button[type=submit]", token);
            await WaitForTextAsync(browser, ".schedule-list", "Biweekly take-home", token);

            // A full document navigation reloads the persisted schedule rather than keeping component state.
            await browser.NavigateAsync(new Uri(origin, "/pay-schedules"), token);
            await WaitForTextAsync(browser, ".schedule-list", "Biweekly take-home", token);
            await WaitForTextAsync(browser, ".schedule-list", "Biweekly", token);
            await WaitForTextAsync(browser, ".schedule-list", "2,750.00", token);
            await WaitForTextAsync(browser, ".schedule-list", "3 allocations", token);
            await browser.ExecuteScriptAsync(
                "const button = [...document.querySelectorAll('.schedule-actions button')].find(x => x.textContent.trim() === 'Edit'); if (!button) throw new Error('Missing Edit'); button.click();", null, token);
            await WaitForTextAsync(browser, "#schedule-form-heading", "Edit pay schedule", token);
            Assert.Equal(anchor.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), (await browser.ExecuteScriptAsync("return document.querySelector('#schedule-first-date').value;", null, token)).GetString());
            await FreshAssertAllocationsAsync(browser, ".schedule-form", token);

            await FreshOpenMonthCalendarAsync(browser, origin, expectationMonth, token);
            foreach (var date in new[] { firstExpectation, secondExpectation })
            {
                await FreshSelectDayAsync(browser, date, token);
                await FreshClickAsync(browser, "details.pending-income-state summary", token);
                await WaitForTextAsync(browser, ".pending-income-list", "Biweekly take-home", token);
                await WaitForTextAsync(browser, ".pending-income-list", "BoA Checking: $1,000.00", token);
                await WaitForTextAsync(browser, ".pending-income-list", "CapOne Checking: $1,500.00", token);
                await WaitForTextAsync(browser, ".pending-income-list", "UHCU Checking: $250.00", token);
                Assert.Equal(0, (await browser.ExecuteScriptAsync("return document.querySelectorAll('#completed-income-heading').length;", null, token)).GetInt32());
            }

            await FreshSelectDayAsync(browser, firstExpectation.AddDays(-1), token);
            Assert.Equal(0, (await browser.ExecuteScriptAsync("return document.querySelectorAll('details.pending-income-state').length;", null, token)).GetInt32());
            await browser.NavigateAsync(new Uri(origin, $"/months/{expectationMonth.Year}/{expectationMonth.Month}"), token);
            await FreshAssertReviewTotalAsync(browser, "Income recorded so far (confirmed)", "$0.00", token);

            // Invalid reconciliation must not enable save; a valid plan persists and still shows no actual income.
            await browser.NavigateAsync(new Uri(origin, planPath), token);
            await WaitForElementAsync(browser, "#expected-income", token);
            string[] fields = ["expected-income", "intended-savings", "necessities", "optional", "culture", "unexpected"];
            string[] amounts = ["5500", "500", "3500", "750", "250", "501"];
            for (var index = 0; index < fields.Length; index++)
            {
                await SetFormValueAsync(browser, $"#{fields[index]}", amounts[index], "input", token);
            }

            await WaitForTextAsync(browser, "#plan-reconciliation", "Allocate exactly", token);
            Assert.True((await browser.ExecuteScriptAsync("return document.querySelector('.monthly-budget-page button[type=submit]').disabled;", null, token)).GetBoolean());
            await WaitForTextAsync(browser, "#plan-heading", "Create a monthly plan", token);
            amounts[^1] = "500";
            for (var index = 0; index < fields.Length; index++)
            {
                await SetFormValueAsync(browser, $"#{fields[index]}", amounts[index], "input", token);
            }

            await WaitForTextAsync(browser, "#plan-reconciliation", "reconciles exactly", token);
            await FreshClickAsync(browser, ".monthly-budget-page button[type=submit]", token);
            await WaitForTextAsync(browser, "#plan-heading", "Revise your monthly plan", token);
            await browser.NavigateAsync(new Uri(origin, planPath), token);
            await WaitForTextAsync(browser, "#plan-heading", "Revise your monthly plan", token);
            for (var index = 0; index < fields.Length; index++)
            {
                Assert.Equal(
                    decimal.Parse(amounts[index], System.Globalization.CultureInfo.InvariantCulture),
                    decimal.Parse((await browser.ExecuteScriptAsync("return document.querySelector(arguments[0]).value;", [$"#{fields[index]}"], token)).GetString()!, System.Globalization.CultureInfo.InvariantCulture));
            }

            await FreshAssertReviewTotalAsync(browser, "Expected take-home income", "$5,500.00", token);
            await FreshAssertReviewTotalAsync(browser, "Intended savings (target, not actual savings)", "$500.00", token);
            await FreshAssertReviewTotalAsync(browser, "Income recorded so far (confirmed)", "$0.00", token);
            await FreshAssertReviewTotalAsync(browser, "Income variance (actual minus expected)", "-$5,500.00", token);
            var spending = await browser.ExecuteScriptAsync("return [...document.querySelectorAll('.monthly-budget-page tbody tr')].map(x => [...x.children].map(c => c.textContent.trim()));", null, token);
            Assert.Equal(4, spending.GetArrayLength());
            foreach (var row in spending.EnumerateArray())
            {
                var planned = row[0].GetString() switch
                {
                    "Necessities" => "$3,500.00",
                    "Optional" => "$750.00",
                    "Culture" => "$250.00",
                    "Unexpected" => "$500.00",
                    _ => throw new InvalidOperationException($"Unexpected classification: {row[0]}"),
                };
                Assert.Equal(planned, row[1].GetString());
                Assert.Equal("$0.00", row[2].GetString());
                Assert.Equal($"-{planned}", row[3].GetString());
            }

            await FreshOpenMonthCalendarAsync(browser, origin, anchor, token);
            await FreshSelectDayAsync(browser, anchor, token);
            await WaitForElementAsync(browser, $"a[href='{receiptPath}']", token);
            await FreshClickAsync(browser, $"a[href='{receiptPath}']", token);
            await WaitForElementAsync(browser, "#received-amount", token);
            await SetFormValueAsync(browser, "#received-date", anchor.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), "change", token);
            await SetFormValueAsync(browser, "#received-amount", "2750", "input", token);
            await FreshFillAllocationsAsync(browser, ".confirm-income-page", "input", token);
            await FreshClickAsync(browser, "#receipt-confirmation", token);
            await FreshClickAsync(browser, ".confirm-income-page button[type=submit]", token);
            await WaitForElementAsync(browser, $"a[href='{receiptMonthPath}']", token);
            await browser.NavigateAsync(new Uri(origin, receiptMonthPath), token);
            await FreshAssertReviewTotalAsync(browser, "Income recorded so far (confirmed)", "$2,750.00", token);
            await FreshAssertReviewTotalAsync(browser, "Unallocated remainder (confirmed income remaining after recorded expenses)", "$2,750.00", token);
            await FreshOpenMonthCalendarAsync(browser, origin, anchor, token);
            await FreshSelectDayAsync(browser, anchor, token);
            await WaitForTextAsync(browser, ".income-receipt-list:not(.pending-income-list)", "2,750.00", token);
            await WaitForTextAsync(browser, ".income-receipt-list:not(.pending-income-list)", "BoA Checking: $1,000.00", token);
            await WaitForTextAsync(browser, ".income-receipt-list:not(.pending-income-list)", "CapOne Checking: $1,500.00", token);
            await WaitForTextAsync(browser, ".income-receipt-list:not(.pending-income-list)", "UHCU Checking: $250.00", token);
            var receiptState = await browser.ExecuteScriptAsync(
                "const list = document.querySelector('.income-receipt-list:not(.pending-income-list)'); return { count: list.children.length, destinations: [...list.querySelectorAll('ul li')].map(x => x.textContent.trim()) };", null, token);
            Assert.Equal(1, receiptState.GetProperty("count").GetInt32());
            Assert.Equal(3, receiptState.GetProperty("destinations").GetArrayLength());
            Assert.DoesNotContain("Savings", receiptState.ToString(), StringComparison.Ordinal);
            await browser.ExecuteScriptAsync("const button = [...document.querySelectorAll('.income-receipts button')].find(x => x.textContent.trim() === 'Correct'); button.click();", null, token);
            await WaitForElementAsync(browser, "form.income-edit-form", token);
            await SetFormValueAsync(browser, "input[id^='income-amount-']", "2751", "input", token);
            await browser.ExecuteScriptAsync("const form = document.querySelector('form.income-edit-form'); const index = [...form.querySelectorAll('select')].findIndex(x => x.selectedOptions[0].textContent.trim() === 'BoA Checking'); if (index < 0) throw new Error('Missing BoA allocation'); const input = form.querySelectorAll('input[id^=\"income-allocation-\"]')[index]; input.value = '1001'; input.dispatchEvent(new Event('input', { bubbles: true }));", null, token);
            await FreshClickAsync(browser, "form.income-edit-form button[type=submit]", token);
            await WaitForTextAsync(browser, ".income-receipt-list", "2,751.00", token);
            await FreshOpenMonthCalendarAsync(browser, origin, anchor, token);
            await FreshSelectDayAsync(browser, anchor, token);
            await WaitForTextAsync(browser, ".income-receipt-list", "2,751.00", token);
            await browser.ExecuteScriptAsync("const button = [...document.querySelectorAll('.income-receipts button')].find(x => x.textContent.trim() === 'Remove'); button.click();", null, token);
            await WaitForTextAsync(browser, ".income-receipts", "Remove receipt", token);
            await browser.ExecuteScriptAsync("const button = [...document.querySelectorAll('.income-receipts button')].find(x => x.textContent.trim() === 'Remove receipt'); button.click();", null, token);
            while ((await browser.ExecuteScriptAsync("return document.querySelectorAll('.income-receipt-list:not(.pending-income-list)').length;", null, token)).GetInt32() != 0)
            {
                await Task.Delay(50, token);
            }

            await browser.NavigateAsync(new Uri(origin, receiptMonthPath), token);
            await FreshAssertReviewTotalAsync(browser, "Income recorded so far (confirmed)", "$0.00", token);
            await browser.NavigateAsync(new Uri(origin, planPath), token);
            await FreshAssertReviewTotalAsync(browser, "Income recorded so far (confirmed)", "$0.00", token);
            await WaitForTextAsync(browser, "#plan-heading", "Revise your monthly plan", token);

            await FreshOpenMonthCalendarAsync(browser, origin, planMonth, token);
            await FreshSelectDayAsync(browser, expenseDate, token);
            var focusedDay = await browser.FindElementAsync(".calendar-day[tabindex='0']", token);
            await browser.SendKeysAsync(focusedDay, "\uE004", token);
            Assert.False((await browser.ExecuteScriptAsync("return document.activeElement.classList.contains('calendar-day');", null, token)).GetBoolean());
            await WaitForTextAsync(browser, "#selected-day-heading", expenseDate.ToString("dddd, MMMM d, yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US")), token);
            await WaitForElementAsync(browser, "#transaction-account", token);
            await SetSelectOptionByTextAsync(browser, "#transaction-account", "BoA Checking", token);
            await SetFormValueAsync(browser, "#transaction-amount", "82.45", "input", token);
            await SetFormValueAsync(browser, "#transaction-classification", "Necessities", "change", token);
            await SetFormValueAsync(browser, "#transaction-description", "Weekly groceries", "input", token);
            await ClickButtonByTextAsync(browser, "Save expense", token);
            await WaitForTransactionSummaryAsync(browser, "BoA Checking", "Necessities", "$82.45", token);
            await WaitForSelectedCalendarSummaryAsync(browser, "$82.45", token);
            await WaitForTextAsync(browser, ".transaction-inspector", "Weekly groceries", token);

            await FreshOpenMonthCalendarAsync(browser, origin, planMonth, token);
            await FreshSelectDayAsync(browser, expenseDate, token);
            await WaitForTransactionSummaryAsync(browser, "BoA Checking", "Necessities", "$82.45", token);
            await WaitForSelectedCalendarSummaryAsync(browser, "$82.45", token);
            await browser.NavigateAsync(new Uri(origin, "/accounts"), token);
            await WaitForTextAsync(browser, ".accounts-gallery", "BoA Checking", token);
            await ClickAccountLinkAsync(browser, "BoA Checking", token);
            await WaitForTextAsync(browser, ".account-history .result-count", "1 transaction", token);
            var expenseRows = await browser.ExecuteScriptAsync(
                "return [...document.querySelectorAll('.account-history tbody tr')].map(x => [...x.children].map(c => c.textContent.trim()));", null, token);
            Assert.Equal(1, expenseRows.GetArrayLength());
            Assert.Equal(expenseDate.ToString("MMM d, yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US")), expenseRows[0][0].GetString());
            Assert.Equal("$82.45", expenseRows[0][1].GetString());
            Assert.Equal("Necessities", expenseRows[0][2].GetString());
            Assert.Equal("Weekly groceries", expenseRows[0][3].GetString());
            Assert.Equal(1, (await browser.ExecuteScriptAsync("return document.querySelectorAll('article.account-card').length;", null, token)).GetInt32());

            await browser.NavigateAsync(new Uri(origin, planPath), token);
            await FreshAssertReviewTotalAsync(browser, "Income recorded so far (confirmed)", "$0.00", token);
            await FreshAssertReviewTotalAsync(browser, "Recorded cashflow (income minus recorded expenses)", "-$82.45", token);
            await FreshAssertReviewTotalAsync(browser, "Unallocated remainder (confirmed income remaining after recorded expenses)", "$0.00", token);
            await FreshAssertReviewTotalAsync(browser, "Intended savings (target, not actual savings)", "$500.00", token);
            var necessities = await browser.ExecuteScriptAsync(
                "const row = [...document.querySelectorAll('.monthly-budget-page tbody tr')].find(x => x.children[0].textContent.trim() === 'Necessities'); return [...row.children].map(x => x.textContent.trim());", null, token);
            Assert.Equal("Necessities", necessities[0].GetString());
            Assert.Equal("$3,500.00", necessities[1].GetString());
            Assert.Equal("$82.45", necessities[2].GetString());
            Assert.Equal("-$3,417.55", necessities[3].GetString());
            await WaitForTextAsync(browser, "#plan-heading", "Revise your monthly plan", token);

            await SetFormValueAsync(browser, "#reflection-worked", "Recording groceries helped me notice spending.", "input", token);
            await SetFormValueAsync(browser, "#reflection-intention", "Review before optional purchases.", "input", token);
            await FreshClickAsync(browser, "form.reflection-form button[type=submit]", token);
            await WaitForTextAsync(browser, ".monthly-budget-page", "Monthly reflection saved separately from the spending plan.", token);
            await browser.NavigateAsync(new Uri(origin, planPath), token);
            await WaitForElementAsync(browser, "#reflection-worked", token);
            Assert.Equal("Recording groceries helped me notice spending.", (await browser.ExecuteScriptAsync("return document.querySelector('#reflection-worked').value;", null, token)).GetString());
            Assert.Equal("Review before optional purchases.", (await browser.ExecuteScriptAsync("return document.querySelector('#reflection-intention').value;", null, token)).GetString());
            await SetFormValueAsync(browser, "#necessities", "3499", "input", token);
            await FreshClickAsync(browser, ".monthly-budget-page a[href='/']", token);
            await browser.DismissAlertAsync(token);
            Assert.Equal("3499", (await browser.ExecuteScriptAsync("return document.querySelector('#necessities').value;", null, token)).GetString());
            await SetFormValueAsync(browser, "#necessities", "3500.00", "input", token);

            foreach (var name in names.TakeLast(2))
            {
                await browser.NavigateAsync(new Uri(origin, "/accounts"), token);
                await WaitForTextAsync(browser, ".accounts-gallery", name, token);
                await ClickAccountLinkAsync(browser, name, token);
                await WaitForTextAsync(browser, ".account-history", "No transactions recorded for this account.", token);
                await WaitForTextAsync(browser, ".account-history .result-count", "0 transactions", token);
            }

            TestContext.Current.TestOutputHelper?.WriteLine($"Fresh setup verified via pinned Firefox: api={origin} apiPid={api.Id} driverPid={driver.Id} firefoxPid={firefoxPid}; UI-only writes; anchor/receipt={anchor:yyyy-MM-dd} expected={firstExpectation:yyyy-MM-dd},{secondExpectation:yyyy-MM-dd} expense={expenseDate:yyyy-MM-dd}; receipt confirmed, corrected, removed; plan-month actual income 0, plan 5500, described expense 82.45; reflection persisted.");
        }
        catch
        {
            if (browser is not null)
            {
                TestContext.Current.TestOutputHelper?.WriteLine(
                    (await browser.ExecuteScriptAsync("return document.body.innerText;", null, CancellationToken.None)).GetString() ?? "No browser text.");
            }

            throw;
        }
        finally
        {
            if (browser is not null)
            {
                await browser.DisposeAsync();
            }

            if (driver is not null)
            {
                await StopOwnedProcessAsync(driver);
                driver.Dispose();
            }

            if (api is not null)
            {
                await StopOwnedProcessAsync(api);
                api.Dispose();
            }

            if (firefoxPid is not null)
            {
                await AssertProcessExitedAsync(firefoxPid.Value);
            }

            DeleteDirectory(profile);
            AssertPortsReleased(inputs.ApiPort, driverPort);
        }
    }

    private static async Task FreshClickAsync(W3cWebDriver browser, string selector, CancellationToken token)
    {
        await browser.ClickAsync(await browser.FindElementAsync(selector, token), token);
    }

    private static async Task FreshFillAllocationsAsync(W3cWebDriver browser, string scope, string amountEvent, CancellationToken token)
    {
        for (var count = 1; count < 3; count++)
        {
            await browser.ExecuteScriptAsync("const button = [...document.querySelectorAll(arguments[0] + ' button')].find(x => x.textContent.trim() === 'Add allocation'); button.click();", [scope], token);
        }

        string[] names = ["BoA Checking", "CapOne Checking", "UHCU Checking"];
        string[] amounts = ["1000", "1500", "250"];
        for (var index = 0; index < names.Length; index++)
        {
            await SetSelectOptionByTextAsync(browser, $"{scope} .allocation-row:nth-of-type({index + 1}) select", names[index], token);
            await SetFormValueAsync(browser, $"{scope} .allocation-row:nth-of-type({index + 1}) input", amounts[index], amountEvent, token);
        }
    }

    private static async Task FreshAssertAllocationsAsync(W3cWebDriver browser, string scope, CancellationToken token)
    {
        var rows = await browser.ExecuteScriptAsync("return [...document.querySelectorAll(arguments[0] + ' .allocation-row')].map(x => ({ name: x.querySelector('select').selectedOptions[0].textContent.trim(), amount: Number(x.querySelector('input').value) }));", [scope], token);
        Assert.Equal(3, rows.GetArrayLength());
        string[] names = ["BoA Checking", "CapOne Checking", "UHCU Checking"];
        decimal[] amounts = [1000, 1500, 250];
        for (var index = 0; index < names.Length; index++)
        {
            var row = Assert.Single(rows.EnumerateArray(), row => row.GetProperty("name").GetString() == names[index]);
            Assert.Equal(amounts[index], row.GetProperty("amount").GetDecimal());
        }
    }

    private static async Task FreshOpenMonthCalendarAsync(W3cWebDriver browser, Uri origin, DateOnly month, CancellationToken token)
    {
        await browser.NavigateAsync(new Uri(origin, $"/months/{month.Year}/{month.Month}"), token);
        await WaitForElementAsync(browser, "#expected-income", token);
        await FreshClickAsync(browser, ".monthly-budget-page a[href='/']", token);
        await WaitForElementAsync(browser, "#calendar-period-heading", token);
        var heading = (await browser.ExecuteScriptAsync("return document.querySelector('#calendar-period-heading').textContent.trim();", null, token)).GetString()!;
        var displayedMonth = DateTime.Parse(heading, System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        var target = new DateTime(month.Year, month.Month, 1);
        while (displayedMonth.Year != target.Year || displayedMonth.Month != target.Month)
        {
            var forward = displayedMonth < target;
            await FreshClickAsync(browser, forward ? "button[aria-label='Next period']" : "button[aria-label='Previous period']", token);
            displayedMonth = displayedMonth.AddMonths(forward ? 1 : -1);
            await WaitForTextAsync(browser, "#calendar-period-heading", displayedMonth.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US")), token);
        }
    }

    private static async Task FreshSelectDayAsync(W3cWebDriver browser, DateOnly date, CancellationToken token)
    {
        await browser.ExecuteScriptAsync("const day = [...document.querySelectorAll('.calendar-day')].find(x => x.querySelector('.calendar-day-date-number')?.textContent.trim() === String(arguments[0])); if (!day) throw new Error('Missing calendar day'); day.click();", [date.Day], token);
        await WaitForTextAsync(browser, "#selected-day-heading", date.ToString("dddd, MMMM d, yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US")), token);
    }

    private static async Task FreshAssertReviewTotalAsync(W3cWebDriver browser, string label, string expected, CancellationToken token)
    {
        await WaitForElementAsync(browser, ".review-totals", token);
        var actual = await browser.ExecuteScriptAsync("return [...document.querySelectorAll('.review-totals dt')].find(x => x.textContent.trim() === arguments[0])?.nextElementSibling.textContent.trim();", [label], token);
        Assert.Equal(expected, actual.GetString());
    }
}
