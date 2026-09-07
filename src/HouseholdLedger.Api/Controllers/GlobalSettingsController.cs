// <copyright file="GlobalSettingsController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Settings;
using HouseholdLedger.Domain.Settings;
using Microsoft.AspNetCore.Mvc;

/// <summary>Reads and updates application-wide settings.</summary>
[ApiController]
[Route("api/v1/settings")]
public sealed class GlobalSettingsController(GlobalSettingsService service) : ControllerBase
{
    /// <summary>Gets the effective global settings.</summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The persisted settings or defaults.</returns>
    [HttpGet]
    [ProducesResponseType<GlobalSettingsResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<GlobalSettingsResponse>> Get(CancellationToken cancellationToken)
    {
        var settings = await service.GetAsync(cancellationToken);
        return this.Ok(Map(settings));
    }

    /// <summary>Persists the global display currency.</summary>
    /// <param name="request">The requested settings.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authoritative saved settings.</returns>
    [HttpPut]
    [ProducesResponseType<GlobalSettingsResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<GlobalSettingsResponse>> Update(
        UpdateGlobalSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseCurrencyCode(request.DisplayCurrency, out var displayCurrency))
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.DisplayCurrency)] = ["Choose a supported display currency."],
                }));
        }

        var settings = await service.UpdateCurrencyAsync(displayCurrency, cancellationToken);
        return this.Ok(Map(settings));
    }

    /// <summary>Persists the global workbench theme without changing currency.</summary>
    /// <param name="request">The requested theme.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authoritative saved settings.</returns>
    [HttpPut("theme")]
    [ProducesResponseType<GlobalSettingsResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<GlobalSettingsResponse>> UpdateTheme(
        UpdateThemeRequest request,
        CancellationToken cancellationToken)
    {
        if (!WorkbenchThemeCode.TryParse(request.Theme, out var theme))
        {
            return this.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Theme)] = ["Choose a supported workbench theme."],
                }));
        }

        var settings = await service.UpdateThemeAsync(theme, cancellationToken);
        return this.Ok(Map(settings));
    }

    private static GlobalSettingsResponse Map(GlobalSettingsDto settings) =>
        new(settings.DisplayCurrency.ToString(), WorkbenchThemeCode.ToCode(settings.Theme));

    private static bool TryParseCurrencyCode(string? value, out DisplayCurrency displayCurrency)
    {
        DisplayCurrency? parsedCurrency = value?.ToUpperInvariant() switch
        {
            "USD" => DisplayCurrency.USD,
            "CAD" => DisplayCurrency.CAD,
            "EUR" => DisplayCurrency.EUR,
            "GBP" => DisplayCurrency.GBP,
            "AUD" => DisplayCurrency.AUD,
            "XXX" => DisplayCurrency.XXX,
            _ => (DisplayCurrency?)null,
        };

        displayCurrency = parsedCurrency.GetValueOrDefault();
        return parsedCurrency.HasValue;
    }
}
