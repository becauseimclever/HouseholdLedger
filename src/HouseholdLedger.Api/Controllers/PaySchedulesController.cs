// <copyright file="PaySchedulesController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Income;
using HouseholdLedger.Domain.Income;
using Microsoft.AspNetCore.Mvc;

/// <summary>Reads, creates, revises, pauses, resumes, and materializes recurring income pay schedules.</summary>
[ApiController]
[Route("api/v1/pay-schedules")]
public sealed class PaySchedulesController(IncomeScheduleService service) : ControllerBase
{
    /// <summary>Lists recurring income pay schedules.</summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The pay schedules.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PayScheduleResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<PayScheduleResponse>>> List(CancellationToken cancellationToken)
    {
        var schedules = await service.ListAsync(cancellationToken);
        return this.Ok(schedules.Select(Map).ToArray());
    }

    /// <summary>Gets one recurring income pay schedule.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The matching pay schedule.</returns>
    [HttpGet("{scheduleId:guid}")]
    [ProducesResponseType<PayScheduleResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PayScheduleResponse>> Get(Guid scheduleId, CancellationToken cancellationToken)
    {
        var schedule = await service.GetAsync(scheduleId, cancellationToken);
        return schedule is null ? this.NotFound(CreateScheduleNotFoundProblem()) : this.Ok(Map(schedule));
    }

    /// <summary>Lists materialized income receipts in an inclusive calendar range.</summary>
    /// <param name="from">The inclusive first pay date.</param>
    /// <param name="to">The inclusive final pay date.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The receipt snapshots in the requested range.</returns>
    [HttpGet("receipts")]
    [ProducesResponseType<IReadOnlyList<IncomeReceiptResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<IncomeReceiptResponse>>> ListReceipts(
        [FromQuery(Name = "from")] DateOnly? from,
        [FromQuery(Name = "to")] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (from is null || to is null)
        {
            return this.ReceiptRangeValidationProblem(from is null ? "from" : "to", "Choose an inclusive calendar date.");
        }

        try
        {
            var receipts = await service.ListReceiptsAsync(from.Value, to.Value, cancellationToken);
            return this.Ok(receipts.Select(Map).ToArray());
        }
        catch (ArgumentException exception)
        {
            return this.ReceiptRangeValidationProblem("to", exception.Message);
        }
    }

    /// <summary>Creates one recurring income pay schedule.</summary>
    /// <param name="request">The schedule values.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created schedule.</returns>
    [HttpPost]
    [ProducesResponseType<PayScheduleResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PayScheduleResponse>> Create(
        CreatePayScheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Cadence))
        {
            return this.CadenceValidationProblem<PayScheduleResponse>();
        }

        try
        {
            var created = await service.CreateAsync(Map(request), cancellationToken);
            var response = Map(created);
            return this.Created($"api/v1/pay-schedules/{response.Id}", response);
        }
        catch (ArgumentException exception)
        {
            return this.ValidationProblemFor<PayScheduleResponse>(exception);
        }
    }

    /// <summary>Revises the future settings for one recurring income pay schedule.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="request">The replacement future schedule values.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The revised schedule.</returns>
    [HttpPut("{scheduleId:guid}")]
    [ProducesResponseType<PayScheduleResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PayScheduleResponse>> Revise(
        Guid scheduleId,
        RevisePayScheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Cadence))
        {
            return this.CadenceValidationProblem<PayScheduleResponse>();
        }

        try
        {
            var revised = await service.ReviseAsync(scheduleId, Map(request), cancellationToken);
            return revised is null ? this.NotFound(CreateScheduleNotFoundProblem()) : this.Ok(Map(revised));
        }
        catch (ArgumentException exception)
        {
            return this.ValidationProblemFor<PayScheduleResponse>(exception);
        }
    }

