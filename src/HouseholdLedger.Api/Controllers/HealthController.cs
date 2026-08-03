// <copyright file="HealthController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Reports API service availability.
/// </summary>
[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Gets the current API availability status.
    /// </summary>
    /// <returns>The API availability status.</returns>
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError,
        "application/problem+json")]
    public ActionResult<HealthResponse> Get()
    {
        return this.Ok(new HealthResponse("available"));
    }
}
