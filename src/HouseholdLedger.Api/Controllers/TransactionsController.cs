// <copyright file="TransactionsController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Transactions;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Creates, lists, corrects, and removes selected-day expense transactions.
/// </summary>
[ApiController]
[Route("api/v1/days/{ledgerDate}/transactions")]
public sealed class TransactionsController(ExpenseTransactionService service) : ControllerBase
{
    /// <summary>Lists transactions for one ledger date.</summary>
    /// <param name="ledgerDate">The route-selected ledger date.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The date's transactions in creation order.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ExpenseTransactionResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<ExpenseTransactionResponse>>> List(
        DateOnly ledgerDate,
        CancellationToken cancellationToken)
    {
        var transactions = await service.ListAsync(ledgerDate, cancellationToken);
        return this.Ok(transactions.Select(Map).ToArray());
    }

    /// <summary>Creates one transaction for the route-selected ledger date.</summary>
    /// <param name="ledgerDate">The route-selected ledger date.</param>
    /// <param name="request">The transaction values.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created transaction.</returns>
    [HttpPost]
    [ProducesResponseType<ExpenseTransactionResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<ExpenseTransactionResponse>> Create(
        DateOnly ledgerDate,
        CreateExpenseTransactionRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ExpenseClassification>(request.Classification, true, out var classification)
            || !Enum.IsDefined(classification))
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Classification)] = ["Choose a supported classification."],
                }));
        }

        try
        {
            var created = await service.CreateAsync(
                ledgerDate,
                request.AccountId,
                request.Amount,
                classification,
                cancellationToken);
            var response = Map(created);
            return this.Created($"api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions/{created.Id}", response);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Amount)] = [exception.Message],
                }));
        }
        catch (ArgumentException exception)
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.AccountId)] = [exception.Message],
                }));
        }
    }

    /// <summary>Corrects one transaction under its route-selected ledger date.</summary>
    /// <param name="ledgerDate">The route-selected ledger date.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="request">The replacement transaction values.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The corrected transaction.</returns>
    [HttpPut("{transactionId:guid}")]
    [ProducesResponseType<ExpenseTransactionResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ExpenseTransactionResponse>> Revise(
        DateOnly ledgerDate,
        Guid transactionId,
        UpdateExpenseTransactionRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ExpenseClassification>(request.Classification, true, out var classification)
            || !Enum.IsDefined(classification))
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Classification)] = ["Choose a supported classification."],
                }));
        }

        try
        {
            var revised = await service.ReviseAsync(
                ledgerDate,
                transactionId,
                request.AccountId,
                request.Amount,
                classification,
                cancellationToken);
            return revised is null ? this.NotFound(CreateNotFoundProblem()) : this.Ok(Map(revised));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Amount)] = [exception.Message],
                }));
        }
        catch (ArgumentException exception)
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.AccountId)] = [exception.Message],
                }));
        }
    }

    /// <summary>Removes one transaction under its route-selected ledger date.</summary>
    /// <param name="ledgerDate">The route-selected ledger date.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when removed.</returns>
    [HttpDelete("{transactionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Remove(
        DateOnly ledgerDate,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var removed = await service.RemoveAsync(ledgerDate, transactionId, cancellationToken);
        return removed ? this.NoContent() : this.NotFound(CreateNotFoundProblem());
    }

    private static ProblemDetails CreateNotFoundProblem() => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Transaction not found",
        Detail = "The transaction does not exist under the selected ledger date.",
    };

    private static ExpenseTransactionResponse Map(ExpenseTransactionDto transaction) => new(
        transaction.Id,
        transaction.AccountId,
        transaction.AccountName,
        transaction.Date,
        transaction.Amount,
        transaction.Classification.ToString());
}
