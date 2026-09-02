// <copyright file="PostgreSqlConnectivityTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.IntegrationTests;

using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Transactions;
using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    /// Verifies that migrations provision PostgreSQL and a transaction persists and rereads.
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
        var repository = scope.ServiceProvider.GetRequiredService<IExpenseTransactionRepository>();
        var ledgerDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var transaction = new ExpenseTransaction(
            Guid.NewGuid(),
            ledgerDate,
            12.34m,
            ExpenseClassification.Culture);

        await repository.AddAsync(transaction, cancellationToken);
        var returned = await repository.ListByDateAsync(ledgerDate, cancellationToken);

        try
        {
            var persisted = Assert.Single(returned, item => item.Id == transaction.Id);

            Assert.Multiple(
                () => Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName),
                () => Assert.Equal(12.34m, persisted.Amount),
                () => Assert.Equal(ExpenseClassification.Culture, persisted.Classification),
                () => Assert.True(persisted.Sequence > 0),
                () => Assert.Contains(context.Database.GetAppliedMigrations(), migration => migration.EndsWith("AddExpenseTransactions", StringComparison.Ordinal)));
        }
        finally
        {
            context.ExpenseTransactions.Remove(transaction);
            await context.SaveChangesAsync(cancellationToken);
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
