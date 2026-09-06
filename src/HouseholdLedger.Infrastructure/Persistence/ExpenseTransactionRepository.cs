// <copyright file="ExpenseTransactionRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Persists expense transactions through Entity Framework Core.
/// </summary>
public sealed class ExpenseTransactionRepository(HouseholdLedgerDbContext dbContext)
    : IExpenseTransactionRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(ExpenseTransaction transaction, CancellationToken cancellationToken)
    {
        dbContext.ExpenseTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ExpenseTransaction?> FindAsync(
        DateOnly ledgerDate,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ExpenseTransactions.SingleOrDefaultAsync(
            transaction => transaction.Date == ledgerDate && transaction.Id == transactionId,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExpenseTransactionDto>> ListByDateAsync(
        DateOnly ledgerDate,
        CancellationToken cancellationToken)
    {
        return await dbContext.ExpenseTransactions
            .AsNoTracking()
            .Where(transaction => transaction.Date == ledgerDate)
            .OrderBy(transaction => transaction.Sequence)
            .Join(
                dbContext.Accounts,
                transaction => transaction.AccountId,
                account => account.Id,
                (transaction, account) => new ExpenseTransactionDto(
                    transaction.Id,
                    transaction.AccountId,
                    account.Name,
                    transaction.Date,
                    transaction.Amount,
                    transaction.Classification))
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExpenseTransaction>> ListByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        return await dbContext.ExpenseTransactions
            .AsNoTracking()
            .Where(transaction => transaction.Date >= startDate && transaction.Date < endDate)
            .OrderBy(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Sequence)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(ExpenseTransaction transaction, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(ExpenseTransaction transaction, CancellationToken cancellationToken)
    {
        dbContext.ExpenseTransactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
