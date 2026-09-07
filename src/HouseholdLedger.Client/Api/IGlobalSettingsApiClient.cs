// <copyright file="IGlobalSettingsApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using HouseholdLedger.Api.Contracts;

/// <summary>Reads and updates authoritative global application settings.</summary>
public interface IGlobalSettingsApiClient
{
    /// <summary>Gets the effective global settings.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The effective settings.</returns>
    Task<GlobalSettingsResponse> GetAsync(CancellationToken cancellationToken);

    /// <summary>Updates the global display currency.</summary>
    /// <param name="displayCurrency">The supported currency code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative saved settings.</returns>
    Task<GlobalSettingsResponse> UpdateCurrencyAsync(string displayCurrency, CancellationToken cancellationToken);

    /// <summary>Updates the global workbench theme.</summary>
    /// <param name="theme">The supported theme identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative saved settings.</returns>
    Task<GlobalSettingsResponse> UpdateThemeAsync(string theme, CancellationToken cancellationToken);
}
