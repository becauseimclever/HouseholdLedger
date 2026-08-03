// <copyright file="PostgreSqlConnectivityTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.IntegrationTests;

using System.Data;
using System.Globalization;

using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

/// <summary>
/// Verifies the persistence boundary against an isolated PostgreSQL database.
/// </summary>
public sealed class PostgreSqlConnectivityTests
{
    private const string ConnectionStringEnvironmentVariable =
        "HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING";

    /// <summary>
    /// Verifies that the empty context connects without creating a schema.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyContextConnectsWithoutCreatingTables()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Ignore($"Set {ConnectionStringEnvironmentVariable} to an isolated PostgreSQL database.");
        }

        var services = new ServiceCollection();
        services.AddHouseholdLedgerInfrastructure(connectionString);

        await using var provider = services.BuildServiceProvider(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HouseholdLedgerDbContext>();
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var tableCount = await CountPublicTablesAsync(context, cancellationToken);

            Assert.Multiple(() =>
            {
                Assert.That(context.Database.ProviderName, Is.EqualTo("Npgsql.EntityFrameworkCore.PostgreSQL"));
                Assert.That(context.Model.GetEntityTypes(), Is.Empty);
                Assert.That(context.Database.GetMigrations(), Is.Empty);
                Assert.That(tableCount, Is.Zero);
            });
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        Assert.That(context.Database.GetDbConnection().State, Is.EqualTo(ConnectionState.Closed));
    }

    private static async Task<int> CountPublicTablesAsync(
        HouseholdLedgerDbContext context,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pg_catalog.pg_tables WHERE schemaname = 'public'";
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }
}
