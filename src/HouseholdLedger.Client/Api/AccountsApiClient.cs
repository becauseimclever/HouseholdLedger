// <copyright file="AccountsApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;

/// <summary>Calls the versioned account catalog endpoints.</summary>
public sealed class AccountsApiClient(HttpClient httpClient) : IAccountsApiClient
{
    private const string Route = "api/v1/accounts";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<AccountResponse[]>(Route, cancellationToken) ?? [];
    }

    /// <inheritdoc/>
    public async Task<AccountCreationResult> CreateAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(Route, request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return AccountCreationResult.Invalid;
        }

        response.EnsureSuccessStatusCode();
        return AccountCreationResult.Success;
    }
}
