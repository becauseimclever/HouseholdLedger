// <copyright file="ApiContractTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts.Tests;

using System.Text.Json;
using System.Xml.Linq;

using HouseholdLedger.Api.Contracts;
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
    [Test]
    public void CheckedOpenApiDescribesHealthTransportContract()
    {
        using var document = LoadOpenApiDocument();
        var root = document.RootElement;
        var operation = root.GetProperty("paths").GetProperty("/api/v1/health").GetProperty("get");
        var responses = operation.GetProperty("responses");
        var healthSchema = root.GetProperty("components").GetProperty("schemas")
            .GetProperty("HealthResponse");
        var requiredProperties = healthSchema.GetProperty("required")
            .EnumerateArray()
            .Select(property => property.GetString());

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("openapi").GetString(), Is.EqualTo("3.1.1"));
            Assert.That(
                responses.GetProperty("200").GetProperty("content").GetProperty("application/json")
                    .GetProperty("schema").GetProperty("$ref").GetString(),
                Is.EqualTo("#/components/schemas/HealthResponse"));
            Assert.That(
                responses.GetProperty("500").GetProperty("content").GetProperty("application/problem+json")
                    .GetProperty("schema").GetProperty("$ref").GetString(),
                Is.EqualTo("#/components/schemas/ProblemDetails"));
            Assert.That(healthSchema.GetProperty("type").GetString(), Is.EqualTo("object"));
            Assert.That(requiredProperties, Is.EqualTo(RequiredHealthProperties));
            Assert.That(
                healthSchema.GetProperty("properties").GetProperty("status").GetProperty("type").GetString(),
                Is.EqualTo("string"));
        });
    }

    /// <summary>
    /// Verifies that a .NET client can consume JSON conforming to the checked OpenAPI schema.
    /// </summary>
    [Test]
    public void HealthResponseConsumesLanguageNeutralJsonContract()
    {
        const string languageNeutralJson = """{"status":"available"}""";

        using var openApiDocument = LoadOpenApiDocument();
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

    private static JsonDocument LoadOpenApiDocument()
    {
        var artifactPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "HouseholdLedger.Api",
            "openapi",
            "v1.json");
        return JsonDocument.Parse(File.ReadAllText(artifactPath));
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
