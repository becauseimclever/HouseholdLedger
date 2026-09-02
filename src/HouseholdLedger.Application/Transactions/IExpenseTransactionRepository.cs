// <copyright file="IExpenseTransactionRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Transactions;

using HouseholdLedger.Domain.Transactions;

/// <summary>
/// Persists and queries expense transactions.
/// </summary>
public interface IExpenseTransactionRepository
{
    /// <summary>Adds one transaction.</summary>
    /// <param name="transaction">The transaction to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task AddAsync(ExpenseTransaction transaction, CancellationToken cancellationToken);

    /// <summary>Returns transactions recorded on a date in creation order.</summary>
    /// <param name="ledgerDate">The ledger date to query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transactions recorded on the date.</returns>
    Task<IReadOnlyList<ExpenseTransaction>> ListByDateAsync(DateOnly ledgerDate, CancellationToken cancellationToken);
}
