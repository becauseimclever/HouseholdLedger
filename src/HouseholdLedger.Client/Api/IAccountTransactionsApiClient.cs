// <copyright file="IAccountTransactionsApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using HouseholdLedger.Api.Contracts;

/// <summary>Loads complete transaction history for a selected account.</summary>
public interface IAccountTransactionsApiClient
{
    /// <summary>Gets an account's transaction history.</summary>
    /// <param name="accountId">The selected account identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account history, or <see langword="null"/> when the account does not exist.</returns>
    Task<AccountTransactionHistoryResponse?> GetAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>Gets an account's matching transaction history.</summary>
    /// <param name="accountId">The selected account identifier.</param>
    /// <param name="filters">The normalized account-history filters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account history, or <see langword="null"/> when the account does not exist.</returns>
    Task<AccountTransactionHistoryResponse?> GetAsync(
        Guid accountId,
        AccountTransactionFilterRequest filters,
        CancellationToken cancellationToken) => this.GetAsync(accountId, cancellationToken);
}
