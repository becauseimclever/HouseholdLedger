// <copyright file="IMonthlyBudgetApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using HouseholdLedger.Api.Contracts;

/// <summary>Provides deliberate monthly plans and authoritative reviews.</summary>
public interface IMonthlyBudgetApiClient
{
    /// <summary>Reads the plan, or null when no plan exists.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The saved plan.</returns>
    Task<MonthlyBudgetPlanResponse?> GetPlanAsync(int year, int month, CancellationToken cancellationToken);

    /// <summary>Saves an explicitly reconciled plan.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="request">The intended allocation.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The saved plan.</returns>
    Task<MonthlyBudgetPlanResponse> SavePlanAsync(int year, int month, MonthlyBudgetPlanRequest request, CancellationToken cancellationToken);

    /// <summary>Reads the backend-calculated monthly comparison.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The authoritative review.</returns>
    Task<MonthlyBudgetReviewResponse> GetReviewAsync(int year, int month, CancellationToken cancellationToken);
}
