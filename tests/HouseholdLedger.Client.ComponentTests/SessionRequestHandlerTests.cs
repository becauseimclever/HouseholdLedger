// <copyright file="SessionRequestHandlerTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.State;
using Xunit;

/// <summary>Verifies browser writes use the host's session verification token.</summary>
public sealed class SessionRequestHandlerTests
{
    private static readonly string[] ExpectedPaths = ["/api/v1/accounts", "/auth/session", "/api/v1/accounts"];

    /// <summary>Verifies reads are untouched and writes acquire a fresh verification token.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task WritesFetchTokenWithoutRetryingTheWrite()
    {
        using var transport = new RecordingHandler();
        using var handler = new SessionRequestHandler(transport, new SessionAccessState());
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://ledger.example") };
        using var read = await client.GetAsync("/api/v1/accounts", TestContext.Current.CancellationToken);
        using var write = await client.PostAsync("/api/v1/accounts", null, TestContext.Current.CancellationToken);
        Assert.Equal(ExpectedPaths, transport.Paths);
        Assert.Equal("verification", transport.WriteToken);
    }

    /// <summary>Verifies session failures never submit or replay the pending financial write.</summary>
    /// <param name="status">The token response status.</param>
    /// <param name="token">The returned request token.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, null)]
    [InlineData(HttpStatusCode.OK, null)]
    public async Task MissingSessionStopsWrite(HttpStatusCode status, string? token)
    {
        using var transport = new FailedSessionHandler(status, token);
        using var handler = new SessionRequestHandler(transport, new SessionAccessState());
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://ledger.example") };
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.PostAsync("/api/v1/accounts", null, TestContext.Current.CancellationToken));
        Assert.Equal(1, transport.CallCount);
    }

    /// <summary>Verifies invalid JSON and incomplete session modes stop financial writes.</summary>
    /// <param name="body">The session response.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData("{}")]
    [InlineData("not json")]
    [InlineData("null")]
    public async Task InvalidSessionStopsWrite(string body)
    {
        using var transport = new InvalidSessionHandler(body);
        using var handler = new SessionRequestHandler(transport, new SessionAccessState());
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://ledger.example") };
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.PostAsync("/api/v1/accounts", null, TestContext.Current.CancellationToken));
        Assert.Equal(1, transport.CallCount);
    }

    /// <summary>Verifies lost access is surfaced for reads, token fetches, and mutations without retries.</summary>
    /// <param name="write">Whether the operation mutates data.</param>
    /// <param name="rejectToken">Whether token acquisition fails before dispatch.</param>
    /// <param name="status">The authorization failure.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false, false, HttpStatusCode.Unauthorized)]
    [InlineData(false, false, HttpStatusCode.Forbidden)]
    [InlineData(true, true, HttpStatusCode.Unauthorized)]
    [InlineData(true, true, HttpStatusCode.Forbidden)]
    [InlineData(true, false, HttpStatusCode.Unauthorized)]
    [InlineData(true, false, HttpStatusCode.Forbidden)]
    public async Task LostAccessOffersRecoveryWithoutRetry(bool write, bool rejectToken, HttpStatusCode status)
    {
        var access = new SessionAccessState();
        using var transport = new RejectedRequestHandler(status, rejectToken);
        using var handler = new SessionRequestHandler(transport, access);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://ledger.example") };
        if (write && rejectToken)
        {
            await Assert.ThrowsAsync<HttpRequestException>(
                () => client.PostAsync("/api/v1/accounts", null, TestContext.Current.CancellationToken));
        }
        else
        {
            using var response = write
                ? await client.PostAsync("/api/v1/accounts", null, TestContext.Current.CancellationToken)
                : await client.GetAsync("/api/v1/accounts", TestContext.Current.CancellationToken);
            Assert.Equal(status, response.StatusCode);
        }

        Assert.Equal(status, access.FailureStatus);
        Assert.Equal(write && !rejectToken ? 2 : 1, transport.CallCount);
    }

    private sealed class RejectedRequestHandler(HttpStatusCode status, bool rejectToken) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.CallCount++;
            return Task.FromResult(request.RequestUri!.AbsolutePath == "/auth/session" && !rejectToken
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new HostedSession(true, "verification")) }
                : new HttpResponseMessage(status));
        }
    }

    private sealed class InvalidSessionHandler(string body) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    private sealed class FailedSessionHandler(HttpStatusCode status, string? token) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.CallCount++;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = JsonContent.Create(new HostedSession(true, token)),
            });
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];

        public string? WriteToken { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Paths.Add(request.RequestUri!.AbsolutePath);
            if (request.RequestUri.AbsolutePath == "/auth/session")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { AuthenticationEnabled = true, RequestToken = "verification" }),
                });
            }

            if (request.Method == HttpMethod.Post)
            {
                this.WriteToken = request.Headers.GetValues("X-CSRF-TOKEN").Single();
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
