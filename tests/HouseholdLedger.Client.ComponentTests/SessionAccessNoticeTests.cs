// <copyright file="SessionAccessNoticeTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Net;
using Bunit;
using HouseholdLedger.Client;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Components;
using HouseholdLedger.Client.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>Verifies access failures offer explicit sign-in without losing pending input.</summary>
public sealed class SessionAccessNoticeTests
{
    /// <summary>Verifies access failures update the notice and use full-document sign-in.</summary>
    /// <param name="status">The rejected access status.</param>
    /// <param name="message">The expected recovery message.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Your session has ended")]
    [InlineData(HttpStatusCode.Forbidden, "does not have access")]
    public async Task RejectedAccessShowsSignIn(HttpStatusCode status, string message)
    {
        using var context = new BunitContext();
        var access = new SessionAccessState();
        context.Services.AddSingleton(access);
        var notice = context.Render<SessionAccessNotice>();
        Assert.Empty(notice.FindAll("[role='alert']"));

        await notice.InvokeAsync(() => access.ObserveResponse(status));

        Assert.Contains(message, notice.Find("[role='alert']").TextContent, StringComparison.Ordinal);
        Assert.Contains("not automatically resubmitted", notice.Markup, StringComparison.Ordinal);
        Assert.Equal("/auth/login", notice.Find("a").GetAttribute("href"));
        Assert.Equal("_top", notice.Find("a").GetAttribute("target"));
    }

    /// <summary>Verifies transport and server failures are not misreported as logout.</summary>
    [Fact]
    public void OtherStatusesDoNotShowSessionFailure()
    {
        var access = new SessionAccessState();
        access.ObserveResponse(HttpStatusCode.InternalServerError);
        access.ObserveResponse(HttpStatusCode.OK);
        Assert.Null(access.FailureStatus);
    }

    /// <summary>Verifies an anonymous startup displays recovery instead of a broken workspace.</summary>
    [Fact]
    public void AnonymousStartupShowsRecoveryWithoutLoadingProtectedPages()
    {
        using var context = new BunitContext();
        var access = new SessionAccessState();
        context.Services.AddSingleton(access);
        using var transport = new AnonymousHandler();
        using var handler = new SessionRequestHandler(transport, access);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://ledger.example") };
        using var settings = new GlobalSettingsState(new GlobalSettingsApiClient(client));
        context.Services.AddSingleton(settings);
        context.JSInterop.SetupVoid("householdLedger.setTheme", _ => true);

        var app = context.Render<App>();

        app.WaitForAssertion(() =>
        {
            Assert.Contains("Your session has ended", app.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.Empty(app.FindAll(".boot-status"));
            Assert.Empty(app.FindAll("#workspace-main"));
            Assert.Equal(1, transport.CallCount);
        });
    }

    /// <summary>Verifies concurrent rejection notifications do not pester or hide the recovery prompt.</summary>
    [Fact]
    public void RepeatedFailuresNotifyOnceAndSuccessfulResponsesDoNotHideRecovery()
    {
        var access = new SessionAccessState();
        var changes = 0;
        access.Changed += () => changes++;
        access.ObserveResponse(HttpStatusCode.Unauthorized);
        access.ObserveResponse(HttpStatusCode.Unauthorized);
        access.ObserveResponse(HttpStatusCode.OK);
        Assert.Equal(1, changes);
        Assert.Equal(HttpStatusCode.Unauthorized, access.FailureStatus);
    }

    private sealed class AnonymousHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }
    }
}
