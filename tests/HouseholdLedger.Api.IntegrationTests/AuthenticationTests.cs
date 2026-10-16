// <copyright file="AuthenticationTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>Verifies the owner-only hosted authentication boundary.</summary>
public sealed class AuthenticationTests
{
    /// <summary>Verifies that protected API requests never redirect to HTML login pages.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task AnonymousApiRequestIsUnauthorizedButHealthAndContractArePublic()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        using var accounts = await client.GetAsync("/api/v1/accounts", TestContext.Current.CancellationToken);
        using var health = await client.GetAsync("/api/v1/health", TestContext.Current.CancellationToken);
        using var contract = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        using var shell = await client.GetAsync("/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, accounts.StatusCode);
        Assert.Null(accounts.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, contract.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, shell.StatusCode);
        Assert.StartsWith("https://localhost/auth/login", shell.Headers.Location?.AbsoluteUri, StringComparison.Ordinal);
    }

    /// <summary>Verifies authorization uses both issuer and subject, not a display claim.</summary>
    /// <param name="identity">The test identity.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData("other")]
    [InlineData("wrong-issuer")]
    public async Task AuthenticatedNonOwnerIsForbidden(string identity)
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Add("Test-Identity", identity);
        using var response = await client.GetAsync("/auth/session", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies authenticated writes require a framework antiforgery token.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task OwnerGetsSessionAndWritesRequireAntiforgery()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Add("Test-Identity", "owner");
        var session = await client.GetFromJsonAsync<Session>(
            "/auth/session", TestContext.Current.CancellationToken);
        Assert.NotNull(session);
        Assert.True(session.AuthenticationEnabled);
        Assert.NotEmpty(session.RequestToken!);

        using var rejected = await client.PostAsync("/auth/logout", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        request.Headers.Add("X-CSRF-TOKEN", session.RequestToken);
        using var accepted = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, accepted.StatusCode);
        Assert.Equal("/auth/signed-out", accepted.Headers.Location?.OriginalString);
    }

    /// <summary>Verifies missing authentication cannot expose a non-development deployment.</summary>
    [Fact]
    public void ProductionWithoutIdentityConfigurationFailsClosed()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Authentication:Authority", string.Empty);
        });
        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }

    /// <summary>Verifies readiness is not the process liveness signal.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task MissingDatabaseIsNotReady()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Authentication:Authority", "https://identity.example/application/o/ledger/");
            builder.UseSetting("Authentication:ClientId", "ledger");
            builder.UseSetting("Authentication:ClientSecret", "test-only-not-a-real-secret");
            builder.UseSetting("Authentication:OwnerSubject", "owner");
            builder.UseSetting("ConnectionStrings:HouseholdLedger", string.Empty);
            builder.ConfigureServices(services =>
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestIdentityHandler>("Test", _ => { }));
        });

    private sealed record Session(bool AuthenticationEnabled, string? RequestToken);

    private sealed class TestIdentityHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = this.Request.Headers["Test-Identity"].ToString();
            if (string.IsNullOrEmpty(identity))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var subject = identity == "other" ? "other" : "owner";
            var issuer = identity == "wrong-issuer" ? "https://other.example/" : "https://identity.example/application/o/ledger/";
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", subject, ClaimValueTypes.String, issuer)], this.Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, this.Scheme.Name)));
        }
    }
}