    /// <summary>Pauses one recurring income pay schedule.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when the schedule was paused.</returns>
    [HttpPost("{scheduleId:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Pause(Guid scheduleId, CancellationToken cancellationToken)
    {
        var paused = await service.PauseAsync(scheduleId, cancellationToken);
        return paused ? this.NoContent() : this.NotFound(CreateScheduleNotFoundProblem());
    }

    /// <summary>Resumes one recurring income pay schedule without backfilling missed receipts.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="request">The date from which future receipts are eligible.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when the schedule was resumed.</returns>
    [HttpPost("{scheduleId:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Resume(
        Guid scheduleId,
        ResumePayScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var resumed = await service.ResumeAsync(scheduleId, request.ResumeDate, cancellationToken);
        return resumed ? this.NoContent() : this.NotFound(CreateScheduleNotFoundProblem());
    }

    /// <summary>Materializes active pay schedules that are due on a selected date.</summary>
    /// <param name="payDate">The date for which due receipts are materialized.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The newly materialized receipts.</returns>
    [HttpPost("materialize/{payDate:datetime}")]
    [ProducesResponseType<IReadOnlyList<IncomeReceiptResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<IncomeReceiptResponse>>> Materialize(
        DateOnly payDate,
        CancellationToken cancellationToken)
    {
        var receipts = await service.MaterializeDueReceiptsAsync(payDate, cancellationToken);
        return this.Ok(receipts.Select(Map).ToArray());
    }

    private static IncomeScheduleCommand Map(CreatePayScheduleRequest request) => new(
        request.Name,
        request.FirstPayDate,
        (Domain.Income.PayPeriodCadence)request.Cadence,
        request.NetIncome,
        request.Allocations.Select(allocation => new IncomeAllocationCommand(allocation.AccountId, allocation.Amount)).ToArray(),
        request.SecondMonthlyPayDay);

    private static IncomeScheduleCommand Map(RevisePayScheduleRequest request) => new(
        request.Name,
        request.FirstPayDate,
        (Domain.Income.PayPeriodCadence)request.Cadence,
        request.NetIncome,
        request.Allocations.Select(allocation => new IncomeAllocationCommand(allocation.AccountId, allocation.Amount)).ToArray(),
        request.SecondMonthlyPayDay);

    private static PayScheduleResponse Map(IncomeScheduleDto schedule) => new(
        schedule.Id,
        schedule.Name,
        schedule.FirstPayDate,
        (Api.Contracts.PayPeriodCadence)schedule.Cadence,
        schedule.NetIncome,
        schedule.Allocations.Select(allocation => new IncomeAllocationResponse(allocation.AccountId, allocation.Amount)).ToArray(),
        schedule.SecondMonthlyPayDay,
        schedule.IsPaused);

    private static IncomeReceiptResponse Map(IncomeReceipt receipt) => new(
        receipt.Id,
        receipt.ScheduleId,
        receipt.PayDate,
        receipt.NetIncome,
        receipt.Allocations.Select(allocation => new IncomeAllocationResponse(allocation.AccountId, allocation.Amount)).ToArray());

    private static IncomeReceiptResponse Map(IncomeReceiptDto receipt) => new(
        receipt.Id,
        receipt.ScheduleId,
        receipt.PayDate,
        receipt.NetIncome,
        receipt.Allocations.Select(allocation => new IncomeAllocationResponse(allocation.AccountId, allocation.Amount)).ToArray());

    private static ProblemDetails CreateScheduleNotFoundProblem() => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Pay schedule not found",
        Detail = "The requested pay schedule does not exist.",
    };

    private ActionResult<T> CadenceValidationProblem<T>() => this.ValidationProblem(new ValidationProblemDetails(
        new Dictionary<string, string[]>
        {
            [nameof(CreatePayScheduleRequest.Cadence)] = ["Choose a supported pay cadence."],
        }));

    private ActionResult<IReadOnlyList<IncomeReceiptResponse>> ReceiptRangeValidationProblem(string field, string message) =>
        this.ValidationProblem(new ValidationProblemDetails(
            new Dictionary<string, string[]>
            {
                [field] = [message],
            }));

    private ActionResult<T> ValidationProblemFor<T>(ArgumentException exception)
    {
        var field = exception.ParamName switch
        {
            "name" => nameof(CreatePayScheduleRequest.Name),
            "cadence" => nameof(CreatePayScheduleRequest.Cadence),
            "netIncome" => nameof(CreatePayScheduleRequest.NetIncome),
            "sourceAllocations" or "commands" => nameof(CreatePayScheduleRequest.Allocations),
            "secondMonthlyPayDay" => nameof(CreatePayScheduleRequest.SecondMonthlyPayDay),
            _ => string.Empty,
        };
        return this.ValidationProblem(new ValidationProblemDetails(
            new Dictionary<string, string[]>
            {
                [field] = [exception.Message],
            }));
    }
}
