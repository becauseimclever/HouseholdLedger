// <copyright file="MonthlyExpenseSummaryController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Provides calendar-ready monthly expense summaries.
/// </summary>
[ApiController]
[Route("api/v1/months/{year:int:min(1):max(9999)}/{month:int:min(1):max(12)}/expense-summary")]
public sealed class MonthlyExpenseSummaryController(ExpenseTransactionService service) : ControllerBase
{
    /// <summary>Gets daily totals and month-to-date spending for one calendar month.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>One summary for every day in the month.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DailyExpenseSummaryResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<DailyExpenseSummaryResponse>>> Get(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var summaries = await service.SummarizeMonthAsync(year, month, cancellationToken);
        return this.Ok(summaries.Select(summary => new DailyExpenseSummaryResponse(
            summary.Date,
            summary.DailyTotal,
            summary.MonthToDateTotal)));
    }
}
