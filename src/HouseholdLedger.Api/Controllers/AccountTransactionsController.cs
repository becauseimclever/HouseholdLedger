// <copyright file="AccountTransactionsController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Transactions;
using Microsoft.AspNetCore.Mvc;

/// <summary>Lists the complete transaction history for one account.</summary>
[ApiController]
[Route("api/v1/accounts/{accountId:guid}/transactions")]
public sealed class AccountTransactionsController(ExpenseTransactionService service) : ControllerBase
{
    /// <summary>Gets one account and its matching transactions newest first.</summary>
    /// <param name="accountId">The route-selected account identifier.</param>
    /// <param name="request">The optional account-history filters.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The selected account and its matching history.</returns>
    [HttpGet]
    [ProducesResponseType<AccountTransactionHistoryResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<AccountTransactionHistoryResponse>> Get(
        Guid accountId,
        [FromQuery] AccountTransactionFilterRequest request,
        CancellationToken cancellationToken)
    {
        ExpenseClassification? classification = null;
        if (!string.IsNullOrWhiteSpace(request.Classification))
        {
            if (!Enum.TryParse<ExpenseClassification>(request.Classification, true, out var parsedClassification)
                || !Enum.IsDefined(parsedClassification))
            {
                return this.FilterValidationProblem(
                    nameof(request.Classification),
                    "Choose a supported classification.");
            }

            classification = parsedClassification;
        }

        AccountTransactionCriteria criteria;
        try
        {
            criteria = AccountTransactionCriteria.Create(
                request.From,
                request.To,
                classification,
                request.MinimumAmount,
                request.MaximumAmount,
                request.Search);
        }
        catch (ArgumentException exception)
        {
            return this.FilterValidationProblem(exception.ParamName ?? "filters", exception.Message);
        }

        var history = await service.GetAccountHistoryAsync(accountId, criteria, cancellationToken);
        if (history is null)
        {
            return this.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Account not found",
                Detail = "The selected account does not exist.",
            });
        }

        return this.Ok(new AccountTransactionHistoryResponse(
            new AccountResponse(history.AccountId, history.AccountName),
            history.Transactions.Select(Map).ToArray()));
    }

    private static ExpenseTransactionResponse Map(ExpenseTransactionDto transaction) => new(
        transaction.Id,
        transaction.AccountId,
        transaction.AccountName,
        transaction.Date,
        transaction.Amount,
        transaction.Classification.ToString());

    private ActionResult<AccountTransactionHistoryResponse> FilterValidationProblem(string field, string message)
    {
        return this.ValidationProblem(new ValidationProblemDetails(
            new Dictionary<string, string[]>
            {
                [field] = [message],
            }));
    }
}
