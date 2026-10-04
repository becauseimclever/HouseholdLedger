// <copyright file="MonthlyBudgetPlanController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Planning;
using Microsoft.AspNetCore.Mvc;

/// <summary>Reads and saves intentional budget plans and monthly reviews.</summary>
[ApiController]
[Route("api/v1/months/{year:int:min(1):max(9999)}/{month:int:min(1):max(12)}")]
public sealed class MonthlyBudgetPlanController(MonthlyBudgetPlanService service) : ControllerBase
{
    /// <summary>Gets the saved plan for a calendar month.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved plan when one exists.</returns>
    [HttpGet("budget-plan")]
    [ProducesResponseType<MonthlyBudgetPlanResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<MonthlyBudgetPlanResponse>> GetPlan(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await service.GetAsync(year, month, cancellationToken);
            return plan is null ? this.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Monthly plan not found",
                Detail = "No plan has been saved for this month.",
            }) : this.Ok(Map(plan));
        }
        catch (ArgumentOutOfRangeException)
        {
            return this.NotFound();
        }
    }

    /// <summary>Creates or deliberately revises the plan for a calendar month.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="request">The intended income, savings, and expense classifications.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved plan.</returns>
    [HttpPut("budget-plan")]
    [ProducesResponseType<MonthlyBudgetPlanResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<MonthlyBudgetPlanResponse>> SavePlan(
        int year,
        int month,
        MonthlyBudgetPlanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await service.SaveAsync(
                year,
                month,
                new MonthlyBudgetPlanCommand(
                    request.ExpectedIncome,
                    request.IntendedSavings,
                    request.Necessities,
                    request.Optional,
                    request.Culture,
                    request.Unexpected),
                cancellationToken);
            return this.Ok(MonthlyBudgetPlanController.Map(plan));
        }
        catch (ArgumentException exception)
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [exception.ParamName ?? string.Empty] = [exception.Message],
                }));
        }
    }

    /// <summary>Reviews a month using its saved intention and actual ledger activity.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The actual-versus-plan review.</returns>
    [HttpGet("budget-review")]
    [ProducesResponseType<MonthlyBudgetReviewResponse>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<MonthlyBudgetReviewResponse>> GetReview(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        try
        {
            var review = await service.ReviewAsync(year, month, cancellationToken);
            return this.Ok(new MonthlyBudgetReviewResponse(
                review.ExpectedIncome,
                review.IntendedSavings,
                review.ActualIncome,
                review.IncomeVariance,
                review.UnallocatedRemainder,
                review.LastRevisedAt,
                review.Classifications
                    .Select(classification => new MonthlyClassificationReviewResponse(
                        classification.Classification.ToString(),
                        classification.PlannedAmount,
                        classification.ActualAmount,
                        classification.Variance))
                    .ToArray(),
                review.CashflowDifference));
        }
        catch (ArgumentOutOfRangeException)
        {
            return this.NotFound();
        }
    }

    private static MonthlyBudgetPlanResponse Map(HouseholdLedger.Domain.Planning.MonthlyBudgetPlan plan) => new(
        plan.Month.Year,
        plan.Month.Month,
        plan.ExpectedIncome,
        plan.IntendedSavings,
        plan.Necessities,
        plan.Optional,
        plan.Culture,
        plan.Unexpected,
        plan.LastRevisedAt);
}
