// <copyright file="CalendarPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Globalization;

using Bunit;
using HouseholdLedger.Client.Pages;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

/// <summary>
/// Verifies the calendar workspace surface.
/// </summary>
public sealed class CalendarPageTests
{
    private static readonly string[] MondayFirstGermanHeaders = ["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"];

    /// <summary>
    /// Verifies that the injected client-local date initializes the selected Today presentation.
    /// </summary>
    [Test]
    public void InitialRenderUsesInjectedLocalDateAndExposesOneActivePresentationMode()
    {
        using var context = new BunitContext();
        var expectedDate = new DateOnly(2024, 2, 29);
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(expectedDate));

        var component = context.Render<CalendarPage>();
        var modes = component.FindAll("input[name='calendar-mode']");
        var selectedDate = component.Find(".calendar-today .calendar-day");

        Assert.Multiple(() =>
        {
            Assert.That(component.FindAll("main.calendar-page"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("main.calendar-page h1"), Has.Count.EqualTo(1));
            Assert.That(component.Find("main.calendar-page h1").TextContent, Is.EqualTo("Calendar"));
            Assert.That(modes, Has.Count.EqualTo(3));
            Assert.That(modes.Count(mode => mode.HasAttribute("checked")), Is.EqualTo(1));
            Assert.That(component.Find("input[name='calendar-mode'][checked]").ParentElement!.TextContent.Trim(), Is.EqualTo("Today"));
            Assert.That(component.FindAll("table.calendar-grid"), Is.Empty);
            Assert.That(selectedDate.GetAttribute("aria-label"), Is.EqualTo(expectedDate.ToString("D", CultureInfo.CurrentCulture)));
            Assert.That(selectedDate.GetAttribute("aria-pressed"), Is.EqualTo("true"));
            Assert.That(selectedDate.GetAttribute("aria-current"), Is.EqualTo("date"));
            Assert.That(selectedDate.GetAttribute("tabindex"), Is.EqualTo("0"));
        });
    }

    /// <summary>
    /// Verifies that native mode controls are exclusive and preserve the active date.
    /// </summary>
    [Test]
    public void ModeControlsAreExclusiveAndPresentTheContainingDayWeekOrMonth()
    {
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 2, 29);
        var firstDayOffset = ((int)activeDate.DayOfWeek - (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek + 7) % 7;
        var expectedWeekStart = activeDate.AddDays(-firstDayOffset);
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(activeDate));
        var component = context.Render<CalendarPage>();

        component.FindAll("input[name='calendar-mode']")[1].Change();

        Assert.Multiple(() =>
        {
            Assert.That(component.FindAll("input[name='calendar-mode'][checked]"), Has.Count.EqualTo(1));
            Assert.That(component.Find("input[name='calendar-mode'][checked]").ParentElement!.TextContent.Trim(), Is.EqualTo("This Week"));
            Assert.That(component.Find("table.calendar-grid").GetAttribute("aria-label"), Does.Contain(activeDate.ToString("D", CultureInfo.CurrentCulture)));
            Assert.That(component.FindAll("table.calendar-grid thead th[scope='col']"), Has.Count.EqualTo(7));
            Assert.That(component.FindAll("table.calendar-grid .calendar-day"), Has.Count.EqualTo(7));
            Assert.That(component.FindAll("table.calendar-grid .calendar-day")[0].GetAttribute("aria-label"), Is.EqualTo(expectedWeekStart.ToString("D", CultureInfo.CurrentCulture)));
            Assert.That(component.FindAll(".calendar-day[aria-pressed='true']"), Has.Count.EqualTo(1));
        });

        component.FindAll("input[name='calendar-mode']")[2].Change();

