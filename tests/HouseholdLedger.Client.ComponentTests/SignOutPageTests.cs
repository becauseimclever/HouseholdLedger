// <copyright file="SignOutPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Net;
using System.Net.Http.Json;
using Bunit;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>Verifies the sign-out confirmation and session recovery UI.</summary>
public sealed class SignOutPageTests
{
    /// <summary>Verifies the native sign-out form contains a framework verification token.</summary>
    [Fact]
    public void OwnerReceivesVerifiedSignOutForm()
    {
        using var context = CreateContext(HttpStatusCode.OK, new HostedSession(true, "verification"));
        var page = context.Render<SignOutPage>();
        Assert.Equal("post", page.Find("form").GetAttribute("method"));
        Assert.Equal("/auth/logout", page.Find("form").GetAttribute("action"));
        Assert.Equal("verification", page.Find("input[name='__RequestVerificationToken']").GetAttribute("value"));
    }

    /// <summary>Verifies unavailable sessions provide recovery without a false sign-out form.</summary>
    /// <param name="status">The session HTTP status.</param>
    /// <param name="enabled">Whether authentication is enabled.</param>
    /// <param name="token">The token returned by the host.</param>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, true, null)]
    [InlineData(HttpStatusCode.OK, true, null)]
    public void InvalidSessionOffersSignIn(HttpStatusCode status, bool enabled, string? token)
    {
        using var context = CreateContext(status, new HostedSession(enabled, token));
        var page = context.Render<SignOutPage>();
        Assert.NotEmpty(page.Find("[role='alert']").TextContent);
        Assert.Equal("/auth/login", page.Find("a").GetAttribute("href"));
        Assert.Equal("_top", page.Find("a").GetAttribute("target"));
        Assert.Empty(page.FindAll("form"));
    }

    /// <summary>Verifies local development is not presented as an authenticated session.</summary>
    [Fact]
    public void DevelopmentModeHasNoSignOutForm()
    {
        using var context = CreateContext(HttpStatusCode.OK, new HostedSession(false, null));
        var page = context.Render<SignOutPage>();
        Assert.Contains("does not use sign-in", page.Markup, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("form"));
    }

    private static BunitContext CreateContext(HttpStatusCode status, HostedSession session)
    {
        var context = new BunitContext();
        context.Services.AddSingleton(new HttpClient(new SessionHandler(status, session))
        {
            BaseAddress = new Uri("https://ledger.example"),
        });
        return context;
    }

    private sealed class SessionHandler(HttpStatusCode status, HostedSession session) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(session) });
    }
}
