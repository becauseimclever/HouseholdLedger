// <copyright file="MainLayout.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Layout;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Provides the application-wide navigation and content frame.
/// </summary>
public partial class MainLayout : LayoutComponentBase
{
    private bool inspectorExpanded = true;
    private bool navigationExpanded = true;

    private string InspectorToggleLabel => this.inspectorExpanded ? "Collapse inspector" : "Expand inspector";

    private string NavigationToggleLabel => this.navigationExpanded ? "Collapse navigation" : "Expand navigation";

    private void ToggleInspector()
    {
        this.inspectorExpanded = !this.inspectorExpanded;
    }

    private void ToggleNavigation()
    {
        this.navigationExpanded = !this.navigationExpanded;
    }
}
