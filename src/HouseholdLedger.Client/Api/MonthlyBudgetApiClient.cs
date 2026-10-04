// <copyright file="MonthlyBudgetApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;

/// <summary>Calls the versioned monthly intention endpoints.</summary>
public sealed class MonthlyBudgetApiClient(HttpClient httpClient) : IMonthlyBudgetApiClient
{
    /// <inheritdoc/>
    public async Task<MonthlyBudgetPlanResponse?> GetPlanAsync(int year, int month, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"{GetRoute(year, month)}/budget-plan", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MonthlyBudgetPlanResponse>(cancellationToken)
            ?? throw new HttpRequestException("The monthly plan response was empty.");
    }

    /// <inheritdoc/>
    public async Task<MonthlyBudgetPlanResponse> SavePlanAsync(int year, int month, MonthlyBudgetPlanRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PutAsJsonAsync($"{GetRoute(year, month)}/budget-plan", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MonthlyBudgetPlanResponse>(cancellationToken)
            ?? throw new HttpRequestException("The saved monthly plan response was empty.");
    }

    /// <inheritdoc/>
    public async Task<MonthlyBudgetReviewResponse> GetReviewAsync(int year, int month, CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<MonthlyBudgetReviewResponse>($"{GetRoute(year, month)}/budget-review", cancellationToken)
        ?? throw new HttpRequestException("The monthly review response was empty.");

    private static string GetRoute(int year, int month) => $"api/v1/months/{year}/{month}";
}
