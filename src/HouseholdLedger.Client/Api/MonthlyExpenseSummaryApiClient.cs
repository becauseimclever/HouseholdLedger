// <copyright file="MonthlyExpenseSummaryApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;

/// <summary>
/// Calls the versioned monthly expense summary endpoint.
/// </summary>
public sealed class MonthlyExpenseSummaryApiClient(HttpClient httpClient) : IMonthlyExpenseSummaryApiClient
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<DailyExpenseSummaryResponse>> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var route = $"api/v1/months/{year}/{month}/expense-summary";
        return await httpClient.GetFromJsonAsync<DailyExpenseSummaryResponse[]>(route, cancellationToken) ?? [];
    }
}
