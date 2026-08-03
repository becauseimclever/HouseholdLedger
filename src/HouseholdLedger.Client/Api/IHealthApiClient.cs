// <copyright file="IHealthApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

/// <summary>
/// Checks the documented API health resource.
/// </summary>
public interface IHealthApiClient
{
    /// <summary>
    /// Checks whether the API reports itself as available.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns><see langword="true"/> when the API reports availability.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
