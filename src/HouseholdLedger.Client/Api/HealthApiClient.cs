// <copyright file="HealthApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Net.Http.Json;

using HouseholdLedger.Api.Contracts;

/// <summary>
/// Calls the versioned health endpoint through HTTP and its transport contract.
/// </summary>
public sealed class HealthApiClient(HttpClient httpClient) : IHealthApiClient
{
    private const string HealthRoute = "api/v1/health";

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(HealthRoute, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);
        return string.Equals(health?.Status, "available", StringComparison.Ordinal);
    }
}
