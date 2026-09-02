// <copyright file="InfrastructureRegistrationTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.IntegrationTests;

using System.Data;

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
        var transactionEntity = Assert.Single(context.Model.GetEntityTypes());

        Assert.Multiple(
            () => Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName),
            () => Assert.Equal(typeof(ExpenseTransaction), transactionEntity.ClrType),
            () => Assert.Equal("numeric(18,2)", transactionEntity.FindProperty(nameof(ExpenseTransaction.Amount))?.GetColumnType()),
            () => Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State));
    }
}
