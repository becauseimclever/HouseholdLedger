// <copyright file="ITransactionsApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using HouseholdLedger.Api.Contracts;

/// <summary>
/// Accesses selected-day transaction operations.
/// </summary>
public interface ITransactionsApiClient
{
    /// <summary>Lists transactions for one date.</summary>
    /// <param name="ledgerDate">The ledger date to query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transactions recorded on the date.</returns>
    Task<IReadOnlyList<ExpenseTransactionResponse>> ListAsync(DateOnly ledgerDate, CancellationToken cancellationToken);

    /// <summary>Creates one transaction for a date.</summary>
    /// <param name="ledgerDate">The selected ledger date.</param>
    /// <param name="request">The transaction values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the transaction was accepted; otherwise, <see langword="false"/>.</returns>
    Task<bool> CreateAsync(DateOnly ledgerDate, CreateExpenseTransactionRequest request, CancellationToken cancellationToken);
}
