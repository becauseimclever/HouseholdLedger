// <copyright file="ClientApiUnitTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Net;
using System.Text;
using System.Text.Json;

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
