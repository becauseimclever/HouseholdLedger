// <copyright file="PostgreSqlConnectivityTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.IntegrationTests;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Transactions;
using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

/// <summary>
/// Verifies the persistence boundary against an isolated PostgreSQL database.
/// </summary>
[Collection(PostgreSqlConnectivityTests.PostgreSqlCollectionDefinition.Name)]
public sealed class PostgreSqlConnectivityTests
{
    private const string ConnectionStringEnvironmentVariable =
        "HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING";

    /// <summary>
    /// Verifies that migrations provision PostgreSQL and transaction mutations persist.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task MigrationsProvisionTransactionPersistence()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Skip($"Set {ConnectionStringEnvironmentVariable} to an isolated PostgreSQL database.");
        }

        var services = new ServiceCollection();
        services.AddHouseholdLedgerInfrastructure(connectionString);

        await using var provider = services.BuildServiceProvider(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HouseholdLedgerDbContext>();
        var cancellationToken = TestContext.Current.CancellationToken;

        await context.Database.MigrateAsync(cancellationToken);
        var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var repository = scope.ServiceProvider.GetRequiredService<IExpenseTransactionRepository>();
        var ledgerDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var primaryAccount = new Account(Guid.NewGuid(), $"Checking {Guid.NewGuid():N}");
        var revisedAccount = new Account(Guid.NewGuid(), $"Cash {Guid.NewGuid():N}");
        var firstTransaction = new ExpenseTransaction(
            Guid.NewGuid(),
            primaryAccount.Id,
            ledgerDate,
            12.34m,
            ExpenseClassification.Culture);
        var secondTransaction = new ExpenseTransaction(
            Guid.NewGuid(),
            primaryAccount.Id,
            ledgerDate,
            23.45m,
            ExpenseClassification.Necessities);

        Assert.True(await accountRepository.TryAddAsync(primaryAccount, cancellationToken));
        Assert.True(await accountRepository.TryAddAsync(revisedAccount, cancellationToken));
        await repository.AddAsync(firstTransaction, cancellationToken);
        await repository.AddAsync(secondTransaction, cancellationToken);

        try
        {
            var ownershipException = await Assert.ThrowsAsync<PostgresException>(
                () => context.Accounts
                    .Where(account => account.Id == primaryAccount.Id)
                    .ExecuteDeleteAsync(cancellationToken));
            var persisted = await repository.FindAsync(ledgerDate, firstTransaction.Id, cancellationToken);
            Assert.NotNull(persisted);
            persisted.Revise(revisedAccount.Id, 45.67m, ExpenseClassification.Unexpected);
            await repository.UpdateAsync(persisted, cancellationToken);
            var selectedDay = (await repository.ListByDateAsync(ledgerDate, cancellationToken))
                .Where(item => item.Id == firstTransaction.Id || item.Id == secondTransaction.Id)
                .ToArray();
            var revised = selectedDay[0];
            var dateRange = await repository.ListByDateRangeAsync(
                ledgerDate,
                ledgerDate.AddDays(1),
                cancellationToken);
            await repository.RemoveAsync(persisted, cancellationToken);
            var afterRemoval = await repository.ListByDateAsync(ledgerDate, cancellationToken);

            Assert.Multiple(
                () => Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName),
                () => Assert.Equal(PostgresErrorCodes.RestrictViolation, ownershipException.SqlState),
                () => Assert.Equal(new[] { firstTransaction.Id, secondTransaction.Id }, selectedDay.Select(item => item.Id)),
                () => Assert.Equal(revisedAccount.Id, revised.AccountId),
                () => Assert.Equal(revisedAccount.Name, revised.AccountName),
                () => Assert.Equal(45.67m, revised.Amount),
                () => Assert.Equal(ExpenseClassification.Unexpected, revised.Classification),
                () => Assert.True(firstTransaction.Sequence > 0),
                () => Assert.Contains(dateRange, item => item.Id == firstTransaction.Id),
                () => Assert.DoesNotContain(afterRemoval, item => item.Id == firstTransaction.Id),
                () => Assert.Contains(context.Database.GetAppliedMigrations(), migration => migration.EndsWith("RequireTransactionAccount", StringComparison.Ordinal)));
        }
        finally
        {
            await context.ExpenseTransactions
                .Where(item => item.Id == firstTransaction.Id || item.Id == secondTransaction.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await context.Accounts
                .Where(account => account.Id == primaryAccount.Id || account.Id == revisedAccount.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    /// <summary>Verifies migrations provision deterministic account persistence and normalized uniqueness.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task MigrationsProvisionAccountPersistenceAndUniqueness()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Skip($"Set {ConnectionStringEnvironmentVariable} to an isolated PostgreSQL database.");
        }

        var services = new ServiceCollection();
        services.AddHouseholdLedgerInfrastructure(connectionString);

        await using var provider = services.BuildServiceProvider(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HouseholdLedgerDbContext>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountIdentifiers = new[]
        {
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Guid.Parse("10000000-0000-0000-0000-000000000003"),
        };

        await context.Database.MigrateAsync(cancellationToken);
        await context.Accounts
            .Where(account => accountIdentifiers.Contains(account.Id))
            .ExecuteDeleteAsync(cancellationToken);
        var repository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        try
        {
            Assert.True(await repository.TryAddAsync(
                new Account(accountIdentifiers[0], "Household Checking"),
                cancellationToken));
            Assert.True(await repository.TryAddAsync(
                new Account(accountIdentifiers[1], "Cash Wallet"),
                cancellationToken));
            Assert.True(await repository.TryAddAsync(
                new Account(accountIdentifiers[2], "Rainy Day Savings"),
                cancellationToken));
            var duplicateAdded = await repository.TryAddAsync(
                new Account(Guid.NewGuid(), "cash wallet"),
                cancellationToken);
            var found = await repository.FindAsync(accountIdentifiers[1], cancellationToken);
            var missing = await repository.FindAsync(Guid.NewGuid(), cancellationToken);
            var accounts = await repository.ListAsync(cancellationToken);
            var fixtures = accounts.Where(account => accountIdentifiers.Contains(account.Id)).ToArray();

            Assert.Multiple(
                () => Assert.False(duplicateAdded),
                () => Assert.Equal("Cash Wallet", found?.Name),
                () => Assert.Null(missing),
                () => Assert.Equal(accountIdentifiers[1], fixtures[0].Id),
                () => Assert.Equal(accountIdentifiers[0], fixtures[1].Id),
                () => Assert.Equal(accountIdentifiers[2], fixtures[2].Id),
                () => Assert.Contains(context.Database.GetAppliedMigrations(), migration => migration.EndsWith("AddAccounts", StringComparison.Ordinal)));
        }
        finally
        {
            await context.Accounts
                .Where(account => accountIdentifiers.Contains(account.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Prevents tests using the shared external database from overlapping other collections.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class PostgreSqlCollectionDefinition
    {
        /// <summary>
        /// The xUnit collection name.
        /// </summary>
        public const string Name = "PostgreSQL database";
    }
}
