// <copyright file="AuthenticationController.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Controllers;

using HouseholdLedger.Api.Hosting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Manages the hosted browser session without exposing provider tokens.</summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("auth")]
public sealed class AuthenticationController(AuthenticationMode mode, IAntiforgery antiforgery) : ControllerBase
{
    /// <summary>Starts an external sign-in using a local return address.</summary>
    /// <param name="returnUrl">The local page to return to.</param>
    /// <returns>The authentication challenge.</returns>
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string returnUrl = "/")
    {
        if (!this.Url.IsLocalUrl(returnUrl))
        {
            return this.BadRequest();
        }

        return mode.Enabled
            ? this.Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme)
            : this.LocalRedirect(returnUrl);
    }

    /// <summary>Returns session mode and a request verification token for the authenticated owner.</summary>
    /// <returns>The hosted session details.</returns>
    [HttpGet("session")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Session()
    {
        var requestToken = mode.Enabled ? antiforgery.GetAndStoreTokens(this.HttpContext).RequestToken : null;
        return this.Ok(new { AuthenticationEnabled = mode.Enabled, RequestToken = requestToken });
    }

    /// <summary>Ends the local session. The identity-provider session is managed separately.</summary>
    /// <returns>A redirect to the signed-out page.</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return this.LocalRedirect("/auth/signed-out");
    }

    /// <summary>Provides an explicit signed-out page without immediately signing in again.</summary>
    /// <returns>The signed-out page.</returns>
    [AllowAnonymous]
    [HttpGet("signed-out")]
    public ContentResult SignedOut() => this.Content(
        "<!doctype html><html lang=\"en\"><meta name=\"viewport\" content=\"width=device-width\"><title>Signed out</title><main><h1>Signed out</h1><p>Your HouseholdLedger session has ended. Your identity-provider session may still be active.</p><a href=\"/auth/login\">Sign in</a></main></html>",
        "text/html");

    /// <summary>Reports external sign-in failure without disclosing protocol data.</summary>
    /// <returns>A safe problem response.</returns>
    [AllowAnonymous]
    [HttpGet("error")]
    public IActionResult Error() => this.Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Sign-in could not be completed.",
        detail: "Check the configured identity provider and household owner, then return to /auth/login.");
}
