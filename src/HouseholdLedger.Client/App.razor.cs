// <copyright file="App.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client;

using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Loads and applies authoritative settings before exposing the workspace.</summary>
public partial class App : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource lifetimeCancellation = new();

    /// <summary>Gets or sets the global settings state.</summary>
    [Inject]
    private GlobalSettingsState GlobalSettings { get; set; } = null!;

    /// <summary>Gets or sets the JavaScript runtime.</summary>
    [Inject]
    private IJSRuntime JavaScript { get; set; } = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.GlobalSettings.Changed -= this.ApplyTheme;
        this.lifetimeCancellation.Cancel();
        this.lifetimeCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        this.GlobalSettings.Changed += this.ApplyTheme;
        await this.GlobalSettings.EnsureLoadedAsync(this.lifetimeCancellation.Token);
        await this.ApplyThemeAsync();
    }

    private void ApplyTheme()
    {
        _ = this.InvokeAsync(this.ApplyThemeAsync);
    }

    private async Task ApplyThemeAsync()
    {
        await this.JavaScript.InvokeVoidAsync(
            "householdLedger.setTheme",
            this.lifetimeCancellation.Token,
            this.GlobalSettings.CurrentTheme);
        await this.InvokeAsync(this.StateHasChanged);
    }
}
