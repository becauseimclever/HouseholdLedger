// <copyright file="IMonthlyReflectionApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using HouseholdLedger.Api.Contracts;

/// <summary>Accesses a household's independent monthly reflection.</summary>
public interface IMonthlyReflectionApiClient
{
    /// <summary>Gets the reflection for one month.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The saved reflection, or <see langword="null"/> when none exists.</returns>
    Task<MonthlyReflectionResponse?> GetAsync(int year, int month, CancellationToken cancellationToken);

    /// <summary>Saves the two optional monthly reflection prompts.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="request">The reflection values.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The saved reflection.</returns>
    Task<MonthlyReflectionResponse> SaveAsync(int year, int month, MonthlyReflectionRequest request, CancellationToken cancellationToken);
}
