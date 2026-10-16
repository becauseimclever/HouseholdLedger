// <copyright file="SessionAccessState.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.State;

using System.Net;

/// <summary>Reports lost server access without navigating away from pending work.</summary>
public sealed class SessionAccessState
{
    /// <summary>Occurs when the access failure changes.</summary>
    public event Action? Changed;

    /// <summary>Gets the last authorization failure, retained until a new page session.</summary>
    public HttpStatusCode? FailureStatus { get; private set; }

    /// <summary>Observes authorization failures while leaving other HTTP errors to their callers.</summary>
    /// <param name="status">The server response status.</param>
    public void ObserveResponse(HttpStatusCode status)
    {
        if (status is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            || this.FailureStatus == status)
        {
            return;
        }

        this.FailureStatus = status;
        this.Changed?.Invoke();
    }
}
