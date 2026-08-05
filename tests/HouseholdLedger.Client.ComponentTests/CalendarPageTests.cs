// <copyright file="CalendarPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Client.Pages;
using NUnit.Framework;

/// <summary>
/// Verifies the temporary sample shell.
/// </summary>
public sealed class CalendarPageTests
{
    /// <summary>
    /// Verifies that the root content presents one accessible, neutral sample heading.
    /// </summary>
    [Test]
    public void TemporarySampleShellHasOneMainHeadingAndNeutralContent()
    {
        using var context = new BunitContext();

        var component = context.Render<CalendarPage>();

        Assert.Multiple(() =>
        {
            Assert.That(component.FindAll("main"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("main h1"), Has.Count.EqualTo(1));
            Assert.That(component.Find("main h1").TextContent, Is.EqualTo("Temporary sample content"));
            Assert.That(component.Find("main p").TextContent, Is.EqualTo("This is a temporary sample."));
            Assert.That(component.Find("main").TextContent, Does.Not.Contain("calendar").IgnoreCase);
            Assert.That(component.Find("main").TextContent, Does.Not.Contain("ledger").IgnoreCase);
            Assert.That(component.Find("main").TextContent, Does.Not.Contain("budget").IgnoreCase);
        });
    }
}
