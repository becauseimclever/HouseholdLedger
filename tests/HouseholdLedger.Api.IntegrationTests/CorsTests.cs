// <copyright file="CorsTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

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
    [Fact]
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

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var allowedOrigins = response.Headers.GetValues("Access-Control-Allow-Origin");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal([AllowedOrigin], allowedOrigins);
    }

    /// <summary>
    /// Verifies that an origin absent from configuration is denied.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PreflightFromUnconfiguredOriginIsDenied()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);
        using var request = CreatePreflightRequest(UnconfiguredOrigin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return request;
    }
}
