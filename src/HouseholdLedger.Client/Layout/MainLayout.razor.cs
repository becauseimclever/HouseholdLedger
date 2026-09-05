// <copyright file="MainLayout.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Layout;

using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Provides the application-wide navigation and content frame.
/// </summary>
public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private bool inspectorExpanded = true;
    private bool navigationExpanded = true;

    private bool InspectorAvailable => string.IsNullOrEmpty(this.Navigation.ToBaseRelativePath(this.Navigation.Uri).Split('?', '#')[0].Trim('/'));

    private string InspectorToggleLabel => this.inspectorExpanded ? "Collapse inspector" : "Expand inspector";

    private string NavigationToggleLabel => this.navigationExpanded ? "Collapse navigation" : "Expand navigation";

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    /// <summary>Gets or sets the selected-date state.</summary>
    [Inject]
    private SelectedDateState SelectedDate { get; set; } = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Navigation.LocationChanged -= this.HandleLocationChanged;
        this.SelectedDate.Changed -= this.OpenInspector;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        this.Navigation.LocationChanged += this.HandleLocationChanged;
        this.SelectedDate.Changed += this.OpenInspector;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        _ = this.InvokeAsync(this.StateHasChanged);
    }

    private void OpenInspector(DateOnly ledgerDate)
    {
        _ = ledgerDate;
        this.inspectorExpanded = true;
        _ = this.InvokeAsync(this.StateHasChanged);
    }

    private void ToggleInspector()
    {
        this.inspectorExpanded = !this.inspectorExpanded;
    }

    private void ToggleNavigation()
    {
        this.navigationExpanded = !this.navigationExpanded;
    }
}
