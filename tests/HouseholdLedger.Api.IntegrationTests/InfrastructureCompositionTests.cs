// <copyright file="InfrastructureCompositionTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;

using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

/// <summary>
/// Verifies persistence composition at the API host boundary.
/// </summary>
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
    [Test]
    [NonParallelizable]
    public async Task ConfiguredConnectionRegistersProviderWithoutConnectingAtStartup()
    {
        var previousConnectionString = Environment.GetEnvironmentVariable(ConfigurationKey);
        Environment.SetEnvironmentVariable(ConfigurationKey, TestConnectionString);

        try
        {
            await using var factory = new WebApplicationFactory<Program>();
            using var client = ApiTestClient.Create(factory);

            using var response = await client.GetAsync("/api/v1/health");
            var responseBody = await response.Content.ReadAsStringAsync();
            var openApiBody = await client.GetStringAsync("/openapi/v1.json");
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HouseholdLedgerDbContext>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(context.Database.ProviderName, Is.EqualTo("Npgsql.EntityFrameworkCore.PostgreSQL"));
            Assert.That(responseBody, Does.Not.Contain(DatabasePassword));
            Assert.That(openApiBody, Does.Not.Contain(DatabasePassword));
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
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(";")]
    [TestCase("not-a-connection-string")]
    [NonParallelizable]
    public async Task UnusableConnectionStringDoesNotRegisterProvider(string? connectionString)
    {
        var previousConnectionString = Environment.GetEnvironmentVariable(ConfigurationKey);
        Environment.SetEnvironmentVariable(ConfigurationKey, connectionString);

        try
        {
            await using var factory = new WebApplicationFactory<Program>();
            using var client = ApiTestClient.Create(factory);

            using var response = await client.GetAsync("/api/v1/health");
            var providerSupportsServiceQueries =
                factory.Services.GetRequiredService<IServiceProviderIsService>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(
                providerSupportsServiceQueries.IsService(typeof(HouseholdLedgerDbContext)),
                Is.False);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConfigurationKey, previousConnectionString);
        }
    }
}
