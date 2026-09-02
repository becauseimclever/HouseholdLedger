// <copyright file="HostedClientRoutingTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

/// <summary>
/// Verifies API-hosted Client routing boundaries through the real host.
/// </summary>
public sealed class HostedClientRoutingTests
{
    /// <summary>
    /// Verifies that the API hosts the referenced Client without masking API or OpenAPI failures.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FrameworkHostedClientServesEntryAssetsAndEligibleDeepLinksWithoutMaskingApiRoutes()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);

        using var rootResponse = await client.GetAsync("/");
        using var deepLinkResponse = await client.GetAsync("/calendar/2026-08");
        using var frameworkAssetResponse = await client.GetAsync("/_framework/blazor.webassembly.js");
        using var unknownApiResponse = await client.GetAsync("/api/v1/missing.json");
        using var unknownOpenApiResponse = await client.GetAsync("/openapi/missing.json");

        var rootBody = await rootResponse.Content.ReadAsStringAsync();
        var deepLinkBody = await deepLinkResponse.Content.ReadAsStringAsync();
        var frameworkAssetBody = await frameworkAssetResponse.Content.ReadAsStringAsync();
        var unknownApiBody = await unknownApiResponse.Content.ReadAsStringAsync();
        var unknownOpenApiBody = await unknownOpenApiResponse.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(rootResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(rootResponse.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(rootBody, Does.Contain("<script src=\"_framework/blazor.webassembly.js\"></script>"));
            Assert.That(deepLinkResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(deepLinkResponse.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(deepLinkBody, Is.EqualTo(rootBody));
            Assert.That(frameworkAssetResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(frameworkAssetResponse.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/javascript"));
            Assert.That(frameworkAssetBody, Is.Not.Empty);
            Assert.That(unknownApiResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(unknownApiResponse.Content.Headers.ContentType?.MediaType, Is.Not.EqualTo("text/html"));
            Assert.That(unknownApiBody, Is.Not.EqualTo(rootBody));
            Assert.That(unknownOpenApiResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(unknownOpenApiResponse.Content.Headers.ContentType?.MediaType, Is.Not.EqualTo("text/html"));
            Assert.That(unknownOpenApiBody, Is.Not.EqualTo(rootBody));
        });
    }
}
