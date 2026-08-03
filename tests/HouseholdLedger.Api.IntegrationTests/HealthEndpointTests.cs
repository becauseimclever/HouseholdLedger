// <copyright file="HealthEndpointTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

/// <summary>
/// Verifies the health endpoint through the real ASP.NET Core host.
/// </summary>
public sealed class HealthEndpointTests
{
    /// <summary>
    /// Verifies that the API host starts and serves the versioned health route.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetHealthStartsHostAndReturnsSuccess()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.GetAsync("/api/v1/health");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var properties = document.RootElement.EnumerateObject().ToArray();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        Assert.That(properties, Has.Length.EqualTo(1));
        Assert.That(properties[0].Name, Is.EqualTo("status"));
        Assert.That(properties[0].Value.GetString(), Is.EqualTo("available"));
        Assert.That(body, Does.Not.Contain("environment").IgnoreCase);
        Assert.That(body, Does.Not.Contain("version").IgnoreCase);
        Assert.That(body, Does.Not.Contain("database").IgnoreCase);
        Assert.That(body, Does.Not.Contain("connection").IgnoreCase);
    }
}
