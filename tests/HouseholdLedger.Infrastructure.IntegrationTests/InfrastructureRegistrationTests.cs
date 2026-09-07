// <copyright file="InfrastructureRegistrationTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.IntegrationTests;

using System.Data;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Transactions;
using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Verifies the PostgreSQL persistence registration boundary.
/// </summary>
public sealed class InfrastructureRegistrationTests
{
    private const string TestConnectionString =
        "Host=127.0.0.1;Port=5432;Database=household_ledger_registration_test;Username=test";

    /// <summary>
    /// Verifies that registration selects Npgsql and exposes the transaction model without opening a connection.
    /// </summary>
    [Fact]
    public void RegistrationConfiguresTransactionNpgsqlContextWithoutConnecting()
    {
        var services = new ServiceCollection();

        services.AddHouseholdLedgerInfrastructure(TestConnectionString);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HouseholdLedgerDbContext>();
        var accountEntity = context.Model.FindEntityType(typeof(Account));
        var transactionEntity = context.Model.FindEntityType(typeof(ExpenseTransaction));
        var accountForeignKey = transactionEntity?.GetForeignKeys().Single();

        Assert.Multiple(
            () => Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName),
            () => Assert.NotNull(scope.ServiceProvider.GetService<IAccountRepository>()),
            () => Assert.NotNull(accountEntity),
            () => Assert.Equal("character varying(100)", accountEntity!.FindProperty(nameof(Account.Name))?.GetColumnType()),
            () => Assert.True(accountEntity!.GetIndexes().Single().IsUnique),
            () => Assert.NotNull(transactionEntity),
            () => Assert.False(accountForeignKey!.IsRequiredDependent),
            () => Assert.True(accountForeignKey!.IsRequired),
            () => Assert.Equal(DeleteBehavior.Restrict, accountForeignKey!.DeleteBehavior),
            () => Assert.Equal("account_id", transactionEntity!.FindProperty(nameof(ExpenseTransaction.AccountId))?.GetColumnName()),
            () => Assert.Equal("numeric", transactionEntity!.FindProperty(nameof(ExpenseTransaction.Amount))?.GetColumnType()),
            () => Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State));
    }
}
