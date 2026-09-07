// <copyright file="DevelopmentDatabaseInitializer.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

/// <summary>Initializes an empty development database with the canonical fixture.</summary>
public sealed class DevelopmentDatabaseInitializer(HouseholdLedgerDbContext dbContext)
{
    /// <summary>Applies migrations and seeds the canonical fixture when the database is empty.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await dbContext.Accounts.AnyAsync(cancellationToken)
            || await dbContext.ExpenseTransactions.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "Development data was not added because the database is not empty.");
        }

        var checkingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var cashId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        dbContext.Accounts.AddRange(
            new Account(checkingId, "Household Checking"),
            new Account(cashId, "Cash Wallet"),
            new Account(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Rainy Day Savings"));
        await dbContext.SaveChangesAsync(cancellationToken);

        var transactions = new ExpenseTransaction[]
        {
            new(Guid.Parse("20000000-0000-0000-0000-000000000001"), checkingId, new DateOnly(2026, 9, 1), 82.45m, ExpenseClassification.Necessities),
            new(Guid.Parse("20000000-0000-0000-0000-000000000002"), cashId, new DateOnly(2026, 9, 1), 12m, ExpenseClassification.Optional),
            new(Guid.Parse("20000000-0000-0000-0000-000000000003"), checkingId, new DateOnly(2026, 9, 3), 14m, ExpenseClassification.Culture),
            new(Guid.Parse("20000000-0000-0000-0000-000000000004"), checkingId, new DateOnly(2026, 9, 3), 6.50m, ExpenseClassification.Optional),
            new(Guid.Parse("20000000-0000-0000-0000-000000000005"), cashId, new DateOnly(2026, 9, 15), 25.25m, ExpenseClassification.Necessities),
            new(Guid.Parse("20000000-0000-0000-0000-000000000006"), checkingId, new DateOnly(2026, 9, 30), 40m, ExpenseClassification.Unexpected),
            new(Guid.Parse("20000000-0000-0000-0000-000000000007"), cashId, new DateOnly(2026, 10, 1), 9.75m, ExpenseClassification.Culture),
        };

        foreach (var expenseTransaction in transactions)
        {
            dbContext.ExpenseTransactions.Add(expenseTransaction);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
