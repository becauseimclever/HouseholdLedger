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

    /// <summary>Finds one transaction under its ledger date.</summary>
    /// <param name="ledgerDate">The transaction's ledger date.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching transaction, or <see langword="null"/>.</returns>
    Task<ExpenseTransaction?> FindAsync(
        DateOnly ledgerDate,
        Guid transactionId,
        CancellationToken cancellationToken);

    /// <summary>Returns transactions recorded on a date in creation order.</summary>
    /// <param name="ledgerDate">The ledger date to query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transactions recorded on the date.</returns>
    Task<IReadOnlyList<ExpenseTransaction>> ListByDateAsync(DateOnly ledgerDate, CancellationToken cancellationToken);

    /// <summary>Returns transactions in a half-open ledger-date range.</summary>
    /// <param name="startDate">The inclusive first ledger date.</param>
    /// <param name="endDate">The exclusive final ledger date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transactions in date and creation order.</returns>
    Task<IReadOnlyList<ExpenseTransaction>> ListByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);

    /// <summary>Persists changes to one transaction.</summary>
    /// <param name="transaction">The revised transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task UpdateAsync(ExpenseTransaction transaction, CancellationToken cancellationToken);

    /// <summary>Removes one transaction.</summary>
    /// <param name="transaction">The transaction to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task RemoveAsync(ExpenseTransaction transaction, CancellationToken cancellationToken);
}
