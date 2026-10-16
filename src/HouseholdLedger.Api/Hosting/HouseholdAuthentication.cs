// <copyright file="HouseholdAuthentication.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Hosting;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

/// <summary>Configures the hosted application's owner-only identity boundary.</summary>
internal static class HouseholdAuthentication
{
    private static readonly Action<ILogger, Exception?> SignInFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(1, nameof(SignInFailed)),
        "External sign-in failed. Check identity-provider configuration and owner assignment.");

    internal static bool AddHouseholdAuthentication(this WebApplicationBuilder builder)
    {
        var authority = builder.Configuration["Authentication:Authority"];
        var enabled = !string.IsNullOrWhiteSpace(authority);
        if (!enabled && !builder.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("Configure Authentication:Authority outside Development.");
        }

        var authentication = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-HouseholdLedger";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/auth/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/auth/session"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    }
                    else
                    {
                        context.Response.Redirect(context.RedirectUri);
                    }

                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        if (enabled)
        {
            if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri)
                || authorityUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Authentication:Authority must be an absolute HTTPS issuer URI.");
            }

            var clientId = Required(builder.Configuration, "Authentication:ClientId");
            var clientSecret = Required(builder.Configuration, "Authentication:ClientSecret");
            var ownerSubject = Required(builder.Configuration, "Authentication:OwnerSubject");
            authentication.AddOpenIdConnect(options =>
            {
                options.Authority = authority;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.MapInboundClaims = false;
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Events.OnTokenValidated = context =>
                {
                    if (context.Principal?.FindFirst("sub")?.Value != ownerSubject
                        || context.SecurityToken.Issuer != authority)
                    {
                        context.Fail("This identity is not the configured household owner.");
                    }

                    return Task.CompletedTask;
                };
                options.Events.OnRemoteFailure = context =>
                {
                    SignInFailed(
                        context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("HouseholdAuthentication"),
                        null);
                    context.HandleResponse();
                    context.Response.Redirect("/auth/error");
                    return Task.CompletedTask;
                };
            });

            builder.Services.AddAuthorization(options =>
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        context.User.FindFirst("sub")?.Value == ownerSubject
                        && context.User.FindFirst("sub")?.Issuer == authority)
                    .Build());
        }
        else
        {
            builder.Services.AddAuthorization();
        }

        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-HouseholdLedger-Antiforgery";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        builder.Services.AddSingleton(new AuthenticationMode(enabled));
        return enabled;
    }

    private static string Required(ConfigurationManager configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key])
            ? configuration[key]!
            : throw new InvalidOperationException($"Configure {key} when authentication is enabled.");
}
