// <copyright file="IncomeReceiptsController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Income;
using Microsoft.AspNetCore.Mvc;

/// <summary>Records income receipts explicitly confirmed by the user.</summary>
[ApiController]
[Route("api/v1/income-receipts")]
public sealed class IncomeReceiptsController(IncomeReceiptService service) : ControllerBase
{
    /// <summary>Confirms that income was received and records its actual destinations.</summary>
    /// <param name="request">The details the user confirmed were received.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly recorded receipt.</returns>
    [HttpPost]
    [ProducesResponseType<IncomeReceiptResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<IncomeReceiptResponse>> Create(
        ConfirmIncomeReceiptRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var receipt = await service.CreateAsync(
                new IncomeReceiptCommand(
                    request.ReceivedDate,
                    request.Amount,
                    request.Allocations
                        .Select(allocation => allocation is null
                            ? throw new ArgumentException("Account allocations cannot contain null.", nameof(request))
                            : new IncomeAllocationCommand(allocation.AccountId, allocation.Amount))
                        .ToArray(),
                    request.RequestId),
                cancellationToken);
            var response = IncomeReceiptsController.Map(receipt);
            return this.CreatedAtAction(nameof(this.Get), new { receiptId = response.Id }, response);
        }
        catch (IncomeReceiptRequestConflictException)
        {
            return this.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Confirmation request conflict",
                Detail = "Use a new request identifier for different receipt details.",
            });
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

    /// <summary>Corrects a confirmed receipt's actual date, amount and destinations.</summary>
    /// <param name="receiptId">The receipt identifier.</param>
    /// <param name="request">The corrected values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The corrected receipt, or a validation or not-found problem.</returns>
    [HttpPut("{receiptId:guid}")]
    [ProducesResponseType<IncomeReceiptResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IncomeReceiptResponse>> Update(
        Guid receiptId,
        UpdateIncomeReceiptRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var receipt = await service.UpdateAsync(
                receiptId,
                new IncomeReceiptCommand(
                    request.ReceivedDate,
                    request.Amount,
                    request.Allocations.Select(item => item is null
                        ? throw new ArgumentException("Account allocations cannot contain null.", nameof(request))
                        : new IncomeAllocationCommand(item.AccountId, item.Amount)).ToArray()),
                cancellationToken);
            return receipt is null ? this.NotFound(NotFoundProblem()) : this.Ok(Map(receipt));
        }
        catch (ArgumentException exception)
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [exception.ParamName ?? string.Empty] = [exception.Message] }));
        }
    }

    /// <summary>Deletes a mistaken confirmed receipt and its account allocations.</summary>
    /// <param name="receiptId">The receipt identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content, or a not-found problem.</returns>
    [HttpDelete("{receiptId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(Guid receiptId, CancellationToken cancellationToken) =>
        await service.DeleteAsync(receiptId, cancellationToken)
            ? this.NoContent()
            : this.NotFound(NotFoundProblem());

    /// <summary>Gets a confirmed income receipt.</summary>
    /// <param name="receiptId">The receipt identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The confirmed receipt when it exists.</returns>
    [HttpGet("{receiptId:guid}")]
    [ProducesResponseType<IncomeReceiptResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IncomeReceiptResponse>> Get(Guid receiptId, CancellationToken cancellationToken)
    {
        var receipt = await service.GetAsync(receiptId, cancellationToken);
        return receipt is null
            ? this.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Income receipt not found",
                Detail = "The requested income receipt does not exist.",
            })
            : this.Ok(IncomeReceiptsController.Map(receipt));
    }

    private static IncomeReceiptResponse Map(IncomeReceiptDto receipt) => new(
        receipt.Id,
        receipt.ScheduleId,
        receipt.PayDate,
        receipt.NetIncome,
        receipt.Allocations.Select(allocation => new IncomeAllocationResponse(allocation.AccountId, allocation.Amount)).ToArray());

    private static ProblemDetails NotFoundProblem() => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Income receipt not found",
        Detail = "The requested income receipt does not exist.",
    };
}
