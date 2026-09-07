// <copyright file="IGlobalSettingsRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Settings;

using HouseholdLedger.Domain.Settings;

/// <summary>Provides persistence for the singleton global settings.</summary>
public interface IGlobalSettingsRepository
{
    /// <summary>Gets the persisted settings row when it exists.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted settings, or <see langword="null"/> when defaults apply.</returns>
    Task<GlobalSettings?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Atomically inserts or updates only the singleton currency setting.</summary>
    /// <param name="displayCurrency">The supported display currency.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative persisted settings.</returns>
    Task<GlobalSettings> UpsertCurrencyAsync(DisplayCurrency displayCurrency, CancellationToken cancellationToken);

    /// <summary>Atomically inserts or updates only the singleton theme setting.</summary>
    /// <param name="theme">The supported workbench theme.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative persisted settings.</returns>
    Task<GlobalSettings> UpsertThemeAsync(WorkbenchTheme theme, CancellationToken cancellationToken);
}
