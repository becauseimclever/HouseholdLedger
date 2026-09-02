// <copyright file="HealthEndpointTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

/// <summary>
/// Verifies the health endpoint through the real ASP.NET Core host.
/// </summary>
public sealed class HealthEndpointTests
{
    /// <summary>
    /// Verifies that the API host starts and serves the versioned health route.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetHealthStartsHostAndReturnsSuccess()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.GetAsync(
            "/api/v1/health",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        var properties = document.RootElement.EnumerateObject().ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Single(properties);
        Assert.Equal("status", properties[0].Name);
        Assert.Equal("available", properties[0].Value.GetString());
        Assert.DoesNotContain("environment", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("version", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection", body, StringComparison.OrdinalIgnoreCase);
    }
}
