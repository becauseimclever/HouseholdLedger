// <copyright file="IMonthlyExpenseSummaryApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using HouseholdLedger.Api.Contracts;

/// <summary>
/// Reads calendar-ready monthly expense summaries.
/// </summary>
public interface IMonthlyExpenseSummaryApiClient
{
    /// <summary>Gets daily and month-to-date expense totals for one month.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One summary for every day in the month.</returns>
    Task<IReadOnlyList<DailyExpenseSummaryResponse>> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken);
}
