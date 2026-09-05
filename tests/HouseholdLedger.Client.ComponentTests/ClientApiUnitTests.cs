// <copyright file="ClientApiUnitTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Net;
using System.Text;
using System.Text.Json;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using Microsoft.Extensions.Configuration;
using Xunit;

/// <summary>
/// Verifies client API configuration and transport behavior at the unit-test layer.
/// </summary>
public sealed class ClientApiUnitTests
{
    /// <summary>
    /// Verifies that valid HTTP base addresses are normalized for relative requests.
    /// </summary>
    /// <param name="configuredValue">The configured base address.</param>
    /// <param name="expectedValue">The normalized base address.</param>
    [Theory]
    [InlineData("http://api.example.test", "http://api.example.test/")]
    [InlineData("https://api.example.test/root/", "https://api.example.test/root/")]
    public void ApiConfigurationReturnsNormalizedHttpBaseAddress(
        string configuredValue,
        string expectedValue)
    {
        var configuration = new ConfigurationManager
        {
            ["Api:BaseUrl"] = configuredValue,
        };

        var result = ApiConfiguration.GetBaseAddress(configuration);

        Assert.Equal(new Uri(expectedValue), result);
    }

    /// <summary>
    /// Verifies that missing and unsupported base addresses fail with actionable configuration guidance.
    /// </summary>
    /// <param name="configuredValue">The invalid configured value.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("api.example.test")]
    [InlineData("ftp://api.example.test")]
    public void ApiConfigurationRejectsInvalidBaseAddress(string? configuredValue)
    {
        var configuration = new ConfigurationManager
        {
            ["Api:BaseUrl"] = configuredValue,
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => ApiConfiguration.GetBaseAddress(configuration));

        Assert.Equal(
            "Static configuration 'Api:BaseUrl' must be an absolute HTTP or HTTPS URL.",
            exception.Message);
    }

