// <copyright file="GlobalSettingsServiceTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.UnitTests;

using HouseholdLedger.Application.Settings;
using HouseholdLedger.Domain.Settings;
using Xunit;

/// <summary>Verifies global settings application behavior.</summary>
public sealed class GlobalSettingsServiceTests
{
    /// <summary>Verifies absence of a persisted row uses USD without writing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task MissingSettingsUseUsdWithoutPersistence()
    {
        var repository = new StubGlobalSettingsRepository();
        var service = new GlobalSettingsService(repository);

        var result = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(DisplayCurrency.USD, result.DisplayCurrency),
            () => Assert.Equal(WorkbenchTheme.WorkbenchDark, result.Theme),
            () => Assert.Equal(0, repository.UpsertCallCount));
    }

    /// <summary>Verifies a supported currency is persisted and returned.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SupportedCurrencyIsPersistedAndReturned()
    {
        var repository = new StubGlobalSettingsRepository();
        var service = new GlobalSettingsService(repository);

        var result = await service.UpdateCurrencyAsync(DisplayCurrency.EUR, TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(DisplayCurrency.EUR, result.DisplayCurrency),
            () => Assert.Equal(1, repository.UpsertCallCount));
    }

    /// <summary>Verifies the explicit no-currency value is persisted distinctly from the default.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GenericDisplayIsPersistedAndReturned()
    {
        var repository = new StubGlobalSettingsRepository();
        var service = new GlobalSettingsService(repository);

        var result = await service.UpdateCurrencyAsync(DisplayCurrency.XXX, TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(DisplayCurrency.XXX, result.DisplayCurrency),
            () => Assert.Equal(1, repository.UpsertCallCount));
    }

    /// <summary>Verifies an undefined currency is rejected before persistence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UnsupportedCurrencyIsRejectedBeforePersistence()
    {
        var repository = new StubGlobalSettingsRepository();
        var service = new GlobalSettingsService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.UpdateCurrencyAsync((DisplayCurrency)999, TestContext.Current.CancellationToken));
        Assert.Equal(0, repository.UpsertCallCount);
    }

    /// <summary>Verifies theme updates preserve the persisted currency.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ThemeUpdatePreservesCurrency()
    {
        var repository = new StubGlobalSettingsRepository();
        var service = new GlobalSettingsService(repository);
        await service.UpdateCurrencyAsync(DisplayCurrency.EUR, TestContext.Current.CancellationToken);

        var result = await service.UpdateThemeAsync(
            WorkbenchTheme.WorkbenchLight,
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(DisplayCurrency.EUR, result.DisplayCurrency),
            () => Assert.Equal(WorkbenchTheme.WorkbenchLight, result.Theme),
            () => Assert.Equal(2, repository.UpsertCallCount));
    }

    private sealed class StubGlobalSettingsRepository : IGlobalSettingsRepository
    {
        private GlobalSettings? settings;

        public int UpsertCallCount { get; private set; }

        public Task<GlobalSettings?> GetAsync(CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult(this.settings);
        }

        public Task<GlobalSettings> UpsertCurrencyAsync(
            DisplayCurrency displayCurrency,
            CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            this.UpsertCallCount++;
            this.settings ??= new GlobalSettings(displayCurrency);
            this.settings.ChangeDisplayCurrency(displayCurrency);
            return Task.FromResult(this.settings);
        }

        public Task<GlobalSettings> UpsertThemeAsync(
            WorkbenchTheme theme,
            CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            this.UpsertCallCount++;
            this.settings ??= new GlobalSettings(DisplayCurrency.USD, theme);
            this.settings.ChangeTheme(theme);
            return Task.FromResult(this.settings);
        }
    }
}
