// <copyright file="InfrastructureRegistrationTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.IntegrationTests;

using System.Data;

using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

/// <summary>
/// Verifies the PostgreSQL persistence registration boundary.
/// </summary>
public sealed class InfrastructureRegistrationTests
{
    private const string TestConnectionString =
        "Host=127.0.0.1;Port=5432;Database=household_ledger_registration_test;Username=test";

    /// <summary>
    /// Verifies that registration selects Npgsql and exposes an empty scoped context without opening a connection.
    /// </summary>
    [Test]
    public void RegistrationConfiguresEmptyNpgsqlContextWithoutConnecting()
    {
        var services = new ServiceCollection();

        services.AddHouseholdLedgerInfrastructure(TestConnectionString);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HouseholdLedgerDbContext>();

        Assert.Multiple(() =>
        {
            Assert.That(context.Database.ProviderName, Is.EqualTo("Npgsql.EntityFrameworkCore.PostgreSQL"));
            Assert.That(context.Model.GetEntityTypes(), Is.Empty);
            Assert.That(context.Database.GetDbConnection().State, Is.EqualTo(ConnectionState.Closed));
        });
    }
}
