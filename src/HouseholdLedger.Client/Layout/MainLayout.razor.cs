// <copyright file="MainLayout.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Layout;

using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Provides the application-wide navigation and content frame.
/// </summary>
public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private bool inspectorExpanded = true;
    private bool navigationExpanded = true;

    private string InspectorToggleLabel => this.inspectorExpanded ? "Collapse inspector" : "Expand inspector";

    private string NavigationToggleLabel => this.navigationExpanded ? "Collapse navigation" : "Expand navigation";

    /// <summary>Gets or sets the selected-date state.</summary>
    [Inject]
    private SelectedDateState SelectedDate { get; set; } = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.SelectedDate.Changed -= this.OpenInspector;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        this.SelectedDate.Changed += this.OpenInspector;
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