        Assert.Multiple(() =>
        {
            Assert.That(component.FindAll("input[name='calendar-mode'][checked]"), Has.Count.EqualTo(1));
            Assert.That(component.Find("input[name='calendar-mode'][checked]").ParentElement!.TextContent.Trim(), Is.EqualTo("This Month"));
            Assert.That(component.Find("table.calendar-grid").GetAttribute("aria-label"), Is.EqualTo("Month of February 2024"));
            Assert.That(component.FindAll("table.calendar-grid thead th[scope='col']"), Has.Count.EqualTo(7));
            Assert.That(component.FindAll("table.calendar-grid .calendar-day"), Has.Count.EqualTo(29));
            Assert.That(component.FindAll("td.calendar-empty-cell[aria-hidden='true']"), Is.Not.Empty);
            Assert.That(component.Find(".calendar-day[aria-pressed='true']").GetAttribute("aria-label"), Is.EqualTo(activeDate.ToString("D", CultureInfo.CurrentCulture)));
        });
    }

    /// <summary>
    /// Verifies that direct and native keyboard activation select a single focused month date.
    /// </summary>
    [Test]
    public void MonthDatesSupportDirectEnterSpaceAndDirectionalSelection()
    {
        using var context = new BunitContext();
        var component = RenderCalendar(context, new DateOnly(2024, 2, 29));
        SelectMode(component, 2);

        var focusRequestCount = GetFocusRequestCount(context);
        FindDateButton(component, new DateOnly(2024, 2, 15)).Click();
        AssertActiveDate(component, new DateOnly(2024, 2, 15));
        AssertFocusWasRequested(context, focusRequestCount);

        focusRequestCount = GetFocusRequestCount(context);
        FindDateButton(component, new DateOnly(2024, 2, 16)).KeyDown(new KeyboardEventArgs { Key = "Enter" });
        AssertActiveDate(component, new DateOnly(2024, 2, 16));
        AssertFocusWasRequested(context, focusRequestCount);

        focusRequestCount = GetFocusRequestCount(context);
        FindDateButton(component, new DateOnly(2024, 2, 17)).KeyDown(new KeyboardEventArgs { Key = " " });
        AssertActiveDate(component, new DateOnly(2024, 2, 17));
        AssertFocusWasRequested(context, focusRequestCount);

        focusRequestCount = GetFocusRequestCount(context);
        FindDateButton(component, new DateOnly(2024, 2, 17)).KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        AssertActiveDate(component, new DateOnly(2024, 2, 18));
        AssertFocusWasRequested(context, focusRequestCount);

        focusRequestCount = GetFocusRequestCount(context);
        FindDateButton(component, new DateOnly(2024, 2, 18)).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        AssertActiveDate(component, new DateOnly(2024, 2, 17));
        AssertFocusWasRequested(context, focusRequestCount);

        focusRequestCount = GetFocusRequestCount(context);
        FindDateButton(component, new DateOnly(2024, 2, 17)).KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        AssertActiveDate(component, new DateOnly(2024, 2, 10));
        AssertFocusWasRequested(context, focusRequestCount);

        focusRequestCount = GetFocusRequestCount(context);
        FindDateButton(component, new DateOnly(2024, 2, 10)).KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        AssertActiveDate(component, new DateOnly(2024, 2, 17));
        AssertFocusWasRequested(context, focusRequestCount);
    }

    /// <summary>
    /// Verifies chronological Tab and Shift+Tab selection, including automatic month changes.
    /// </summary>
    [Test]
    public void MonthTabTraversalMovesSelectionInChronologicalOrderAcrossMonthBoundary()
    {
        using var context = new BunitContext();
        var component = RenderCalendar(context, new DateOnly(2024, 1, 30));
        SelectMode(component, 2);

        FindDateButton(component, new DateOnly(2024, 1, 30)).KeyDown(new KeyboardEventArgs { Key = "Tab" });
        AssertActiveDate(component, new DateOnly(2024, 1, 31));

        FindDateButton(component, new DateOnly(2024, 1, 31)).KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        AssertActiveDate(component, new DateOnly(2024, 2, 1));
        Assert.That(component.Find("#calendar-period-heading").TextContent, Is.EqualTo("February 2024"));

        FindDateButton(component, new DateOnly(2024, 2, 1)).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        AssertActiveDate(component, new DateOnly(2024, 1, 31));

        FindDateButton(component, new DateOnly(2024, 1, 31)).KeyDown(new KeyboardEventArgs { Key = "Tab", ShiftKey = true });
        AssertActiveDate(component, new DateOnly(2024, 1, 30));
    }

    /// <summary>
    /// Verifies chronological Tab and directional movement in the culture-derived weekly grid.
    /// </summary>
    [Test]
    public void WeekGridTabAndArrowsMoveTheSelectedDateAndContainingWeek()
    {
        using var context = new BunitContext();
        var component = RenderCalendar(context, new DateOnly(2024, 2, 29));
        SelectMode(component, 1);

        FindDateButton(component, new DateOnly(2024, 2, 29)).KeyDown(new KeyboardEventArgs { Key = "Tab" });
        AssertActiveDate(component, new DateOnly(2024, 3, 1));

        FindDateButton(component, new DateOnly(2024, 3, 1)).KeyDown(new KeyboardEventArgs { Key = "Tab", ShiftKey = true });
        AssertActiveDate(component, new DateOnly(2024, 2, 29));

        FindDateButton(component, new DateOnly(2024, 2, 29)).KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        AssertActiveDate(component, new DateOnly(2024, 3, 7));
        Assert.That(component.Find("table.calendar-grid").GetAttribute("aria-label"), Does.Contain(new DateOnly(2024, 3, 7).ToString("D", CultureInfo.CurrentCulture)));

        FindDateButton(component, new DateOnly(2024, 3, 7)).KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        AssertActiveDate(component, new DateOnly(2024, 2, 29));
    }

    /// <summary>
    /// Verifies that Shift+Tab from the first weekly date leaves the grid without changing its selection or period.
    /// </summary>
    [Test]
    public void WeekGridFirstDateShiftTabLeavesToTheActiveModeWithoutChangingTheDateOrPeriod()
    {
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 2, 29);
        var component = RenderCalendar(context, activeDate);
        SelectMode(component, 1);
        var firstDate = GetWeekStart(activeDate);
        var weekLabel = component.Find("table.calendar-grid").GetAttribute("aria-label");

        FindDateButton(component, firstDate).KeyDown(new KeyboardEventArgs { Key = "Tab", ShiftKey = true });

        Assert.Multiple(() =>
        {
            AssertActiveDate(component, activeDate);
            Assert.That(component.Find("table.calendar-grid").GetAttribute("aria-label"), Is.EqualTo(weekLabel));
            Assert.That(context.JSInterop.Invocations, Has.Some.Matches<Bunit.JSRuntimeInvocation>(
                invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus"));
        });
    }

    /// <summary>
    /// Verifies that Tab from each final grid date leaves to period navigation without changing the selected date or period.
    /// </summary>
    [Test]
    public void GridLastDateTabLeavesToPreviousPeriodNavigationWithoutChangingTheDateOrPeriod()
    {
        using var context = new BunitContext();
        var weekActiveDate = new DateOnly(2024, 2, 29);
        var component = RenderCalendar(context, weekActiveDate);
        SelectMode(component, 1);
        var weekLabel = component.Find("table.calendar-grid").GetAttribute("aria-label");

        FindDateButton(component, GetWeekStart(weekActiveDate).AddDays(6)).KeyDown(new KeyboardEventArgs { Key = "Tab" });

        Assert.Multiple(() =>
        {
            AssertActiveDate(component, weekActiveDate);
            Assert.That(component.Find("table.calendar-grid").GetAttribute("aria-label"), Is.EqualTo(weekLabel));
            AssertFocusWasRequested(context);
        });

        var monthActiveDate = new DateOnly(2024, 2, 29);
        SelectMode(component, 2);
        var monthLabel = component.Find("table.calendar-grid").GetAttribute("aria-label");
        var focusRequestCount = GetFocusRequestCount(context);

        FindDateButton(component, monthActiveDate).KeyDown(new KeyboardEventArgs { Key = "Tab" });

        Assert.Multiple(() =>
        {
            AssertActiveDate(component, monthActiveDate);
            Assert.That(component.Find("table.calendar-grid").GetAttribute("aria-label"), Is.EqualTo(monthLabel));
            AssertFocusWasRequested(context, focusRequestCount);
        });
    }

    /// <summary>
    /// Verifies that Today exposes one native date control without grid keyboard movement or managed Tab behavior.
    /// </summary>
    [Test]
    public void TodayUsesNativeSequentialAndActivationBehaviorForItsSingleSelectedDate()
    {
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 2, 29);
        var component = RenderCalendar(context, activeDate);
        var todayDate = FindDateButton(component, activeDate);

        Assert.That(
            () => todayDate.KeyDown(new KeyboardEventArgs { Key = "Tab" }),
            Throws.TypeOf<Bunit.MissingEventHandlerException>());
        todayDate.Click();

        Assert.Multiple(() =>
        {
            AssertActiveDate(component, activeDate);
            Assert.That(component.FindAll("table.calendar-grid"), Is.Empty);
            Assert.That(todayDate.GetAttribute("tabindex"), Is.EqualTo("0"));
            Assert.That(context.JSInterop.Invocations, Is.Empty);
        });

        MovePeriod(component, "Next");
        Assert.Multiple(() =>
        {
            AssertActiveDate(component, activeDate.AddDays(1));
            Assert.That(component.Find("#calendar-period-heading").TextContent, Is.EqualTo(activeDate.AddDays(1).ToString("D", CultureInfo.CurrentCulture)));
            Assert.That(component.Find(".calendar-period-status").TextContent, Is.EqualTo($"Showing {activeDate.AddDays(1).ToString("D", CultureInfo.CurrentCulture)}."));
        });
    }

    /// <summary>
    /// Verifies that a Monday-first culture determines both weekday rendering and horizontal arrow direction.
    /// </summary>
    [Test]
    public void WeekGridUsesCurrentCultureFirstDayForRenderedOrderAndHorizontalArrows()
    {
        using var cultureScope = new CultureScope("de-DE");
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 3, 3);
        var component = RenderCalendar(context, activeDate);
        SelectMode(component, 1);

        var headers = component.FindAll("table.calendar-grid thead th[scope='col']").Select(header => header.TextContent).ToArray();
        Assert.That(headers, Is.EqualTo(MondayFirstGermanHeaders));
        Assert.That(FindDateButton(component, new DateOnly(2024, 2, 26)).GetAttribute("aria-label"), Is.EqualTo(new DateOnly(2024, 2, 26).ToString("D", CultureInfo.CurrentCulture)));

        FindDateButton(component, activeDate).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        AssertActiveDate(component, activeDate.AddDays(-1));

        FindDateButton(component, activeDate.AddDays(-1)).KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        AssertActiveDate(component, activeDate);
    }

    /// <summary>
    /// Verifies that cross-week keyboard movement updates status and retains focus on the selected date.
    /// </summary>
    [Test]
    public void WeekArrowBoundaryUpdatesContainingWeekStatusAndRetainsFocus()
    {
        using var context = new BunitContext();
        var activeDate = GetWeekStart(new DateOnly(2024, 2, 29));
        var component = RenderCalendar(context, activeDate);
        SelectMode(component, 1);
        var focusRequestCount = GetFocusRequestCount(context);

        FindDateButton(component, activeDate).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        var expectedDate = activeDate.AddDays(-1);
        Assert.Multiple(() =>
        {
            AssertActiveDate(component, expectedDate);
            Assert.That(component.Find("table.calendar-grid").GetAttribute("aria-label"), Does.Contain(expectedDate.ToString("D", CultureInfo.CurrentCulture)));
            Assert.That(component.Find(".calendar-period-status").TextContent, Is.EqualTo($"Showing the week containing {expectedDate.ToString("D", CultureInfo.CurrentCulture)}."));
            AssertFocusWasRequested(context, focusRequestCount);
        });
    }

    /// <summary>
    /// Verifies month keyboard and period navigation routes retain focus while changing periods across year and leap-day boundaries.
    /// </summary>
    [Test]
    public void MonthBoundaryRoutesUpdatePeriodStatusAndRetainFocus()
    {
        using var context = new BunitContext();
        var component = RenderCalendar(context, new DateOnly(2024, 1, 1));
        SelectMode(component, 2);
        var focusRequestCount = GetFocusRequestCount(context);

        FindDateButton(component, new DateOnly(2024, 1, 1)).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        AssertCrossPeriodFocusAndStatus(context, component, new DateOnly(2023, 12, 31), "December 2023", focusRequestCount);

        focusRequestCount = GetFocusRequestCount(context);
        FindDateButton(component, new DateOnly(2023, 12, 31)).KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        AssertCrossPeriodFocusAndStatus(context, component, new DateOnly(2024, 1, 1), "January 2024", focusRequestCount);

        using var leapContext = new BunitContext();
        var leapComponent = RenderCalendar(leapContext, new DateOnly(2024, 2, 29));
        SelectMode(leapComponent, 2);
        focusRequestCount = GetFocusRequestCount(leapContext);
        FindDateButton(leapComponent, new DateOnly(2024, 2, 29)).KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        AssertCrossPeriodFocusAndStatus(leapContext, leapComponent, new DateOnly(2024, 3, 1), "March 2024", focusRequestCount);

        focusRequestCount = GetFocusRequestCount(leapContext);
        MovePeriod(leapComponent, "Previous");
        AssertCrossPeriodFocusAndStatus(leapContext, leapComponent, new DateOnly(2024, 2, 1), "February 2024", focusRequestCount);
    }

    /// <summary>
    /// Verifies mode-specific navigation across year and leap-month boundaries.
    /// </summary>
    [Test]
    public void PreviousAndNextMoveTheActiveDateByModeAcrossYearAndLeapMonthBoundaries()
    {
        using var context = new BunitContext();
        var component = RenderCalendar(context, new DateOnly(2024, 12, 31));

        MovePeriod(component, "Next");
        AssertActiveDate(component, new DateOnly(2025, 1, 1));
        MovePeriod(component, "Previous");
        AssertActiveDate(component, new DateOnly(2024, 12, 31));

        SelectMode(component, 1);
        MovePeriod(component, "Next");
        AssertActiveDate(component, new DateOnly(2025, 1, 7));
        MovePeriod(component, "Previous");
        AssertActiveDate(component, new DateOnly(2024, 12, 31));

        SelectMode(component, 2);
        MovePeriod(component, "Next");
        AssertActiveDate(component, new DateOnly(2025, 1, 31));
        MovePeriod(component, "Previous");
        AssertActiveDate(component, new DateOnly(2024, 12, 31));

        using var leapContext = new BunitContext();
        var leapComponent = RenderCalendar(leapContext, new DateOnly(2024, 1, 31));
        SelectMode(leapComponent, 2);
        MovePeriod(leapComponent, "Next");
        AssertActiveDate(leapComponent, new DateOnly(2024, 2, 29));
        Assert.That(leapComponent.Find("#calendar-period-heading").TextContent, Is.EqualTo("February 2024"));
    }

    private static IRenderedComponent<CalendarPage> RenderCalendar(BunitContext context, DateOnly date)
    {
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(date));
        return context.Render<CalendarPage>();
    }

    private static void SelectMode(IRenderedComponent<CalendarPage> component, int modeIndex)
    {
        component.FindAll("input[name='calendar-mode']")[modeIndex].Change();
    }

    private static void MovePeriod(IRenderedComponent<CalendarPage> component, string label)
    {
        component.Find($"button[aria-label='{label} period']").Click();
    }

    private static AngleSharp.Dom.IElement FindDateButton(IRenderedComponent<CalendarPage> component, DateOnly date)
    {
        return component.Find($".calendar-day[aria-label='{date.ToString("D", CultureInfo.CurrentCulture)}']");
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var firstDayOfWeek = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var offset = ((int)date.DayOfWeek - firstDayOfWeek + 7) % 7;
        return date.AddDays(-offset);
    }

    private static void AssertCrossPeriodFocusAndStatus(BunitContext context, IRenderedComponent<CalendarPage> component, DateOnly expectedDate, string expectedPeriod, int focusRequestCount)
    {
        Assert.Multiple(() =>
        {
            AssertActiveDate(component, expectedDate);
            Assert.That(component.Find("#calendar-period-heading").TextContent, Is.EqualTo(expectedPeriod));
            Assert.That(component.Find(".calendar-period-status").TextContent, Is.EqualTo($"Showing {expectedPeriod}."));
            AssertFocusWasRequested(context, focusRequestCount);
        });
    }

    private static int GetFocusRequestCount(BunitContext context)
    {
        return context.JSInterop.Invocations.Count(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }

    private static void AssertFocusWasRequested(BunitContext context, int focusRequestCount = 0)
    {
        Assert.That(GetFocusRequestCount(context), Is.GreaterThan(focusRequestCount));
    }

    private static void AssertActiveDate(IRenderedComponent<CalendarPage> component, DateOnly expectedDate)
    {
        var activeDates = component.FindAll(".calendar-day[aria-pressed='true']");

        Assert.Multiple(() =>
        {
            Assert.That(activeDates, Has.Count.EqualTo(1));
            Assert.That(activeDates[0].GetAttribute("aria-label"), Is.EqualTo(expectedDate.ToString("D", CultureInfo.CurrentCulture)));
            Assert.That(activeDates[0].GetAttribute("tabindex"), Is.EqualTo("0"));
            Assert.That(component.FindAll(".calendar-day[tabindex='0']"), Has.Count.EqualTo(1));
        });

        if (component.FindAll(".calendar-grid").Count > 0)
        {
            Assert.That(component.FindAll(".calendar-day[aria-pressed='false'][tabindex='-1']"), Is.Not.Empty);
        }
    }

    private sealed class FixedTimeProvider(DateOnly date) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = this.originalCulture;
            CultureInfo.CurrentUICulture = this.originalUiCulture;
        }
    }
}
