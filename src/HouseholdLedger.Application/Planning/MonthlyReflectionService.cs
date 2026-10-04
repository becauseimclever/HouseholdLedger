// <copyright file="MonthlyReflectionService.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Planning;

using HouseholdLedger.Domain.Planning;

/// <summary>Reads and revises optional reflections without requiring monthly plans.</summary>
public sealed class MonthlyReflectionService(IMonthlyReflectionRepository repository, TimeProvider clock)
{
    /// <summary>Gets a standalone monthly reflection.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reflection, or null when absent.</returns>
    public Task<MonthlyReflection?> GetAsync(int year, int month, CancellationToken cancellationToken = default) =>
        repository.FindAsync(new DateOnly(year, month, 1), cancellationToken);

    /// <summary>Creates or replaces a standalone monthly reflection.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="whatWorked">Optional observations about what worked.</param>
    /// <param name="nextMonthIntention">The optional next-month intention.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved reflection.</returns>
    public async Task<MonthlyReflection> SaveAsync(
        int year,
        int month,
        string? whatWorked,
        string? nextMonthIntention,
        CancellationToken cancellationToken = default)
    {
        var reflection = new MonthlyReflection(new DateOnly(year, month, 1), whatWorked, nextMonthIntention, clock.GetUtcNow());
        await repository.UpsertAsync(reflection, cancellationToken);
        return reflection;
    }
}
