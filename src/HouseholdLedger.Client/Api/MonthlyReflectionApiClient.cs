// <copyright file="MonthlyReflectionApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;

/// <summary>Calls the versioned monthly reflection endpoint.</summary>
public sealed class MonthlyReflectionApiClient(HttpClient httpClient) : IMonthlyReflectionApiClient
{
    /// <inheritdoc/>
    public async Task<MonthlyReflectionResponse?> GetAsync(int year, int month, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(GetRoute(year, month), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MonthlyReflectionResponse>(cancellationToken)
            ?? throw new HttpRequestException("The monthly reflection response was empty.");
    }

    /// <inheritdoc/>
    public async Task<MonthlyReflectionResponse> SaveAsync(int year, int month, MonthlyReflectionRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PutAsJsonAsync(GetRoute(year, month), request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MonthlyReflectionResponse>(cancellationToken)
            ?? throw new HttpRequestException("The saved monthly reflection response was empty.");
    }

    private static string GetRoute(int year, int month) => $"api/v1/months/{year}/{month}/reflection";
}
