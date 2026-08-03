// <copyright file="OpenApiTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

/// <summary>
/// Verifies the generated language-neutral API description.
/// </summary>
public sealed class OpenApiTests
{
    private static readonly JsonSerializerOptions ArtifactJsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Verifies that runtime OpenAPI describes health success and problem responses.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RuntimeDocumentDescribesHealthContract()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/health")
            .GetProperty("get");
        var responses = operation.GetProperty("responses");
        var hasSuccessJson = responses.GetProperty("200").GetProperty("content")
            .TryGetProperty("application/json", out _);
        var hasProblemJson = responses.GetProperty("500").GetProperty("content")
            .TryGetProperty("application/problem+json", out _);
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var hasHealthSchema = schemas.TryGetProperty("HealthResponse", out _);
        var hasProblemSchema = schemas.TryGetProperty("ProblemDetails", out _);
        var hasServers = document.RootElement.TryGetProperty("servers", out var servers);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        Assert.That(hasServers && servers.GetArrayLength() > 0, Is.False);
        Assert.That(hasSuccessJson, Is.True);
        Assert.That(hasProblemJson, Is.True);
        Assert.That(hasHealthSchema, Is.True);
        Assert.That(hasProblemSchema, Is.True);
    }

    /// <summary>
    /// Verifies that the checked canonical contract matches runtime generation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CheckedDocumentMatchesRuntimeDocument()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);
        var environment = factory.Services.GetRequiredService<IHostEnvironment>();
        var artifactPath = Path.Combine(environment.ContentRootPath, "openapi", "v1.json");

        var runtimeJson = await client.GetStringAsync("/openapi/v1.json");
        var formattedRuntimeJson = FormatJson(runtimeJson);

        if (Environment.GetEnvironmentVariable("UPDATE_OPENAPI_ARTIFACT") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(artifactPath)!);
            await File.WriteAllTextAsync(
                artifactPath,
                formattedRuntimeJson + Environment.NewLine);
        }

        Assert.That(File.Exists(artifactPath), Is.True, $"Missing OpenAPI artifact: {artifactPath}");

        var checkedJson = await File.ReadAllTextAsync(artifactPath);
        Assert.That(FormatJson(checkedJson), Is.EqualTo(formattedRuntimeJson));
    }

    private static string FormatJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement, ArtifactJsonOptions);
    }
}
