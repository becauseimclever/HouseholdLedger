// <copyright file="ExpenseTransactionService.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Transactions;

using HouseholdLedger.Domain.Transactions;

/// <summary>
/// Creates and lists date-scoped expense transactions.
/// </summary>
public sealed class ExpenseTransactionService(IExpenseTransactionRepository repository)
{
    /// <summary>Creates one expense for a selected date.</summary>
    /// <param name="ledgerDate">The selected ledger date.</param>
    /// <param name="amount">The positive USD amount.</param>
    /// <param name="classification">The expense classification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created transaction.</returns>
    public async Task<ExpenseTransactionDto> CreateAsync(
        DateOnly ledgerDate,
        decimal amount,
        ExpenseClassification classification,
        CancellationToken cancellationToken = default)
    {
        var transaction = new ExpenseTransaction(Guid.NewGuid(), ledgerDate, amount, classification);
        await repository.AddAsync(transaction, cancellationToken);
        return Map(transaction);
    }

    /// <summary>Lists expenses for one date in backend creation order.</summary>
    /// <param name="ledgerDate">The ledger date to query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transactions recorded on the date.</returns>
    public async Task<IReadOnlyList<ExpenseTransactionDto>> ListAsync(
        DateOnly ledgerDate,
        CancellationToken cancellationToken = default)
    {
        var transactions = await repository.ListByDateAsync(ledgerDate, cancellationToken);
        return transactions.Select(Map).ToArray();
    }

    private static ExpenseTransactionDto Map(ExpenseTransaction transaction) => new(
        transaction.Id,
        transaction.Date,
        transaction.Amount,
        transaction.Classification);
}
