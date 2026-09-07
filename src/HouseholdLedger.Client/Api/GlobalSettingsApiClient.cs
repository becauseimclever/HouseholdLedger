// <copyright file="GlobalSettingsApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Net.Http.Json;
using System.Text.Json;
using HouseholdLedger.Api.Contracts;

/// <summary>Uses HTTP to read and update global application settings.</summary>
public sealed class GlobalSettingsApiClient(HttpClient httpClient) : IGlobalSettingsApiClient
{
    /// <inheritdoc/>
    public async Task<GlobalSettingsResponse> GetAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<GlobalSettingsResponse>(
            "api/v1/settings",
            cancellationToken)
            ?? throw new JsonException("The global settings response was empty.");
    }

    /// <inheritdoc/>
    public async Task<GlobalSettingsResponse> UpdateCurrencyAsync(
        string displayCurrency,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PutAsJsonAsync(
            "api/v1/settings",
            new UpdateGlobalSettingsRequest(displayCurrency),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GlobalSettingsResponse>(cancellationToken)
            ?? throw new JsonException("The saved global settings response was empty.");
    }

    /// <inheritdoc/>
    public async Task<GlobalSettingsResponse> UpdateThemeAsync(
        string theme,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PutAsJsonAsync(
            "api/v1/settings/theme",
            new UpdateThemeRequest(theme),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GlobalSettingsResponse>(cancellationToken)
            ?? throw new JsonException("The saved global settings response was empty.");
    }
}
