// <copyright file="HostedClientRoutingTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

/// <summary>
/// Verifies API-hosted Client routing boundaries through the real host.
/// </summary>
public sealed class HostedClientRoutingTests
{
    /// <summary>
    /// Verifies that an API without a composed Client remains available and does not fabricate a fallback.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ArtifactAbsentHostServesApiAndOpenApiWithoutClientFallback()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);

        using var healthResponse = await client.GetAsync("/api/v1/health");
        using var openApiResponse = await client.GetAsync("/openapi/v1.json");
        using var rootResponse = await client.GetAsync("/");
        using var deepLinkResponse = await client.GetAsync("/calendar/2026-08");

        Assert.Multiple(() =>
        {
            Assert.That(healthResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(openApiResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(rootResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(deepLinkResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    /// <summary>
    /// Verifies that composed Client static files and browser fallback do not mask API or OpenAPI routes.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ComposedClientServesRootAssetsAndEligibleDeepLinksWithoutMaskingApiRoutes()
    {
        var webRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(webRoot, "css"));
        var assets = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["index.html"] = "<html><body>Hosted client entry</body></html>",
            ["css/app.css"] = "body { color: black; }",
        };

        foreach (var (relativePath, content) in assets)
        {
            await File.WriteAllTextAsync(Path.Combine(webRoot, relativePath), content, Encoding.UTF8);
        }

        await File.WriteAllTextAsync(Path.Combine(webRoot, "undeclared.txt"), "Not an asset", Encoding.UTF8);
        await WriteComposedClientManifestsAsync(webRoot, assets.Keys);

        try
        {
            await using var baseFactory = new WebApplicationFactory<Program>();
            await using var factory = baseFactory.WithWebHostBuilder(builder => builder.UseWebRoot(webRoot));
            using var client = ApiTestClient.Create(factory);

            using var rootResponse = await client.GetAsync("/");
            using var assetResponse = await client.GetAsync("/css/app.css");
            using var deepLinkResponse = await client.GetAsync("/calendar/2026-08");
            using var missingFileResponse = await client.GetAsync("/missing.css");
            using var undeclaredFileResponse = await client.GetAsync("/undeclared.txt");
            using var unknownApiResponse = await client.GetAsync("/api/v1/missing");
            using var unknownOpenApiResponse = await client.GetAsync("/openapi/missing.json");

            var rootBody = await rootResponse.Content.ReadAsStringAsync();
            var assetBody = await assetResponse.Content.ReadAsStringAsync();
            var deepLinkBody = await deepLinkResponse.Content.ReadAsStringAsync();
            var missingFileBody = await missingFileResponse.Content.ReadAsStringAsync();
            var undeclaredFileBody = await undeclaredFileResponse.Content.ReadAsStringAsync();
            var unknownApiBody = await unknownApiResponse.Content.ReadAsStringAsync();
            var unknownOpenApiBody = await unknownOpenApiResponse.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(rootResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(rootResponse.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
                Assert.That(rootBody, Does.Contain("Hosted client entry"));
                Assert.That(assetResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(assetResponse.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/css"));
                Assert.That(assetBody, Does.Contain("color: black"));
                Assert.That(deepLinkResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(deepLinkResponse.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
                Assert.That(deepLinkBody, Does.Contain("Hosted client entry"));
                Assert.That(missingFileResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(missingFileBody, Does.Not.Contain("Hosted client entry"));
                Assert.That(undeclaredFileResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(undeclaredFileBody, Does.Not.Contain("Not an asset"));
                Assert.That(unknownApiResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(unknownApiResponse.Content.Headers.ContentType?.MediaType, Is.Not.EqualTo("text/html"));
                Assert.That(unknownApiBody, Does.Not.Contain("Hosted client entry"));
                Assert.That(unknownOpenApiResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(unknownOpenApiResponse.Content.Headers.ContentType?.MediaType, Is.Not.EqualTo("text/html"));
                Assert.That(unknownOpenApiBody, Does.Not.Contain("Hosted client entry"));
            });
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    private static async Task WriteComposedClientManifestsAsync(
        string packageRoot,
        IEnumerable<string> assetPaths)
    {
        var inventory = assetPaths
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => $"{path}|{GetSha256(Path.Combine(packageRoot, path))}")
            .ToArray();
        var clientManifestPath = Path.Combine(packageRoot, "household-ledger-client.manifest");
        var clientManifest = new List<string>
        {
            "contract-version=1",
            "artifact-identity=household-ledger-client@1.0.0",
            "content-root=wwwroot",
            "entry-point=index.html",
            "file-sha256",
        };
        clientManifest.AddRange(inventory);

        await File.WriteAllLinesAsync(clientManifestPath, clientManifest, new UTF8Encoding(false));

        var compositionManifest = new List<string>
        {
            "composition-contract-version=1",
            "composition-command-version=1",
            "client-artifact-identity=household-ledger-client@1.0.0",
            "client-contract-version=1",
            "client-manifest-path=household-ledger-client.manifest",
            $"client-manifest-sha256={GetSha256(clientManifestPath)}",
            "client-content-root=wwwroot",
            "client-entry-point=index.html",
            "destination=/",
            "file-sha256",
        };
        compositionManifest.AddRange(inventory);

        await File.WriteAllLinesAsync(
            Path.Combine(packageRoot, "household-ledger-client.composition.manifest"),
            compositionManifest,
            new UTF8Encoding(false));
    }

    private static string GetSha256(string path)
    {
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    }
}
