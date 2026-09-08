// <copyright file="SettingsPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Formatting;
using HouseholdLedger.Client.Pages;
using HouseholdLedger.Client.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>Verifies global display-currency settings behavior.</summary>
public sealed class SettingsPageTests
{
    /// <summary>Verifies each supported code uses its documented formatting culture.</summary>
    /// <param name="currencyCode">The currency code.</param>
    /// <param name="expected">The expected formatted amount.</param>
    [Theory]
    [InlineData("USD", "$82.45")]
    [InlineData("CAD", "$82.45")]
    [InlineData("EUR", "82,45 €")]
    [InlineData("GBP", "£82.45")]
    [InlineData("AUD", "$82.45")]
    [InlineData("XXX", "¤82.45")]
    public void FormatsEverySupportedCurrency(string currencyCode, string expected)
    {
        Assert.Equal(expected, MoneyFormatter.Format(82.45m, currencyCode));
    }

    /// <summary>Verifies default USD and all named options are presented.</summary>
    [Fact]
    public void DefaultsToUsdAndShowsSupportedCurrencies()
    {
        using var context = CreateContext();

        var component = context.Render<SettingsPage>();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("USD", component.Find("#display-currency").GetAttribute("value")),
            () => Assert.Equal(6, component.FindAll("#display-currency option").Count),
            () => Assert.Equal("workbench-dark", component.Find("#workbench-theme").GetAttribute("value")),
            () => Assert.Equal(2, component.FindAll("#workbench-theme option").Count),
            () => Assert.True(component.Find(".currency-settings-form button[type='submit']").HasAttribute("disabled")),
            () => Assert.True(component.Find(".theme-settings-form button[type='submit']").HasAttribute("disabled")),
            () => Assert.Contains("No unsaved changes", component.Markup, StringComparison.Ordinal)));
    }

    /// <summary>Verifies currency changes require acknowledging display-only behavior before saving.</summary>
    [Fact]
    public void SaveRequiresDisplayOnlyAcknowledgement()
    {
        using var context = CreateContext();
        var state = context.Services.GetRequiredService<GlobalSettingsState>();
        var component = context.Render<SettingsPage>();
        component.WaitForElement("#display-currency");

        component.Find("#display-currency").Change("EUR");
        component.Find(".currency-settings-form").Submit();

        Assert.Multiple(
            () => Assert.Equal("USD", state.CurrentCode),
            () => Assert.Contains("Amount values will not be converted", component.Markup, StringComparison.Ordinal),
            () => Assert.True(component.Find(".currency-settings-form button[type='submit']").HasAttribute("disabled")));

        component.Find("#confirm-currency-display").Change(true);
        component.Find(".currency-settings-form").Submit();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("EUR", state.CurrentCode),
            () => Assert.Equal("82,45 €", MoneyFormatter.Format(82.45m, state.CurrentCode)),
            () => Assert.Contains("Display currency saved as Euro", component.Markup, StringComparison.Ordinal)));
    }

    /// <summary>Verifies no currency is durable client state and a named currency can replace it.</summary>
    [Fact]
    public void SaveGenericDisplayAndRestoreNamedCurrency()
    {
        using var context = CreateContext();
        var state = context.Services.GetRequiredService<GlobalSettingsState>();
        var component = context.Render<SettingsPage>();
        component.WaitForElement("#display-currency");

        component.Find("#display-currency").Change("XXX");
        component.Find("#confirm-currency-display").Change(true);
        component.Find(".currency-settings-form").Submit();
        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("XXX", state.CurrentCode),
            () => Assert.Equal("¤1,234.50", MoneyFormatter.Format(1234.5m, state.CurrentCode)),
            () => Assert.Equal("¤0.00", MoneyFormatter.Format(0m, state.CurrentCode)),
            () => Assert.Contains("Display currency saved as No currency", component.Markup, StringComparison.Ordinal)));

        component.Find("#display-currency").Change("USD");
        component.Find("#confirm-currency-display").Change(true);
        component.Find(".currency-settings-form").Submit();
        component.WaitForAssertion(() => Assert.Equal("$1,234.50", MoneyFormatter.Format(1234.5m, state.CurrentCode)));
    }

    /// <summary>Verifies a failed save leaves the prior effective currency active.</summary>
    [Fact]
    public void FailedSaveDoesNotPublishUnsavedCurrency()
    {
        var apiClient = new StubGlobalSettingsApiClient { FailSave = true };
        using var context = CreateContext(apiClient);
        var state = context.Services.GetRequiredService<GlobalSettingsState>();
        var component = context.Render<SettingsPage>();
        component.WaitForElement("#display-currency");

        component.Find("#display-currency").Change("GBP");
        component.Find("#confirm-currency-display").Change(true);
        component.Find(".currency-settings-form").Submit();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("USD", state.CurrentCode),
            () => Assert.Contains("could not be saved", component.Markup, StringComparison.Ordinal),
            () => Assert.Equal("GBP", component.Find("#display-currency").GetAttribute("value"))));
    }

    /// <summary>Verifies a synchronous initial failure can be retried successfully.</summary>
    [Fact]
    public void FailedInitialLoadCanRetry()
    {
        var apiClient = new StubGlobalSettingsApiClient
        {
            FailLoad = true,
            LoadedCurrency = "EUR",
        };
        using var context = CreateContext(apiClient);
        var state = context.Services.GetRequiredService<GlobalSettingsState>();
        var component = context.Render<SettingsPage>();

        component.WaitForAssertion(() => Assert.Contains("unavailable", component.Markup, StringComparison.OrdinalIgnoreCase));
        apiClient.FailLoad = false;
        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal(2, apiClient.GetCallCount),
            () => Assert.Equal("EUR", state.CurrentCode),
            () => Assert.Equal("EUR", component.Find("#display-currency").GetAttribute("value"))));
    }

    /// <summary>Verifies saving Appearance does not submit an unsaved currency edit.</summary>
    [Fact]
    public void ThemeSavePublishesThemeAndPreservesUnsavedCurrencySelection()
    {
        using var context = CreateContext();
        var state = context.Services.GetRequiredService<GlobalSettingsState>();
        var component = context.Render<SettingsPage>();
        component.WaitForElement("#workbench-theme");

        component.Find("#display-currency").Change("EUR");
        component.Find("#workbench-theme").Change("workbench-light");
        component.Find(".theme-settings-form").Submit();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("USD", state.CurrentCode),
            () => Assert.Equal("workbench-light", state.CurrentTheme),
            () => Assert.Equal("EUR", component.Find("#display-currency").GetAttribute("value")),
            () => Assert.True(component.Find(".currency-settings-form button").HasAttribute("disabled")),
            () => Assert.Contains("Amount values will not be converted", component.Markup, StringComparison.Ordinal),
            () => Assert.Contains("Theme saved as Workbench Light", component.Markup, StringComparison.Ordinal)));
    }

    private static BunitContext CreateContext(StubGlobalSettingsApiClient? apiClient = null)
    {
        var context = new BunitContext();
        apiClient ??= new StubGlobalSettingsApiClient();
        context.Services.AddSingleton<IGlobalSettingsApiClient>(apiClient);
        context.Services.AddSingleton<GlobalSettingsState>();
        return context;
    }

    private sealed class StubGlobalSettingsApiClient : IGlobalSettingsApiClient
    {
        public bool FailLoad { get; set; }

        public bool FailSave { get; init; }

        public int GetCallCount { get; private set; }

        public string LoadedCurrency { get; init; } = "USD";

        public string LoadedTheme { get; init; } = "workbench-dark";

        private string CurrentCurrency { get; set; } = "USD";

        private string CurrentTheme { get; set; } = "workbench-dark";

        public Task<GlobalSettingsResponse> GetAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.GetCallCount++;
            return this.FailLoad
                ? Task.FromException<GlobalSettingsResponse>(new HttpRequestException("Unavailable"))
                : Task.FromResult(new GlobalSettingsResponse(
                    this.CurrentCurrency = this.LoadedCurrency,
                    this.CurrentTheme = this.LoadedTheme));
        }

        public Task<GlobalSettingsResponse> UpdateCurrencyAsync(
            string displayCurrency,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (this.FailSave)
            {
                return Task.FromException<GlobalSettingsResponse>(new HttpRequestException("Unavailable"));
            }

            this.CurrentCurrency = displayCurrency;
            return Task.FromResult(new GlobalSettingsResponse(this.CurrentCurrency, this.CurrentTheme));
        }

        public Task<GlobalSettingsResponse> UpdateThemeAsync(
            string theme,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (this.FailSave)
            {
                return Task.FromException<GlobalSettingsResponse>(new HttpRequestException("Unavailable"));
            }

            this.CurrentTheme = theme;
            return Task.FromResult(new GlobalSettingsResponse(this.CurrentCurrency, this.CurrentTheme));
        }
    }
}
