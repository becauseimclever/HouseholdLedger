// <copyright file="ApiContractTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts.Tests;

using System.Text.Json;
using System.Xml.Linq;

using HouseholdLedger.Api.Contracts;
using Microsoft.OpenApi;
using NUnit.Framework;

/// <summary>
/// Verifies the checked language-neutral contract and .NET transport types.
/// </summary>
public sealed class ApiContractTests
{
    private static readonly string[] RequiredHealthProperties = ["status"];
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Verifies that the checked OpenAPI document describes the health transport contract.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task CheckedOpenApiDescribesHealthTransportContract()
    {
        await using var stream = File.OpenRead(GetOpenApiArtifactPath());
        var readResult = await OpenApiDocument.LoadAsync(
            stream,
            "json",
            settings: null,
            TestContext.CurrentContext.CancellationToken);
        var document = readResult.Document
            ?? throw new InvalidDataException("The checked OpenAPI artifact did not produce a document.");
        var diagnostic = readResult.Diagnostic
            ?? throw new InvalidDataException("The checked OpenAPI artifact did not produce parser diagnostics.");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.SpecificationVersion, Is.EqualTo(OpenApiSpecVersion.OpenApi3_1));
            Assert.That(diagnostic.Errors, Is.Empty);
            Assert.That(diagnostic.Warnings, Is.Empty);
        });

        var paths = document.Paths
            ?? throw new InvalidDataException("The checked OpenAPI artifact does not define paths.");
        Assert.That(paths, Contains.Key("/api/v1/health"));
        var pathItem = paths["/api/v1/health"];
        var operations = pathItem.Operations
            ?? throw new InvalidDataException("The health path does not define operations.");
        Assert.That(operations, Contains.Key(HttpMethod.Get));
        var operation = operations[HttpMethod.Get];
        var responses = operation.Responses
            ?? throw new InvalidDataException("The health GET operation does not define responses.");
        Assert.That(responses, Does.ContainKey("200").And.ContainKey("500"));
        var successContent = responses["200"].Content
            ?? throw new InvalidDataException("The health success response does not define content.");
        Assert.That(successContent, Contains.Key("application/json"));
        var errorContent = responses["500"].Content
            ?? throw new InvalidDataException("The health error response does not define content.");
        Assert.That(errorContent, Contains.Key("application/problem+json"));
        var healthSchema = document.Components?.Schemas?["HealthResponse"]
            ?? throw new InvalidDataException("The checked OpenAPI artifact does not define HealthResponse.");
        var successSchema = successContent["application/json"].Schema;
        var errorSchema = errorContent["application/problem+json"].Schema;
        var successSchemaReference = successSchema as OpenApiSchemaReference;
        var errorSchemaReference = errorSchema as OpenApiSchemaReference;

        Assert.Multiple(() =>
        {
            Assert.That(successSchemaReference, Is.Not.Null);
            Assert.That(
                successSchemaReference?.Reference.ReferenceV3,
                Is.EqualTo("#/components/schemas/HealthResponse"));
            Assert.That(errorSchemaReference, Is.Not.Null);
            Assert.That(
                errorSchemaReference?.Reference.ReferenceV3,
                Is.EqualTo("#/components/schemas/ProblemDetails"));
            Assert.That(healthSchema.Type, Is.EqualTo(JsonSchemaType.Object));
            Assert.That(healthSchema.Required, Is.EqualTo(RequiredHealthProperties));
            Assert.That(healthSchema.Properties?["status"].Type, Is.EqualTo(JsonSchemaType.String));
        });
    }

    /// <summary>
    /// Verifies that a .NET client can consume JSON conforming to the checked OpenAPI schema.
    /// </summary>
    [Test]
    public void HealthResponseConsumesLanguageNeutralJsonContract()
    {
        const string languageNeutralJson = """{"status":"available"}""";

        using var openApiDocument = LoadOpenApiJsonDocument();
        var schemaProperties = openApiDocument.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("HealthResponse").GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();
        var response = new HealthResponse("available");
        var serializedJson = JsonSerializer.Serialize(response, WebJsonOptions);
        using var serializedDocument = JsonDocument.Parse(serializedJson);
        var serializedProperties = serializedDocument.RootElement.EnumerateObject().ToArray();
        var consumedResponse = JsonSerializer.Deserialize<HealthResponse>(languageNeutralJson, WebJsonOptions);

        Assert.Multiple(() =>
        {
            Assert.That(serializedProperties.Select(property => property.Name), Is.EqualTo(schemaProperties));
            Assert.That(serializedProperties[0].Value.ValueKind, Is.EqualTo(JsonValueKind.String));
            Assert.That(consumedResponse, Is.EqualTo(response));
        });
    }

    /// <summary>
    /// Verifies that the .NET convenience contract has no HouseholdLedger implementation dependency.
    /// </summary>
    [Test]
    public void ApiContractsHasNoHouseholdLedgerProjectOrAssemblyDependencies()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "HouseholdLedger.Api.Contracts",
            "HouseholdLedger.Api.Contracts.csproj");
        var projectDocument = XDocument.Load(projectPath);
        var projectReferences = projectDocument.Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference");
        var householdLedgerAssemblyReferences = typeof(HealthResponse).Assembly.GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("HouseholdLedger.", StringComparison.Ordinal) == true);
        var leakedImplementationTypes = typeof(HealthResponse).Assembly.GetExportedTypes()
            .Where(type => type.Namespace != typeof(HealthResponse).Namespace);

        Assert.Multiple(() =>
        {
            Assert.That(projectReferences, Is.Empty);
            Assert.That(householdLedgerAssemblyReferences, Is.Empty);
            Assert.That(leakedImplementationTypes, Is.Empty);
        });
    }

    private static JsonDocument LoadOpenApiJsonDocument()
    {
        return JsonDocument.Parse(File.ReadAllText(GetOpenApiArtifactPath()));
    }

    private static string GetOpenApiArtifactPath()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "src",
            "HouseholdLedger.Api",
            "openapi",
            "v1.json");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HouseholdLedger.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the HouseholdLedger repository root.");
    }
}
