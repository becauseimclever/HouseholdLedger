// <copyright file="IAccountsApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using HouseholdLedger.Api.Contracts;

/// <summary>Calls the versioned account catalog endpoints.</summary>
public interface IAccountsApiClient
{
    /// <summary>Lists persisted accounts.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative account catalog.</returns>
    Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Attempts to create one account.</summary>
    /// <param name="request">The account values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The mutation outcome.</returns>
    Task<AccountCreationResult> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken);
}
