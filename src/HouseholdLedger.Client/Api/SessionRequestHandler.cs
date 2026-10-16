// <copyright file="SessionRequestHandler.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Net.Http.Json;
using System.Text.Json;
using HouseholdLedger.Client.State;

/// <summary>Adds framework session verification to hosted browser writes without retrying mutations.</summary>
/// <param name="innerHandler">The HTTP transport.</param>
/// <param name="access">The shared session-access notification state.</param>
public sealed class SessionRequestHandler(HttpMessageHandler innerHandler, SessionAccessState access) : DelegatingHandler(innerHandler)
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head && request.Method != HttpMethod.Options)
        {
            using var sessionRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(request.RequestUri!, "/auth/session"));
            using var sessionResponse = await base.SendAsync(sessionRequest, cancellationToken);
            access.ObserveResponse(sessionResponse.StatusCode);
            sessionResponse.EnsureSuccessStatusCode();
            HostedSession session;
            try
            {
                session = await sessionResponse.Content.ReadFromJsonAsync<HostedSession>(cancellationToken)
                    ?? throw new HttpRequestException("The hosted session response was empty.");
            }
            catch (JsonException exception)
            {
                throw new HttpRequestException("The hosted session response was invalid.", exception);
            }

            if (session.AuthenticationEnabled)
            {
                if (string.IsNullOrWhiteSpace(session.RequestToken))
                {
                    throw new HttpRequestException("The hosted session verification token was missing.");
                }

                request.Headers.Add("X-CSRF-TOKEN", session.RequestToken);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);
        access.ObserveResponse(response.StatusCode);
        return response;
    }
}
