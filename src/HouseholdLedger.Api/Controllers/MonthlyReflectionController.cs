// <copyright file="MonthlyReflectionController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Planning;
using HouseholdLedger.Domain.Planning;
using Microsoft.AspNetCore.Mvc;

/// <summary>Reads and saves optional monthly reflections independently of plans.</summary>
[ApiController]
[Route("api/v1/months/{year:int:min(1):max(9999)}/{month:int:min(1):max(12)}/reflection")]
public sealed class MonthlyReflectionController(MonthlyReflectionService service) : ControllerBase
{
    /// <summary>Gets a month's saved reflection.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reflection, or a not-found problem when none has been saved.</returns>
    [HttpGet]
    [ProducesResponseType<MonthlyReflectionResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<MonthlyReflectionResponse>> Get(int year, int month, CancellationToken cancellationToken)
    {
        var reflection = await service.GetAsync(year, month, cancellationToken);
        return reflection is null ? this.NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Monthly reflection not found",
            Detail = "No reflection has been saved for this month.",
        }) : this.Ok(Map(reflection));
    }

    /// <summary>Creates or replaces a month's optional reflection.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="request">The optional reflection text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved reflection, or a validation problem.</returns>
    [HttpPut]
    [ProducesResponseType<MonthlyReflectionResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<MonthlyReflectionResponse>> Save(
        int year,
        int month,
        MonthlyReflectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var reflection = await service.SaveAsync(year, month, request.WhatWorked, request.NextMonthIntention, cancellationToken);
            return this.Ok(Map(reflection));
        }
        catch (ArgumentException exception)
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [exception.ParamName ?? string.Empty] = [exception.Message] }));
        }
    }

    private static MonthlyReflectionResponse Map(MonthlyReflection reflection) =>
        new(reflection.WhatWorked, reflection.NextMonthIntention, reflection.LastRevisedAt);
}
