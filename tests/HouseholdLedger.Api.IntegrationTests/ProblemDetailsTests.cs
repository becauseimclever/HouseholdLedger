// <copyright file="ProblemDetailsTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

/// <summary>
/// Verifies framework problem responses through the real ASP.NET Core host.
/// </summary>
public sealed class ProblemDetailsTests
{
    /// <summary>
    /// Verifies that MVC method rejection uses the standard problem shape.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UnsupportedHealthMethodReturnsProblemDetails()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.PostAsync(
            "/api/v1/health",
            content: null,
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var problemStatus = document.RootElement.GetProperty("status").GetInt32();

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("application/problem+json", mediaType);
        Assert.Equal((int)HttpStatusCode.MethodNotAllowed, problemStatus);
    }
}
