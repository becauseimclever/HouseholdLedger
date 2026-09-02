// <copyright file="HostedClientRoutingTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

/// <summary>
/// Verifies API-hosted Client routing boundaries through the real host.
/// </summary>
public sealed class HostedClientRoutingTests
{
    /// <summary>
    /// Verifies that the API hosts the referenced Client without masking API or OpenAPI failures.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FrameworkHostedClientServesEntryAssetsAndEligibleDeepLinksWithoutMaskingApiRoutes()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);

        var cancellationToken = TestContext.Current.CancellationToken;
        using var rootResponse = await client.GetAsync("/", cancellationToken);
        using var deepLinkResponse = await client.GetAsync("/calendar/2026-08", cancellationToken);
        using var frameworkAssetResponse = await client.GetAsync(
            "/_framework/blazor.webassembly.js",
            cancellationToken);
        using var unknownApiResponse = await client.GetAsync("/api/v1/missing.json", cancellationToken);
        using var unknownOpenApiResponse = await client.GetAsync("/openapi/missing.json", cancellationToken);

        var rootBody = await rootResponse.Content.ReadAsStringAsync(cancellationToken);
        var deepLinkBody = await deepLinkResponse.Content.ReadAsStringAsync(cancellationToken);
        var frameworkAssetBody = await frameworkAssetResponse.Content.ReadAsStringAsync(cancellationToken);
        var unknownApiBody = await unknownApiResponse.Content.ReadAsStringAsync(cancellationToken);
        var unknownOpenApiBody = await unknownOpenApiResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, rootResponse.StatusCode),
            () => Assert.Equal("text/html", rootResponse.Content.Headers.ContentType?.MediaType),
            () => Assert.Contains("<script src=\"_framework/blazor.webassembly.js\"></script>", rootBody, StringComparison.Ordinal),
            () => Assert.Equal(HttpStatusCode.OK, deepLinkResponse.StatusCode),
            () => Assert.Equal("text/html", deepLinkResponse.Content.Headers.ContentType?.MediaType),
            () => Assert.Equal(rootBody, deepLinkBody),
            () => Assert.Equal(HttpStatusCode.OK, frameworkAssetResponse.StatusCode),
            () => Assert.Equal("text/javascript", frameworkAssetResponse.Content.Headers.ContentType?.MediaType),
            () => Assert.NotEmpty(frameworkAssetBody),
            () => Assert.Equal(HttpStatusCode.NotFound, unknownApiResponse.StatusCode),
            () => Assert.NotEqual("text/html", unknownApiResponse.Content.Headers.ContentType?.MediaType),
            () => Assert.NotEqual(rootBody, unknownApiBody),
            () => Assert.Equal(HttpStatusCode.NotFound, unknownOpenApiResponse.StatusCode),
            () => Assert.NotEqual("text/html", unknownOpenApiResponse.Content.Headers.ContentType?.MediaType),
            () => Assert.NotEqual(rootBody, unknownOpenApiBody));
    }
}
