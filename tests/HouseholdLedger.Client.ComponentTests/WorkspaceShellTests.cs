// <copyright file="WorkspaceShellTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Client.Layout;
using HouseholdLedger.Client.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Verifies the accessible workspace shell around the calendar surface.
/// </summary>
public sealed class WorkspaceShellTests
{
    /// <summary>
    /// Verifies that the expanded shell presents independently controlled, neutral panes.
    /// </summary>
    [Fact]
    public void ExpandedShellHasAccessibleNeutralPanesAndIndependentNativeToggles()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<SelectedDateState>();

        var component = context.Render<MainLayout>();
        var navigationToggle = component.Find("button[aria-controls='workspace-navigation']");
        var inspectorToggle = component.Find("button[aria-controls='workspace-inspector']");

        Assert.Multiple(
            () => Assert.Single(component.FindAll("nav#workspace-navigation")),
            () => Assert.Single(component.FindAll("section#calendar-workspace")),
            () => Assert.Single(component.FindAll("aside#workspace-inspector")),
            () => Assert.Equal("Navigation", component.Find("#navigation-heading").TextContent),
            () => Assert.Equal("Inspector", component.Find("#inspector-heading").TextContent),
            () => Assert.Equal("No calendar item selected", component.Find(".inspector-empty-state").TextContent),
            () => Assert.Empty(component.FindAll("#workspace-navigation a, #workspace-navigation button")),
            () => Assert.Equal("button", navigationToggle.GetAttribute("type")),
            () => Assert.Equal("true", navigationToggle.GetAttribute("aria-expanded")),
            () => Assert.Equal("Collapse navigation", navigationToggle.GetAttribute("aria-label")),
            () => Assert.Equal("button", inspectorToggle.GetAttribute("type")),
            () => Assert.Equal("true", inspectorToggle.GetAttribute("aria-expanded")),
            () => Assert.Equal("Collapse inspector", inspectorToggle.GetAttribute("aria-label")),
            () => Assert.Equal(2, component.FindAll("button .pane-toggle-tooltip[role='tooltip']").Count),
            () => Assert.Equal(2, component.FindAll("button > span[aria-hidden='true']").Count));

        navigationToggle.Click();

        Assert.Multiple(
            () => Assert.True(component.Find("#workspace-navigation").HasAttribute("hidden")),
            () => Assert.Equal("false", navigationToggle.GetAttribute("aria-expanded")),
            () => Assert.Equal("Expand navigation", navigationToggle.GetAttribute("aria-label")),
            () => Assert.False(component.Find("#workspace-inspector").HasAttribute("hidden")),
            () => Assert.Equal("true", inspectorToggle.GetAttribute("aria-expanded")));

        inspectorToggle.Click();

        Assert.Multiple(
            () => Assert.True(component.Find("#workspace-navigation").HasAttribute("hidden")),
            () => Assert.True(component.Find("#workspace-inspector").HasAttribute("hidden")),
            () => Assert.Equal("false", inspectorToggle.GetAttribute("aria-expanded")),
            () => Assert.Equal("Expand inspector", inspectorToggle.GetAttribute("aria-label")));
    }
}
