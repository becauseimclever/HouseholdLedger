// <copyright file="ApiContractTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts.Tests;

using System.Text.Json;
using System.Xml.Linq;

using HouseholdLedger.Api.Contracts;
using Microsoft.OpenApi;
using Xunit;

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
    [Fact]
    public async Task CheckedOpenApiDescribesHealthTransportContract()
    {
        await using var stream = File.OpenRead(GetOpenApiArtifactPath());
        var readResult = await OpenApiDocument.LoadAsync(
            stream,
            "json",
            settings: null,
            TestContext.Current.CancellationToken);
        var document = readResult.Document
            ?? throw new InvalidDataException("The checked OpenAPI artifact did not produce a document.");
        var diagnostic = readResult.Diagnostic
            ?? throw new InvalidDataException("The checked OpenAPI artifact did not produce parser diagnostics.");

        Assert.Multiple(
            () => Assert.Equal(OpenApiSpecVersion.OpenApi3_1, diagnostic.SpecificationVersion),
            () => Assert.Empty(diagnostic.Errors),
            () => Assert.Empty(diagnostic.Warnings));

        var paths = document.Paths
            ?? throw new InvalidDataException("The checked OpenAPI artifact does not define paths.");
        Assert.Contains("/api/v1/health", paths);
        var pathItem = paths["/api/v1/health"];
        var operations = pathItem.Operations
            ?? throw new InvalidDataException("The health path does not define operations.");
        Assert.Contains(HttpMethod.Get, operations);
        var operation = operations[HttpMethod.Get];
        var responses = operation.Responses
            ?? throw new InvalidDataException("The health GET operation does not define responses.");
        Assert.Multiple(
            () => Assert.Contains("200", responses),
            () => Assert.Contains("500", responses));
        var successContent = responses["200"].Content
            ?? throw new InvalidDataException("The health success response does not define content.");
        Assert.Contains("application/json", successContent);
        var errorContent = responses["500"].Content
            ?? throw new InvalidDataException("The health error response does not define content.");
        Assert.Contains("application/problem+json", errorContent);
        var healthSchema = document.Components?.Schemas?["HealthResponse"]
            ?? throw new InvalidDataException("The checked OpenAPI artifact does not define HealthResponse.");
        var successSchema = successContent["application/json"].Schema;
        var errorSchema = errorContent["application/problem+json"].Schema;
        var successSchemaReference = successSchema as OpenApiSchemaReference;
        var errorSchemaReference = errorSchema as OpenApiSchemaReference;

        Assert.Multiple(
            () => Assert.NotNull(successSchemaReference),
            () => Assert.Equal(
                "#/components/schemas/HealthResponse",
                successSchemaReference?.Reference.ReferenceV3),
            () => Assert.NotNull(errorSchemaReference),
            () => Assert.Equal(
                "#/components/schemas/ProblemDetails",
                errorSchemaReference?.Reference.ReferenceV3),
            () => Assert.Equal(JsonSchemaType.Object, healthSchema.Type),
            () => Assert.Equal(RequiredHealthProperties, healthSchema.Required),
            () => Assert.Equal(JsonSchemaType.String, healthSchema.Properties?["status"].Type));
    }

    /// <summary>
    /// Verifies that a .NET client can consume JSON conforming to the checked OpenAPI schema.
    /// </summary>
    [Fact]
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

        Assert.Multiple(
            () => Assert.Equal(schemaProperties, serializedProperties.Select(property => property.Name)),
            () => Assert.Equal(JsonValueKind.String, serializedProperties[0].Value.ValueKind),
            () => Assert.Equal(response, consumedResponse));
    }

    /// <summary>Verifies the checked contract describes date-scoped transaction correction and removal.</summary>
    [Fact]
    public void CheckedOpenApiDescribesTransactionMutationContract()
    {
        using var document = LoadOpenApiJsonDocument();
        var root = document.RootElement;
        var resource = root.GetProperty("paths")
            .GetProperty("/api/v1/days/{ledgerDate}/transactions/{transactionId}");
        var revise = resource.GetProperty("put");
        var remove = resource.GetProperty("delete");
        var reviseResponses = revise.GetProperty("responses");
        var removeResponses = remove.GetProperty("responses");
        var requestSchemaReference = revise.GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        var updateSchema = root.GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("UpdateExpenseTransactionRequest");
        var required = updateSchema.GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        var createRequired = root.GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("CreateExpenseTransactionRequest")
            .GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        var responseRequired = root.GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("ExpenseTransactionResponse")
            .GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Multiple(
            () => Assert.Equal(
                "#/components/schemas/UpdateExpenseTransactionRequest",
                requestSchemaReference),
            () => Assert.True(reviseResponses.TryGetProperty("200", out _)),
            () => Assert.True(reviseResponses.TryGetProperty("400", out _)),
            () => Assert.True(reviseResponses.TryGetProperty("404", out _)),
            () => Assert.True(removeResponses.TryGetProperty("204", out _)),
            () => Assert.True(removeResponses.TryGetProperty("404", out _)),
            () => Assert.Equal(["accountId", "amount", "classification"], createRequired),
            () => Assert.Equal(["accountId", "amount", "classification"], required),
            () => Assert.Equal(
                ["id", "accountId", "accountName", "date", "amount", "classification"],
                responseRequired));
    }

    /// <summary>Verifies the checked contract describes account listing and creation.</summary>
    [Fact]
    public void CheckedOpenApiDescribesAccountCatalogContract()
    {
        using var document = LoadOpenApiJsonDocument();
        var root = document.RootElement;
        var collection = root.GetProperty("paths").GetProperty("/api/v1/accounts");
        var listResponses = collection.GetProperty("get").GetProperty("responses");
        var create = collection.GetProperty("post");
        var createResponses = create.GetProperty("responses");
        var requestReference = create.GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        var requestRequired = root.GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("CreateAccountRequest")
            .GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        var responseRequired = root.GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("AccountResponse")
            .GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Multiple(
            () => Assert.True(listResponses.TryGetProperty("200", out _)),
            () => Assert.True(createResponses.TryGetProperty("201", out _)),
            () => Assert.True(createResponses.TryGetProperty("400", out _)),
            () => Assert.Equal("#/components/schemas/CreateAccountRequest", requestReference),
            () => Assert.Equal(["name"], requestRequired),
            () => Assert.Equal(["id", "name"], responseRequired));
    }

    /// <summary>
    /// Verifies that the .NET convenience contract has no HouseholdLedger implementation dependency.
    /// </summary>
    [Fact]
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

        Assert.Multiple(
            () => Assert.Empty(projectReferences),
            () => Assert.Empty(householdLedgerAssemblyReferences),
            () => Assert.Empty(leakedImplementationTypes));
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
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

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
