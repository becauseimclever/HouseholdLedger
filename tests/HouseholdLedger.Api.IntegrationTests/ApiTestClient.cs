// <copyright file="ApiTestClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using Microsoft.AspNetCore.Mvc.Testing;

internal static class ApiTestClient
{
    private static readonly Uri HttpsBaseAddress = new("https://localhost");

    public static HttpClient Create(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = HttpsBaseAddress,
        });
    }
}
