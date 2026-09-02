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
    public async Task<IReadOnlyList<ExpenseTransaction>> ListByDateAsync(
        DateOnly ledgerDate,
        CancellationToken cancellationToken)
    {
        return await dbContext.ExpenseTransactions
            .AsNoTracking()
            .Where(transaction => transaction.Date == ledgerDate)
            .OrderBy(transaction => transaction.Sequence)
            .ToArrayAsync(cancellationToken);
    }
}
