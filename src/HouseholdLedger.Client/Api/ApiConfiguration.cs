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
            || !IsHttpAddress(baseAddress))
        {
            throw new InvalidOperationException(
                $"Static configuration '{BaseUrlKey}' must be an absolute HTTP or HTTPS URL.");
        }

        return EnsureTrailingSlash(baseAddress);
    }

    /// <summary>
    /// Gets the configured API base address, resolving the site-root setting against the browser base URI.
    /// </summary>
    /// <param name="configuration">The static client configuration.</param>
    /// <param name="browserBaseUri">The browser navigation base URI.</param>
    /// <returns>The validated API base address.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is missing or invalid.</exception>
    public static Uri GetBaseAddress(IConfiguration configuration, Uri browserBaseUri)
    {
        var configuredValue = configuration[BaseUrlKey];
        if (string.Equals(configuredValue, "/", StringComparison.Ordinal))
        {
            return new Uri(browserBaseUri, configuredValue);
        }

        return GetBaseAddress(configuration);
    }

    private static bool IsHttpAddress(Uri address)
    {
        return address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps;
    }

    private static Uri EnsureTrailingSlash(Uri baseAddress)
    {
        return baseAddress.AbsoluteUri[^1] == '/'
            ? baseAddress
            : new Uri(baseAddress.AbsoluteUri + '/', UriKind.Absolute);
    }
}
