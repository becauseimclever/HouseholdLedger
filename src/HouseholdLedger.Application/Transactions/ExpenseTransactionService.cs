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

    /// <summary>Revises the correctable details of one date-scoped expense.</summary>
    /// <param name="ledgerDate">The transaction's ledger date.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="amount">The replacement positive USD amount.</param>
    /// <param name="classification">The replacement expense classification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The revised transaction, or <see langword="null"/> when it does not exist under the date.</returns>
    public async Task<ExpenseTransactionDto?> ReviseAsync(
        DateOnly ledgerDate,
        Guid transactionId,
        decimal amount,
        ExpenseClassification classification,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FindAsync(ledgerDate, transactionId, cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        transaction.Revise(amount, classification);
        await repository.UpdateAsync(transaction, cancellationToken);
        return Map(transaction);
    }

    /// <summary>Removes one date-scoped expense.</summary>
    /// <param name="ledgerDate">The transaction's ledger date.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when removed; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RemoveAsync(
        DateOnly ledgerDate,
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FindAsync(ledgerDate, transactionId, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        await repository.RemoveAsync(transaction, cancellationToken);
        return true;
    }

    private static ExpenseTransactionDto Map(ExpenseTransaction transaction) => new(
        transaction.Id,
        transaction.Date,
        transaction.Amount,
        transaction.Classification);
}
