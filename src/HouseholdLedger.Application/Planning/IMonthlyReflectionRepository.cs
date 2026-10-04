// <copyright file="IMonthlyReflectionRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Planning;

using HouseholdLedger.Domain.Planning;

/// <summary>Persists standalone monthly reflections.</summary>
public interface IMonthlyReflectionRepository
{
    /// <summary>Finds a saved reflection.</summary>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reflection, or null when absent.</returns>
    Task<MonthlyReflection?> FindAsync(DateOnly month, CancellationToken cancellationToken);

    /// <summary>Creates or replaces a month's reflection.</summary>
    /// <param name="reflection">The reflection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing persistence.</returns>
    Task UpsertAsync(MonthlyReflection reflection, CancellationToken cancellationToken);
}
