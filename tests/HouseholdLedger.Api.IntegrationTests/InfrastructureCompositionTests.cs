// <copyright file="InfrastructureCompositionTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;

using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Verifies persistence composition at the API host boundary.
/// </summary>
[Collection(InfrastructureCompositionTests.EnvironmentVariableCollectionDefinition.Name)]
public sealed class InfrastructureCompositionTests
{
    private const string ConfigurationKey = "ConnectionStrings__HouseholdLedger";
    private const string DatabasePassword = "test-only-secret";
    private const string TestConnectionString =
        $"Host=database.invalid;Database=household_ledger_test;Username=test_user;Password={DatabasePassword}";

    /// <summary>
    /// Verifies that configured persistence is registered without requiring a live database at startup.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConfiguredConnectionRegistersProviderWithoutConnectingAtStartup()
    {
        var previousConnectionString = Environment.GetEnvironmentVariable(ConfigurationKey);
        Environment.SetEnvironmentVariable(ConfigurationKey, TestConnectionString);

        try
        {
            await using var factory = new WebApplicationFactory<Program>();
            using var client = ApiTestClient.Create(factory);

            using var response = await client.GetAsync(
                "/api/v1/health",
                TestContext.Current.CancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var openApiBody = await client.GetStringAsync(
                "/openapi/v1.json",
                TestContext.Current.CancellationToken);
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HouseholdLedgerDbContext>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
            Assert.DoesNotContain(DatabasePassword, responseBody, StringComparison.Ordinal);
            Assert.DoesNotContain(DatabasePassword, openApiBody, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConfigurationKey, previousConnectionString);
        }
    }

    /// <summary>
    /// Verifies that unusable connection-string values are treated as unconfigured.
    /// </summary>
    /// <param name="connectionString">The absent, blank, empty, or malformed configured value.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(";")]
    [InlineData("not-a-connection-string")]
    public async Task UnusableConnectionStringDoesNotRegisterProvider(string? connectionString)
    {
        var previousConnectionString = Environment.GetEnvironmentVariable(ConfigurationKey);
        Environment.SetEnvironmentVariable(ConfigurationKey, connectionString);

        try
        {
            await using var factory = new WebApplicationFactory<Program>();
            using var client = ApiTestClient.Create(factory);

            using var response = await client.GetAsync(
                "/api/v1/health",
                TestContext.Current.CancellationToken);
            var providerSupportsServiceQueries =
                factory.Services.GetRequiredService<IServiceProviderIsService>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(providerSupportsServiceQueries.IsService(typeof(HouseholdLedgerDbContext)));
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConfigurationKey, previousConnectionString);
        }
    }

    /// <summary>
    /// Prevents tests that mutate process environment variables from overlapping other collections.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class EnvironmentVariableCollectionDefinition
    {
        /// <summary>
        /// The xUnit collection name.
        /// </summary>
        public const string Name = "Environment variables";
    }
}
