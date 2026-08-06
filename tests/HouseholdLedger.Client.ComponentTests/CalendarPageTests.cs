// <copyright file="CalendarPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Client.Pages;
using NUnit.Framework;

/// <summary>
/// Verifies the calendar workspace surface.
/// </summary>
public sealed class CalendarPageTests
{
    /// <summary>
    /// Verifies that the root content is the neutral calendar workspace surface.
    /// </summary>
    [Test]
    public void CalendarWorkspaceHasOneMainHeadingAndNoInteractiveContent()
    {
        using var context = new BunitContext();

        var component = context.Render<CalendarPage>();

        Assert.Multiple(() =>
        {
            Assert.That(component.FindAll("main.calendar-page"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("main.calendar-page h1"), Has.Count.EqualTo(1));
            Assert.That(component.Find("main.calendar-page h1").TextContent, Is.EqualTo("Calendar"));
            Assert.That(component.FindAll("main.calendar-page button"), Is.Empty);
            Assert.That(component.FindAll("main.calendar-page input"), Is.Empty);
            Assert.That(component.FindAll("main.calendar-page select"), Is.Empty);
            Assert.That(component.FindAll("main.calendar-page textarea"), Is.Empty);
            Assert.That(component.FindAll("main.calendar-page a"), Is.Empty);
        });
    }
}
