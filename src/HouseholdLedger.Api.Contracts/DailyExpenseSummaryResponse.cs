// <copyright file="DailyExpenseSummaryResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>
/// Describes one day's expense total and month-to-date spending.
/// </summary>
/// <param name="Date">The summarized ledger date.</param>
/// <param name="DailyTotal">The total expenses recorded on the date.</param>
/// <param name="MonthToDateTotal">The total expenses from the first of the month through the date.</param>
public sealed record DailyExpenseSummaryResponse(
    DateOnly Date,
    decimal DailyTotal,
    decimal MonthToDateTotal);
