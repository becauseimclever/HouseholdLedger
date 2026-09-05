// <copyright file="AccountsController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Accounts;
using Microsoft.AspNetCore.Mvc;

/// <summary>Creates and lists named accounts.</summary>
[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController(AccountService service) : ControllerBase
{
    /// <summary>Lists the authoritative account catalog.</summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The deterministically ordered accounts.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> List(CancellationToken cancellationToken)
    {
        var accounts = await service.ListAsync(cancellationToken);
        return this.Ok(accounts.Select(Map).ToArray());
    }

    /// <summary>Creates one named account.</summary>
    /// <param name="request">The account values.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created account.</returns>
    [HttpPost]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<AccountResponse>> Create(
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CreateAsync(request.Name, cancellationToken);
            if (!result.IsCreated)
            {
                return this.NameValidationProblem("An account with this name already exists.");
            }

            var response = Map(result.Account!);
            return this.Created($"api/v1/accounts/{response.Id}", response);
        }
        catch (ArgumentException exception)
        {
            return this.NameValidationProblem(exception.Message);
        }
    }

    private static AccountResponse Map(AccountDto account) => new(account.Id, account.Name);

    private ActionResult<AccountResponse> NameValidationProblem(string message)
    {
        return this.ValidationProblem(new ValidationProblemDetails(
            new Dictionary<string, string[]>
            {
                [nameof(CreateAccountRequest.Name)] = [message],
            }));
    }
}
