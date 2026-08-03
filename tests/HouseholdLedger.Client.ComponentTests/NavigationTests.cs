// <copyright file="NavigationTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Client.Pages;
using NUnit.Framework;

/// <summary>
/// Verifies the accessible not-found navigation surface.
/// </summary>
public sealed class NavigationTests
{
    /// <summary>
    /// Verifies that an unknown route offers a clear path back to the calendar.
    /// </summary>
    [Test]
    public void NotFoundPageHasHeadingAndCalendarNavigation()
    {
        using var context = new BunitContext();

        var component = context.Render<NotFoundPage>();

        Assert.Multiple(() =>
        {
            Assert.That(component.Find("main").GetAttribute("aria-labelledby"), Is.EqualTo("not-found-heading"));
            Assert.That(component.Find("h1").TextContent, Is.EqualTo("Page not found"));
            Assert.That(component.Find("a").GetAttribute("href"), Is.EqualTo(string.Empty));
            Assert.That(component.Find("a").TextContent, Does.Contain("Return to calendar"));
        });
    }
}
