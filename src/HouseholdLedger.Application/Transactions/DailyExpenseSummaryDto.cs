// <copyright file="DailyExpenseSummaryDto.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Transactions;

/// <summary>
/// Summarizes one day's expenses and month-to-date spending.
/// </summary>
/// <param name="Date">The summarized ledger date.</param>
/// <param name="DailyTotal">The total expenses recorded on the date.</param>
/// <param name="MonthToDateTotal">The total expenses from the first of the month through the date.</param>
public sealed record DailyExpenseSummaryDto(
    DateOnly Date,
    decimal DailyTotal,
    decimal MonthToDateTotal);
