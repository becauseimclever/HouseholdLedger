// <copyright file="AccountTransactionsApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HouseholdLedger.Api.Contracts;

/// <summary>Loads complete transaction history for a selected account.</summary>
public sealed class AccountTransactionsApiClient(HttpClient httpClient) : IAccountTransactionsApiClient
{
    /// <inheritdoc/>
    public async Task<AccountTransactionHistoryResponse?> GetAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await this.GetAsync(accountId, new AccountTransactionFilterRequest(), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AccountTransactionHistoryResponse?> GetAsync(
        Guid accountId,
        AccountTransactionFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var query = CreateQuery(filters);
        using var response = await httpClient.GetAsync(
            $"api/v1/accounts/{accountId}/transactions{query}",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountTransactionHistoryResponse>(cancellationToken)
            ?? throw new JsonException("The account history response was empty.");
    }

    private static string CreateQuery(AccountTransactionFilterRequest filters)
    {
        var values = new List<string>();
        Add(values, "from", filters.From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add(values, "to", filters.To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add(values, "classification", filters.Classification);
        Add(values, "minimumAmount", filters.MinimumAmount?.ToString(CultureInfo.InvariantCulture));
        Add(values, "maximumAmount", filters.MaximumAmount?.ToString(CultureInfo.InvariantCulture));
        Add(values, "search", filters.Search);
        return values.Count == 0 ? string.Empty : $"?{string.Join('&', values)}";
    }

    private static void Add(List<string> values, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            values.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }
}
