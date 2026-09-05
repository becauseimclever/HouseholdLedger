// <copyright file="WorkspaceShellTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Client.Layout;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
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
            () => Assert.Single(component.FindAll("section#workspace-main")),
            () => Assert.Single(component.FindAll("aside#workspace-inspector")),
            () => Assert.Equal("Navigation", component.Find("#navigation-heading").TextContent),
            () => Assert.Equal("Inspector", component.Find("#inspector-heading").TextContent),
            () => Assert.Equal("No calendar item selected", component.Find(".inspector-empty-state").TextContent),
            () => Assert.Equal(2, component.FindAll("#workspace-navigation a").Count),
            () => Assert.Equal("Home", component.FindAll("#workspace-navigation a")[0].TextContent),
            () => Assert.Equal("/", component.FindAll("#workspace-navigation a")[0].GetAttribute("href")),
            () => Assert.Equal("Accounts", component.FindAll("#workspace-navigation a")[1].TextContent),
            () => Assert.Equal("/accounts", component.FindAll("#workspace-navigation a")[1].GetAttribute("href")),
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

    /// <summary>Verifies exactly one primary destination is current on owned routes.</summary>
    /// <param name="route">The current route.</param>
    /// <param name="expectedLabel">The expected current navigation label.</param>
    [Theory]
    [InlineData("/", "Home")]
    [InlineData("/accounts", "Accounts")]
    [InlineData("/accounts/10000000-0000-0000-0000-000000000001", "Accounts")]
    public void NavigationExposesExactlyOneCurrentDestination(string route, string expectedLabel)
    {
        using var context = new BunitContext();
        context.Services.AddScoped<SelectedDateState>();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo(route);

        var component = context.Render<MainLayout>();
        var current = Assert.Single(component.FindAll(".workspace-navigation-link[aria-current='page']"));

        Assert.Equal(expectedLabel, current.TextContent);
    }

    /// <summary>Verifies that only pages with inspector content expose the shared pane and its control.</summary>
    [Fact]
    public void InspectorIsAvailableOnlyOnPagesThatNeedIt()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<SelectedDateState>();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var component = context.Render<MainLayout>();

        Assert.Multiple(
            () => Assert.Single(component.FindAll("button[aria-controls='workspace-inspector']")),
            () => Assert.False(component.Find("#workspace-inspector").HasAttribute("hidden")));

        navigation.NavigateTo("/accounts");

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Empty(component.FindAll("button[aria-controls='workspace-inspector']")),
            () => Assert.True(component.Find("#workspace-inspector").HasAttribute("hidden"))));

        navigation.NavigateTo("/");

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Single(component.FindAll("button[aria-controls='workspace-inspector']")),
            () => Assert.False(component.Find("#workspace-inspector").HasAttribute("hidden"))));
    }
}
