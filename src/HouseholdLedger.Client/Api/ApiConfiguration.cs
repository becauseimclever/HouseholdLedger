// <copyright file="ApiConfiguration.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Reads client-owned API transport configuration.
/// </summary>
public static class ApiConfiguration
{
    private const string BaseUrlKey = "Api:BaseUrl";

    /// <summary>
    /// Gets the configured absolute API base address.
    /// </summary>
    /// <param name="configuration">The static client configuration.</param>
    /// <returns>The validated API base address.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is missing or invalid.</exception>
    public static Uri GetBaseAddress(IConfiguration configuration)
    {
        var configuredValue = configuration[BaseUrlKey];
        if (!Uri.TryCreate(configuredValue, UriKind.Absolute, out var baseAddress)
            || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Static configuration '{BaseUrlKey}' must be an absolute HTTP or HTTPS URL.");
        }

        return baseAddress.AbsoluteUri[^1] == '/'
            ? baseAddress
            : new Uri(baseAddress.AbsoluteUri + '/', UriKind.Absolute);
    }
}
