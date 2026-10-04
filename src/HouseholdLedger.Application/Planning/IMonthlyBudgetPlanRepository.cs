// <copyright file="IMonthlyBudgetPlanRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Planning;

using HouseholdLedger.Domain.Planning;

/// <summary>Persists monthly budget plans.</summary>
public interface IMonthlyBudgetPlanRepository
{
    /// <summary>Finds a plan for one month.</summary>
    /// <param name="month">The first day of the month to find.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved plan, or <see langword="null"/>.</returns>
    Task<MonthlyBudgetPlan?> FindAsync(DateOnly month, CancellationToken cancellationToken);

    /// <summary>Creates or replaces a plan for one month.</summary>
    /// <param name="plan">The plan to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task UpsertAsync(MonthlyBudgetPlan plan, CancellationToken cancellationToken);
}
