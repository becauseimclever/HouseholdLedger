// <copyright file="GlobalSettingsService.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Settings;

using HouseholdLedger.Domain.Settings;

/// <summary>Reads and updates application-wide settings.</summary>
public sealed class GlobalSettingsService(IGlobalSettingsRepository repository)
{
    /// <summary>Gets persisted settings or the default USD setting.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative effective settings.</returns>
    public async Task<GlobalSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await repository.GetAsync(cancellationToken);
        return new(
            settings?.DisplayCurrency ?? DisplayCurrency.USD,
            settings?.Theme ?? WorkbenchTheme.WorkbenchDark);
    }

    /// <summary>Persists a supported display currency.</summary>
    /// <param name="displayCurrency">The supported display currency.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative saved settings.</returns>
    public async Task<GlobalSettingsDto> UpdateCurrencyAsync(
        DisplayCurrency displayCurrency,
        CancellationToken cancellationToken)
    {
        _ = new GlobalSettings(displayCurrency);
        var settings = await repository.UpsertCurrencyAsync(displayCurrency, cancellationToken);
        return new(settings.DisplayCurrency, settings.Theme);
    }

    /// <summary>Persists a supported workbench theme without changing currency.</summary>
    /// <param name="theme">The supported workbench theme.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative saved settings.</returns>
    public async Task<GlobalSettingsDto> UpdateThemeAsync(
        WorkbenchTheme theme,
        CancellationToken cancellationToken)
    {
        _ = new GlobalSettings(DisplayCurrency.USD, theme);
        var settings = await repository.UpsertThemeAsync(theme, cancellationToken);
        return new(settings.DisplayCurrency, settings.Theme);
    }
}
