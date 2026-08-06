// <copyright file="WorkspaceShellTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Client.Layout;
using NUnit.Framework;

/// <summary>
/// Verifies the accessible workspace shell around the calendar surface.
/// </summary>
public sealed class WorkspaceShellTests
{
    /// <summary>
    /// Verifies that the expanded shell presents independently controlled, neutral panes.
    /// </summary>
    [Test]
    public void ExpandedShellHasAccessibleNeutralPanesAndIndependentNativeToggles()
    {
        using var context = new BunitContext();

        var component = context.Render<MainLayout>();
        var navigationToggle = component.Find("button[aria-controls='workspace-navigation']");
        var inspectorToggle = component.Find("button[aria-controls='workspace-inspector']");

        Assert.Multiple(() =>
        {
            Assert.That(component.FindAll("nav#workspace-navigation"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("section#calendar-workspace"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("aside#workspace-inspector"), Has.Count.EqualTo(1));
            Assert.That(component.Find("#navigation-heading").TextContent, Is.EqualTo("Navigation"));
            Assert.That(component.Find("#inspector-heading").TextContent, Is.EqualTo("Inspector"));
            Assert.That(component.Find(".inspector-empty-state").TextContent, Is.EqualTo("No calendar item selected"));
            Assert.That(component.FindAll("#workspace-navigation a, #workspace-navigation button"), Is.Empty);
            Assert.That(navigationToggle.GetAttribute("type"), Is.EqualTo("button"));
            Assert.That(navigationToggle.GetAttribute("aria-expanded"), Is.EqualTo("true"));
            Assert.That(navigationToggle.GetAttribute("aria-label"), Is.EqualTo("Collapse navigation"));
            Assert.That(inspectorToggle.GetAttribute("type"), Is.EqualTo("button"));
            Assert.That(inspectorToggle.GetAttribute("aria-expanded"), Is.EqualTo("true"));
            Assert.That(inspectorToggle.GetAttribute("aria-label"), Is.EqualTo("Collapse inspector"));
            Assert.That(component.FindAll("button .pane-toggle-tooltip[role='tooltip']"), Has.Count.EqualTo(2));
            Assert.That(component.FindAll("button > span[aria-hidden='true']"), Has.Count.EqualTo(2));
        });

        navigationToggle.Click();

        Assert.Multiple(() =>
        {
            Assert.That(component.Find("#workspace-navigation").HasAttribute("hidden"), Is.True);
            Assert.That(navigationToggle.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            Assert.That(navigationToggle.GetAttribute("aria-label"), Is.EqualTo("Expand navigation"));
            Assert.That(component.Find("#workspace-inspector").HasAttribute("hidden"), Is.False);
            Assert.That(inspectorToggle.GetAttribute("aria-expanded"), Is.EqualTo("true"));
        });

        inspectorToggle.Click();

        Assert.Multiple(() =>
        {
            Assert.That(component.Find("#workspace-navigation").HasAttribute("hidden"), Is.True);
            Assert.That(component.Find("#workspace-inspector").HasAttribute("hidden"), Is.True);
            Assert.That(inspectorToggle.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            Assert.That(inspectorToggle.GetAttribute("aria-label"), Is.EqualTo("Expand inspector"));
        });
    }
}
