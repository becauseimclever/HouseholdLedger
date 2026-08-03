// <copyright file="HealthStatus.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Components;

using System.Text.Json;

using HouseholdLedger.Client.Api;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Presents the API availability check without blocking the calendar shell.
/// </summary>
public partial class HealthStatus : ComponentBase
{
    /// <summary>
    /// Identifies the health message displayed by the component.
    /// </summary>
    protected enum HealthViewState
    {
        /// <summary>The request is in progress.</summary>
        Loading,

        /// <summary>The API reports availability.</summary>
        Available,

        /// <summary>The API returned a non-success or unknown status.</summary>
        Unavailable,

        /// <summary>The request failed before a valid response was read.</summary>
        Error,
    }

    /// <summary>
    /// Gets or sets the client-owned health adapter.
    /// </summary>
    [Inject]
    protected IHealthApiClient HealthApiClient { get; set; } = null!;

    /// <summary>
    /// Gets the current availability presentation state.
    /// </summary>
    protected HealthViewState ViewState { get; private set; } = HealthViewState.Loading;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await this.CheckAvailabilityAsync();
    }

    /// <summary>
    /// Retries the availability request after a recoverable failure.
    /// </summary>
    /// <returns>A task representing the asynchronous interaction.</returns>
    protected async Task RetryAsync()
    {
        await this.CheckAvailabilityAsync();
    }

    private async Task CheckAvailabilityAsync()
    {
        this.ViewState = HealthViewState.Loading;

        try
        {
            this.ViewState = await this.HealthApiClient.IsAvailableAsync()
                ? HealthViewState.Available
                : HealthViewState.Unavailable;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
        {
            this.ViewState = HealthViewState.Error;
        }
    }
}
