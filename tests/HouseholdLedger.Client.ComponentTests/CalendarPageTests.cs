// <copyright file="CalendarPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

/// <summary>
/// Verifies the calendar-centered scaffold page.
/// </summary>
public sealed class CalendarPageTests
{
    /// <summary>
    /// Verifies that the current period is represented honestly and semantically.
    /// </summary>
    [Test]
    public void CalendarShowsCurrentPeriodAndHonestEmptyState()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero)));
        context.Services.AddSingleton<IHealthApiClient>(new AvailableHealthApiClient());

        var component = context.Render<CalendarPage>();

        Assert.Multiple(() =>
        {
            Assert.That(component.FindAll("main"), Has.Count.EqualTo(1));
            Assert.That(component.Find("h1").TextContent, Is.EqualTo("Calendar"));
            Assert.That(component.Find("[data-period-heading]").TextContent, Is.EqualTo("August 2026"));
            Assert.That(component.Find("table").GetAttribute("aria-labelledby"), Is.EqualTo("calendar-period"));
            Assert.That(component.FindAll("th[scope='col']"), Has.Count.EqualTo(7));
            Assert.That(component.FindAll("td"), Has.Count.EqualTo(42));
            Assert.That(component.FindAll("time[aria-current='date']"), Has.Count.EqualTo(1));
            Assert.That(
                component.Find("time[aria-current='date']").GetAttribute("datetime"),
                Is.EqualTo("2026-08-02"));
            Assert.That(component.Find("[data-empty-state]").TextContent, Does.Contain("No ledger entries for August 2026"));
            Assert.That(component.Markup, Does.Not.Contain("total").IgnoreCase);
            Assert.That(component.Markup, Does.Not.Contain("savings").IgnoreCase);
            Assert.That(component.Markup, Does.Not.Contain("reflection").IgnoreCase);
        });
    }

    /// <summary>
    /// Verifies that the existing period controls move between months and return to today.
    /// </summary>
    [Test]
    public void CalendarPeriodNavigationUpdatesCalendarSemantics()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero)));
        context.Services.AddSingleton<IHealthApiClient>(new AvailableHealthApiClient());
        var component = context.Render<CalendarPage>();

        component.Find("button[aria-label='Show previous month']").Click();

        Assert.Multiple(() =>
        {
            Assert.That(component.Find("[data-period-heading]").TextContent, Is.EqualTo("July 2026"));
            Assert.That(component.Find("caption").TextContent, Is.EqualTo("Calendar for July 2026"));
            Assert.That(
                component.Find("time[aria-current='date']").GetAttribute("datetime"),
                Is.EqualTo("2026-08-02"));
            Assert.That(
                component.Find("time[aria-current='date']").ParentElement!.ClassList,
                Does.Contain("outside-period"));
        });

        component.Find("button[aria-label='Show next month']").Click();
        component.Find("button[aria-label='Show next month']").Click();

        Assert.Multiple(() =>
        {
            Assert.That(component.Find("[data-period-heading]").TextContent, Is.EqualTo("September 2026"));
            Assert.That(component.Find("[data-empty-state]").TextContent, Does.Contain("No ledger entries for September 2026"));
        });

        component.Find("button.today-button").Click();

        Assert.Multiple(() =>
        {
            Assert.That(component.Find("[data-period-heading]").TextContent, Is.EqualTo("August 2026"));
            Assert.That(component.FindAll("time[aria-current='date']"), Has.Count.EqualTo(1));
        });
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class AvailableHealthApiClient : IHealthApiClient
    {
        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
