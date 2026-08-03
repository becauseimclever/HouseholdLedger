// <copyright file="ProblemDetailsTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

/// <summary>
/// Verifies framework problem responses through the real ASP.NET Core host.
/// </summary>
public sealed class ProblemDetailsTests
{
    /// <summary>
    /// Verifies that MVC method rejection uses the standard problem shape.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnsupportedHealthMethodReturnsProblemDetails()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.PostAsync("/api/v1/health", content: null);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var problemStatus = document.RootElement.GetProperty("status").GetInt32();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
        Assert.That(mediaType, Is.EqualTo("application/problem+json"));
        Assert.That(problemStatus, Is.EqualTo((int)HttpStatusCode.MethodNotAllowed));
    }
}
