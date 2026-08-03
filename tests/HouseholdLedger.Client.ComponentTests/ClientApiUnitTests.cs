// <copyright file="ClientApiUnitTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Net;
using System.Text;
using System.Text.Json;

using HouseholdLedger.Client.Api;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

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
    [TestCase("http://api.example.test", "http://api.example.test/")]
    [TestCase("https://api.example.test/root/", "https://api.example.test/root/")]
    public void ApiConfigurationReturnsNormalizedHttpBaseAddress(
        string configuredValue,
        string expectedValue)
    {
        var configuration = new ConfigurationManager
        {
            ["Api:BaseUrl"] = configuredValue,
        };

        var result = ApiConfiguration.GetBaseAddress(configuration);

        Assert.That(result, Is.EqualTo(new Uri(expectedValue)));
    }

    /// <summary>
    /// Verifies that missing and unsupported base addresses fail with actionable configuration guidance.
    /// </summary>
    /// <param name="configuredValue">The invalid configured value.</param>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("api.example.test")]
    [TestCase("ftp://api.example.test")]
    public void ApiConfigurationRejectsInvalidBaseAddress(string? configuredValue)
    {
        var configuration = new ConfigurationManager
        {
            ["Api:BaseUrl"] = configuredValue,
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => ApiConfiguration.GetBaseAddress(configuration));

        Assert.That(
            exception!.Message,
            Is.EqualTo("Static configuration 'Api:BaseUrl' must be an absolute HTTP or HTTPS URL."));
    }

    /// <summary>
    /// Verifies the exact health request URI and documented available JSON outcome.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HealthApiClientRequestsVersionedHealthUriAndReturnsAvailable()
    {
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(new DelegateHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{\"status\":\"available\"}"));
        }));
        var client = new HealthApiClient(httpClient);

        var result = await client.IsAvailableAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(
                capturedRequest.RequestUri,
                Is.EqualTo(new Uri("https://api.example.test/root/api/v1/health")));
        });
    }

    /// <summary>
    /// Verifies that successful valid JSON must contain the exact documented availability value.
    /// </summary>
    /// <param name="content">The successful JSON response body.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestCase("{\"status\":\"unavailable\"}")]
    [TestCase("{\"status\":\"Available\"}")]
    [TestCase("{\"status\":null}")]
    public async Task HealthApiClientReturnsFalseForOtherSuccessfulJson(string content)
    {
        using var httpClient = CreateHttpClient(new DelegateHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, content))));
        var client = new HealthApiClient(httpClient);

        var result = await client.IsAvailableAsync();

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that malformed successful JSON is exposed to the presentation fallback.
    /// </summary>
    [Test]
    public void HealthApiClientThrowsForMalformedSuccessfulJson()
    {
        using var httpClient = CreateHttpClient(new DelegateHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, "not-json"))));
        var client = new HealthApiClient(httpClient);

        Assert.That(
            async () => await client.IsAvailableAsync(),
            Throws.TypeOf<JsonException>());
    }

    /// <summary>
    /// Verifies that an unsuccessful response returns false without reading its body as JSON.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HealthApiClientReturnsFalseForUnsuccessfulResponse()
    {
        using var httpClient = CreateHttpClient(new DelegateHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.ServiceUnavailable, "not-json"))));
        var client = new HealthApiClient(httpClient);

        var result = await client.IsAvailableAsync();

        Assert.That(result, Is.False);
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