    /// <summary>
    /// Verifies the exact health request URI and documented available JSON outcome.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task HealthApiClientRequestsVersionedHealthUriAndReturnsAvailable()
    {
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(new DelegateHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{\"status\":\"available\"}"));
        }));
        var client = new HealthApiClient(httpClient);

        var result = await client.IsAvailableAsync(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.True(result),
            () => Assert.NotNull(capturedRequest),
            () => Assert.Equal(HttpMethod.Get, capturedRequest!.Method),
            () => Assert.Equal(
                new Uri("https://api.example.test/root/api/v1/health"),
                capturedRequest!.RequestUri));
    }

    /// <summary>
    /// Verifies that successful valid JSON must contain the exact documented availability value.
    /// </summary>
    /// <param name="content">The successful JSON response body.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData("{\"status\":\"unavailable\"}")]
    [InlineData("{\"status\":\"Available\"}")]
    [InlineData("{\"status\":null}")]
    public async Task HealthApiClientReturnsFalseForOtherSuccessfulJson(string content)
    {
        using var httpClient = CreateHttpClient(new DelegateHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, content))));
        var client = new HealthApiClient(httpClient);

        var result = await client.IsAvailableAsync(TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    /// <summary>
    /// Verifies that malformed successful JSON is exposed to the presentation fallback.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task HealthApiClientThrowsForMalformedSuccessfulJson()
    {
        using var httpClient = CreateHttpClient(new DelegateHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, "not-json"))));
        var client = new HealthApiClient(httpClient);

        await Assert.ThrowsAsync<JsonException>(
            () => client.IsAvailableAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that an unsuccessful response returns false without reading its body as JSON.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task HealthApiClientReturnsFalseForUnsuccessfulResponse()
    {
        using var httpClient = CreateHttpClient(new DelegateHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.ServiceUnavailable, "not-json"))));
        var client = new HealthApiClient(httpClient);

        var result = await client.IsAvailableAsync(TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    /// <summary>Verifies account listing uses the versioned collection URI and consumes JSON.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AccountsApiClientListsVersionedCollection()
    {
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(new DelegateHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                "[{\"id\":\"10000000-0000-0000-0000-000000000001\",\"name\":\"Household Checking\"}]"));
        }));
        var client = new AccountsApiClient(httpClient);

        var accounts = await client.ListAsync(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpMethod.Get, capturedRequest?.Method),
            () => Assert.Equal(new Uri("https://api.example.test/root/api/v1/accounts"), capturedRequest?.RequestUri),
            () => Assert.Equal("Household Checking", Assert.Single(accounts).Name));
    }

    /// <summary>Verifies account creation serializes the request and maps documented outcomes.</summary>
    /// <param name="statusCode">The HTTP response status.</param>
    /// <param name="expected">The expected creation result.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(HttpStatusCode.Created, AccountCreationResult.Success)]
    [InlineData(HttpStatusCode.BadRequest, AccountCreationResult.Invalid)]
    public async Task AccountsApiClientCreatesVersionedResource(
        HttpStatusCode statusCode,
        AccountCreationResult expected)
    {
        HttpMethod? capturedMethod = null;
        Uri? capturedUri = null;
        string? capturedContent = null;
        using var httpClient = CreateHttpClient(new DelegateHandler(async (request, cancellationToken) =>
        {
            capturedMethod = request.Method;
            capturedUri = request.RequestUri;
            capturedContent = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse(statusCode, "{}");
        }));
        var client = new AccountsApiClient(httpClient);

        var result = await client.CreateAsync(
            new CreateAccountRequest("Household Checking"),
            TestContext.Current.CancellationToken);

        using var content = JsonDocument.Parse(capturedContent!);
        Assert.Multiple(
            () => Assert.Equal(expected, result),
            () => Assert.Equal(HttpMethod.Post, capturedMethod),
            () => Assert.Equal(new Uri("https://api.example.test/root/api/v1/accounts"), capturedUri),
            () => Assert.Equal("Household Checking", content.RootElement.GetProperty("name").GetString()));
    }

    /// <summary>Verifies transaction correction uses the date-scoped resource and maps expected outcomes.</summary>
    /// <param name="statusCode">The HTTP response status.</param>
    /// <param name="expected">The expected mutation result.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(HttpStatusCode.OK, TransactionMutationResult.Success)]
    [InlineData(HttpStatusCode.BadRequest, TransactionMutationResult.Invalid)]
    [InlineData(HttpStatusCode.NotFound, TransactionMutationResult.NotFound)]
    public async Task TransactionsApiClientRevisesDateScopedResource(
        HttpStatusCode statusCode,
        TransactionMutationResult expected)
    {
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(new DelegateHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(JsonResponse(statusCode, "{}"));
        }));
        var client = new TransactionsApiClient(httpClient);
        var transactionId = Guid.Parse("efc8d3df-a67e-45f3-8b5a-a0303b61f0ed");

        var result = await client.ReviseAsync(
            new DateOnly(2026, 9, 1),
            transactionId,
            new UpdateExpenseTransactionRequest(12.34m, "Culture"),
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(expected, result),
            () => Assert.Equal(HttpMethod.Put, capturedRequest?.Method),
            () => Assert.Equal(
                new Uri($"https://api.example.test/root/api/v1/days/2026-09-01/transactions/{transactionId}"),
                capturedRequest?.RequestUri));
    }

    /// <summary>Verifies transaction removal uses the date-scoped resource.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TransactionsApiClientRemovesDateScopedResource()
    {
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(new DelegateHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }));
        var client = new TransactionsApiClient(httpClient);
        var transactionId = Guid.Parse("efc8d3df-a67e-45f3-8b5a-a0303b61f0ed");

        var result = await client.RemoveAsync(
            new DateOnly(2026, 9, 1),
            transactionId,
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(TransactionMutationResult.Success, result),
            () => Assert.Equal(HttpMethod.Delete, capturedRequest?.Method),
            () => Assert.Equal(
                new Uri($"https://api.example.test/root/api/v1/days/2026-09-01/transactions/{transactionId}"),
                capturedRequest?.RequestUri));
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/root/"),
        };
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string content)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class DelegateHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return response(request, cancellationToken);
        }
    }
}
