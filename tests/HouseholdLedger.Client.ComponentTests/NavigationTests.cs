// <copyright file="NavigationTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Client.Pages;
using Xunit;

/// <summary>
/// Verifies the accessible not-found navigation surface.
/// </summary>
public sealed class NavigationTests
{
    /// <summary>
    /// Verifies that an unknown route offers a clear path back to the calendar.
    /// </summary>
    [Fact]
    public void NotFoundPageHasHeadingAndCalendarNavigation()
    {
        using var context = new BunitContext();

        var component = context.Render<NotFoundPage>();

        Assert.Multiple(
            () => Assert.Equal("not-found-heading", component.Find("main").GetAttribute("aria-labelledby")),
            () => Assert.Equal("Page not found", component.Find("h1").TextContent),
            () => Assert.Equal(string.Empty, component.Find("a").GetAttribute("href")),
            () => Assert.Contains("Return to calendar", component.Find("a").TextContent, StringComparison.Ordinal));
    }
}
