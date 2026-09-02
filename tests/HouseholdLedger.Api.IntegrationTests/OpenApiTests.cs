// <copyright file="OpenApiTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

/// <summary>
/// Verifies the generated language-neutral API description.
/// </summary>
public sealed class OpenApiTests
{
    private static readonly string[] ExpectedHealthRequiredProperties = ["status"];

    private static readonly JsonSerializerOptions ArtifactJsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Verifies that runtime OpenAPI describes health success and problem responses.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RuntimeDocumentDescribesHealthContract()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.GetAsync(
            "/openapi/v1.json",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/health")
            .GetProperty("get");
        var responses = operation.GetProperty("responses");
        var successSchema = responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");
        var problemSchema = responses.GetProperty("500").GetProperty("content")
            .GetProperty("application/problem+json").GetProperty("schema");
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var healthSchema = schemas.GetProperty("HealthResponse");
        var healthRequiredProperties = healthSchema.GetProperty("required")
            .EnumerateArray()
            .Select(property => property.GetString())
            .ToArray();
        var healthStatusSchema = healthSchema.GetProperty("properties").GetProperty("status");
        var hasServers = document.RootElement.TryGetProperty("servers", out var servers);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("3.1.", document.RootElement.GetProperty("openapi").GetString(), StringComparison.Ordinal);
        Assert.False(hasServers && servers.GetArrayLength() > 0);
        Assert.Equal("#/components/schemas/HealthResponse", successSchema.GetProperty("$ref").GetString());
        Assert.Equal("#/components/schemas/ProblemDetails", problemSchema.GetProperty("$ref").GetString());
        Assert.Equal(ExpectedHealthRequiredProperties, healthRequiredProperties);
        Assert.Equal("object", healthSchema.GetProperty("type").GetString());
        Assert.Equal("string", healthStatusSchema.GetProperty("type").GetString());
        Assert.True(schemas.TryGetProperty("ProblemDetails", out _));
    }

    /// <summary>
    /// Verifies that the checked canonical contract matches runtime generation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CheckedDocumentMatchesRuntimeDocument()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);
        var environment = factory.Services.GetRequiredService<IHostEnvironment>();
        var artifactPath = Path.Combine(environment.ContentRootPath, "openapi", "v1.json");

        var runtimeJson = await client.GetStringAsync(
            "/openapi/v1.json",
            TestContext.Current.CancellationToken);
        var formattedRuntimeJson = FormatJson(runtimeJson);

        Assert.True(File.Exists(artifactPath), $"Missing OpenAPI artifact: {artifactPath}");

        var checkedJson = await File.ReadAllTextAsync(
            artifactPath,
            TestContext.Current.CancellationToken);
        Assert.Equal(formattedRuntimeJson, FormatJson(checkedJson));
    }

    private static string FormatJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement, ArtifactJsonOptions);
    }
}
