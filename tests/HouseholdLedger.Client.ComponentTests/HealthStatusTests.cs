// <copyright file="HealthStatusTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Net;
using System.Text;

using Bunit;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

/// <summary>
/// Verifies API availability presentation through a controlled HTTP boundary.
/// </summary>
public sealed class HealthStatusTests
{
    /// <summary>
    /// Verifies the loading state while the health response is pending.
    /// </summary>
    [Test]
    public void HealthStatusShowsLoadingWhileRequestIsPending()
    {
        var pendingResponse = new TaskCompletionSource<HttpResponseMessage>();
        using var context = CreateContext(new DelegateHandler((_, _) => pendingResponse.Task));

        var component = context.Render<HealthStatus>();

        Assert.Multiple(() =>
        {
            Assert.That(component.Find("[role='status']").TextContent, Does.Contain("Checking API availability"));
            Assert.That(component.Find("[role='status']").GetAttribute("aria-live"), Is.EqualTo("polite"));
            Assert.That(component.FindAll("button"), Is.Empty);
        });
    }

    /// <summary>
    /// Verifies the available state after a successful documented health response.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HealthStatusShowsAvailableForSuccessfulResponse()
    {
        using var context = CreateContext(new DelegateHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, "{\"status\":\"available\"}"))));

        var component = context.Render<HealthStatus>();

        await component.WaitForAssertionAsync(() => Assert.Multiple(() =>
        {
            Assert.That(component.Find("[role='status']").TextContent, Does.Contain("API available"));
            Assert.That(component.Find("[role='status']").GetAttribute("aria-live"), Is.EqualTo("polite"));
            Assert.That(component.FindAll("[role='alert']"), Is.Empty);
        }));
    }

    /// <summary>
    /// Verifies that a non-success response is presented as unavailable.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HealthStatusShowsUnavailableForNonSuccessResponse()
    {
        using var context = CreateContext(new DelegateHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));

        var component = context.Render<HealthStatus>();

        await component.WaitForAssertionAsync(() =>
        {
            Assert.That(component.Find("[role='alert']").TextContent, Does.Contain("API unavailable"));
            Assert.That(component.Find("button").TextContent, Does.Contain("Try again"));
            Assert.That(component.Find("button").GetAttribute("type"), Is.EqualTo("button"));
        });
    }

    /// <summary>
    /// Verifies that malformed successful JSON is presented as a recoverable error.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HealthStatusShowsErrorFallbackForMalformedSuccessfulJson()
    {
        using var context = CreateContext(new DelegateHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, "not-json"))));

        var component = context.Render<HealthStatus>();

        await component.WaitForAssertionAsync(() => Assert.Multiple(() =>
        {
            Assert.That(component.Find("[role='alert']").TextContent, Does.Contain("We couldn't check the API"));
            Assert.That(component.Find("button").TextContent, Does.Contain("Try again"));
            Assert.That(component.Find("button").GetAttribute("type"), Is.EqualTo("button"));
            Assert.That(component.FindAll("[role='status']"), Is.Empty);
        }));
    }

    /// <summary>
    /// Verifies that a transport error is recoverable through the retry control.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HealthStatusCanRetryAfterTransportError()
    {
        var attempt = 0;
        using var context = CreateContext(new DelegateHandler((_, _) =>
        {
            attempt++;
            return attempt == 1
                ? Task.FromException<HttpResponseMessage>(new HttpRequestException("Offline"))
                : Task.FromResult(JsonResponse(HttpStatusCode.OK, "{\"status\":\"available\"}"));
        }));
        var component = context.Render<HealthStatus>();

        await component.WaitForAssertionAsync(() =>
            Assert.That(component.Find("[role='alert']").TextContent, Does.Contain("couldn't check")));

        await component.Find("button").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() =>
            Assert.That(component.Find("[role='status']").TextContent, Does.Contain("API available")));
        Assert.That(attempt, Is.EqualTo(2));
    }

    private static BunitContext CreateContext(HttpMessageHandler handler)
    {
        var context = new BunitContext();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/"),
        };
        context.Services.AddSingleton(httpClient);
        context.Services.AddSingleton<IHealthApiClient, HealthApiClient>();
        return context;
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
