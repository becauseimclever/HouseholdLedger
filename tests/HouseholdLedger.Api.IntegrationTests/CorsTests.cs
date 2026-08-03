// <copyright file="CorsTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

/// <summary>
/// Verifies cross-origin access through the real ASP.NET Core host.
/// </summary>
public sealed class CorsTests
{
    private const string AllowedOrigin = "https://frontend.example";
    private const string UnconfiguredOrigin = "https://unconfigured.example";

    /// <summary>
    /// Verifies that a configured standalone frontend origin is allowed.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PreflightFromConfiguredOriginIsAllowed()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = AllowedOrigin,
        };

        await using var baseFactory = new WebApplicationFactory<Program>();
        await using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(settings)));
        using var client = ApiTestClient.Create(factory);
        using var request = CreatePreflightRequest(AllowedOrigin);

        using var response = await client.SendAsync(request);
        var allowedOrigins = response.Headers.GetValues("Access-Control-Allow-Origin");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(allowedOrigins, Is.EqualTo(new[] { AllowedOrigin }));
    }

    /// <summary>
    /// Verifies that an origin absent from configuration is denied.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PreflightFromUnconfiguredOriginIsDenied()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);
        using var request = CreatePreflightRequest(UnconfiguredOrigin);

        using var response = await client.SendAsync(request);

        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return request;
    }
}
