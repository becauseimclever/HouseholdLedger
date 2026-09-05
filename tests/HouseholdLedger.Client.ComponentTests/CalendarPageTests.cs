// <copyright file="CalendarPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Globalization;

using Bunit;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Pages;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Verifies the calendar workspace surface.
/// </summary>
[CollectionDefinition("Static culture", DisableParallelization = true)]
[Collection("Static culture")]
public sealed class CalendarPageTests
{
    private static readonly string[] MondayFirstGermanHeaders = ["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"];

    /// <summary>
    /// Verifies that the injected client-local date initializes the selected Month presentation.
    /// </summary>
    [Fact]
    public void InitialRenderUsesInjectedLocalDateAndExposesOneActivePresentationMode()
    {
        using var context = new BunitContext();
        var expectedDate = new DateOnly(2024, 2, 29);
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(expectedDate));
        context.Services.AddSingleton<IMonthlyExpenseSummaryApiClient>(new StubMonthlyExpenseSummaryApiClient());
        context.Services.AddScoped<SelectedDateState>();

        var component = context.Render<CalendarPage>();
        var modes = component.FindAll("input[name='calendar-mode']");
        var selectedDate = component.Find(".calendar-grid .calendar-day[aria-pressed='true']");
        var selectedDateState = context.Services.GetRequiredService<SelectedDateState>();

        Assert.Multiple(
            () => Assert.Single(component.FindAll("main.calendar-page")),
            () => Assert.Single(component.FindAll("main.calendar-page h1")),
            () => Assert.Equal("Calendar", component.Find("main.calendar-page h1").TextContent),
            () => Assert.Equal(3, modes.Count),
            () => Assert.Equal(1, modes.Count(mode => mode.HasAttribute("checked"))),
            () => Assert.Equal("This Month", component.Find("input[name='calendar-mode'][checked]").ParentElement!.TextContent.Trim()),
            () => Assert.Single(component.FindAll("table.calendar-grid")),
            () => Assert.Equal(expectedDate.ToString("D", CultureInfo.CurrentCulture), selectedDate.GetAttribute("aria-label")),
            () => Assert.Equal("true", selectedDate.GetAttribute("aria-pressed")),
            () => Assert.Equal("date", selectedDate.GetAttribute("aria-current")),
            () => Assert.Equal("0", selectedDate.GetAttribute("tabindex")),
            () => Assert.Equal(expectedDate, selectedDateState.Value));
    }

    /// <summary>
    /// Verifies that native mode controls are exclusive and preserve the active date.
    /// </summary>
    [Fact]
    public void ModeControlsAreExclusiveAndPresentTheContainingDayWeekOrMonth()
    {
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 2, 29);
        var firstDayOffset = ((int)activeDate.DayOfWeek - (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek + 7) % 7;
        var expectedWeekStart = activeDate.AddDays(-firstDayOffset);
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(activeDate));
        context.Services.AddSingleton<IMonthlyExpenseSummaryApiClient>(new StubMonthlyExpenseSummaryApiClient());
        context.Services.AddScoped<SelectedDateState>();
        var component = context.Render<CalendarPage>();

        component.FindAll("input[name='calendar-mode']")[1].Change();

        Assert.Multiple(
            () => Assert.Single(component.FindAll("input[name='calendar-mode'][checked]")),
            () => Assert.Equal("This Week", component.Find("input[name='calendar-mode'][checked]").ParentElement!.TextContent.Trim()),
            () => Assert.Contains(activeDate.ToString("D", CultureInfo.CurrentCulture), component.Find("table.calendar-grid").GetAttribute("aria-label"), StringComparison.Ordinal),
            () => Assert.Equal(7, component.FindAll("table.calendar-grid thead th[scope='col']").Count),
            () => Assert.Equal(7, component.FindAll("table.calendar-grid .calendar-day").Count),
            () => Assert.Equal(expectedWeekStart.ToString("D", CultureInfo.CurrentCulture), component.FindAll("table.calendar-grid .calendar-day")[0].GetAttribute("aria-label")),
            () => Assert.Single(component.FindAll(".calendar-day[aria-pressed='true']")));

        component.FindAll("input[name='calendar-mode']")[2].Change();

        Assert.Multiple(
            () => Assert.Single(component.FindAll("input[name='calendar-mode'][checked]")),
            () => Assert.Equal("This Month", component.Find("input[name='calendar-mode'][checked]").ParentElement!.TextContent.Trim()),
            () => Assert.Equal("Month of February 2024", component.Find("table.calendar-grid").GetAttribute("aria-label")),
            () => Assert.Equal(7, component.FindAll("table.calendar-grid thead th[scope='col']").Count),
            () => Assert.Equal(29, component.FindAll("table.calendar-grid .calendar-day").Count),
            () => Assert.NotEmpty(component.FindAll("td.calendar-empty-cell[aria-hidden='true']")),
            () => Assert.Equal(activeDate.ToString("D", CultureInfo.CurrentCulture), component.Find(".calendar-day[aria-pressed='true']").GetAttribute("aria-label")));
    }

    /// <summary>
    /// Verifies month cells present their date, daily total, and cumulative monthly spending.
    /// </summary>
    [Fact]
    public void MonthCellsPresentDailyAndMonthToDateExpenseTotals()
    {
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 2, 2);
        var api = new StubMonthlyExpenseSummaryApiClient();
        api.Seed(new DailyExpenseSummaryResponse(activeDate, 7.25m, 19.75m));
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(activeDate));
        context.Services.AddSingleton<IMonthlyExpenseSummaryApiClient>(api);
        context.Services.AddScoped<SelectedDateState>();

        var component = context.Render<CalendarPage>();
        var dateButton = FindDateButton(component, activeDate);
        var summary = component.Find($"#{dateButton.GetAttribute("aria-describedby")}");

        Assert.Multiple(
            () => Assert.Equal("2", dateButton.QuerySelector(".calendar-day-date-number")!.TextContent),
            () => Assert.Contains("Daily total$7.25", summary.TextContent, StringComparison.Ordinal),
            () => Assert.Contains("Month to date$19.75", summary.TextContent, StringComparison.Ordinal),
            () => Assert.Equal(1, api.GetCallCount));
    }

    /// <summary>Verifies a zero-expense date remains visually quiet.</summary>
    [Fact]
    public void MonthCellsDoNotPresentZeroExpenseSummaries()
    {
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 2, 2);
        var api = new StubMonthlyExpenseSummaryApiClient();
        api.Seed(new DailyExpenseSummaryResponse(activeDate, 0m, 19.75m));
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(activeDate));
        context.Services.AddSingleton<IMonthlyExpenseSummaryApiClient>(api);
        context.Services.AddScoped<SelectedDateState>();

        var component = context.Render<CalendarPage>();
        var dateButton = FindDateButton(component, activeDate);

        Assert.Multiple(
            () => Assert.Equal("2", dateButton.TextContent.Trim()),
            () => Assert.False(dateButton.HasAttribute("aria-describedby")),
            () => Assert.Empty(dateButton.QuerySelectorAll(".calendar-day-summary")));
    }

    /// <summary>Verifies month navigation refreshes the displayed month's expense summaries.</summary>
    [Fact]
    public void MonthNavigationLoadsTheNewMonthsExpenseSummaries()
    {
        using var context = new BunitContext();
        var initialDate = new DateOnly(2024, 2, 29);
        var nextMonthDate = new DateOnly(2024, 3, 29);
        var api = new StubMonthlyExpenseSummaryApiClient();
        api.Seed(new DailyExpenseSummaryResponse(nextMonthDate, 9m, 23m));
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(initialDate));
        context.Services.AddSingleton<IMonthlyExpenseSummaryApiClient>(api);
        context.Services.AddScoped<SelectedDateState>();
        var component = context.Render<CalendarPage>();

        MovePeriod(component, "Next");

        Assert.Multiple(
            () => Assert.Equal("March 2024", component.Find("#calendar-period-heading").TextContent),
            () => Assert.Contains("Daily total$9.00", FindDateButton(component, nextMonthDate).TextContent, StringComparison.Ordinal),
            () => Assert.Equal(2, api.GetCallCount));
    }

    /// <summary>Verifies a superseded month response cannot replace the currently displayed summary.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LateMonthResponseDoesNotReplaceTheCurrentMonthsExpenseSummaries()
    {
        using var context = new BunitContext();
        var initialDate = new DateOnly(2024, 2, 29);
        var api = new ControllableMonthlyExpenseSummaryApiClient();
        api.CompleteImmediately(
            2024,
            2,
            new DailyExpenseSummaryResponse(initialDate, 1m, 1m));
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(initialDate));
        context.Services.AddSingleton<IMonthlyExpenseSummaryApiClient>(api);
        context.Services.AddScoped<SelectedDateState>();
        var component = context.Render<CalendarPage>();

        var marchNavigation = component.Find("button[aria-label='Next period']").ClickAsync(new MouseEventArgs());
        await api.WaitForRequestAsync(2024, 3);
        var aprilNavigation = component.Find("button[aria-label='Next period']").ClickAsync(new MouseEventArgs());
        await api.WaitForRequestAsync(2024, 4);
        api.Complete(
            2024,
            4,
            new DailyExpenseSummaryResponse(new DateOnly(2024, 4, 29), 44m, 44m));
        await aprilNavigation;
        api.Complete(
            2024,
            3,
            new DailyExpenseSummaryResponse(new DateOnly(2024, 3, 29), 33m, 33m));
        await marchNavigation;

        Assert.Multiple(
            () => Assert.Equal("April 2024", component.Find("#calendar-period-heading").TextContent),
            () => Assert.Contains("Daily total$44.00", FindDateButton(component, new DateOnly(2024, 4, 29)).TextContent, StringComparison.Ordinal),
            () => Assert.DoesNotContain("$33.00", component.Markup, StringComparison.Ordinal));
    }

    /// <summary>Verifies transaction mutations refresh the visible month's summaries.</summary>
    [Fact]
    public void TransactionChangesRefreshTheVisibleMonth()
    {
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 2, 29);
        var api = new StubMonthlyExpenseSummaryApiClient();
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(activeDate));
        context.Services.AddSingleton<IMonthlyExpenseSummaryApiClient>(api);
        context.Services.AddScoped<SelectedDateState>();
        var component = context.Render<CalendarPage>();

        context.Services.GetRequiredService<SelectedDateState>().NotifyTransactionsChanged(activeDate);

        component.WaitForAssertion(() => Assert.Equal(2, api.GetCallCount));
    }

    /// <summary>
    /// Verifies that direct and native keyboard activation select a single focused month date.
    /// </summary>
    [Fact]
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
    [Fact]
    public void MonthTabTraversalMovesSelectionInChronologicalOrderAcrossMonthBoundary()
    {
        using var context = new BunitContext();
        var component = RenderCalendar(context, new DateOnly(2024, 1, 30));
        SelectMode(component, 2);

        FindDateButton(component, new DateOnly(2024, 1, 30)).KeyDown(new KeyboardEventArgs { Key = "Tab" });
        AssertActiveDate(component, new DateOnly(2024, 1, 31));

        FindDateButton(component, new DateOnly(2024, 1, 31)).KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        AssertActiveDate(component, new DateOnly(2024, 2, 1));
        Assert.Equal("February 2024", component.Find("#calendar-period-heading").TextContent);

        FindDateButton(component, new DateOnly(2024, 2, 1)).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        AssertActiveDate(component, new DateOnly(2024, 1, 31));

        FindDateButton(component, new DateOnly(2024, 1, 31)).KeyDown(new KeyboardEventArgs { Key = "Tab", ShiftKey = true });
        AssertActiveDate(component, new DateOnly(2024, 1, 30));
    }

    /// <summary>
    /// Verifies chronological Tab and directional movement in the culture-derived weekly grid.
    /// </summary>
    [Fact]
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
        Assert.Contains(new DateOnly(2024, 3, 7).ToString("D", CultureInfo.CurrentCulture), component.Find("table.calendar-grid").GetAttribute("aria-label"), StringComparison.Ordinal);

        FindDateButton(component, new DateOnly(2024, 3, 7)).KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        AssertActiveDate(component, new DateOnly(2024, 2, 29));
    }

    /// <summary>
    /// Verifies that Shift+Tab from the first weekly date leaves the grid without changing its selection or period.
    /// </summary>
    [Fact]
    public void WeekGridFirstDateShiftTabLeavesToTheActiveModeWithoutChangingTheDateOrPeriod()
    {
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 2, 29);
        var component = RenderCalendar(context, activeDate);
        SelectMode(component, 1);
        var firstDate = GetWeekStart(activeDate);
        var weekLabel = component.Find("table.calendar-grid").GetAttribute("aria-label");

        FindDateButton(component, firstDate).KeyDown(new KeyboardEventArgs { Key = "Tab", ShiftKey = true });

        Assert.Multiple(
            () => AssertActiveDate(component, activeDate),
            () => Assert.Equal(weekLabel, component.Find("table.calendar-grid").GetAttribute("aria-label")),
            () => Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus"));
    }

    /// <summary>
    /// Verifies that Tab from each final grid date leaves to period navigation without changing the selected date or period.
    /// </summary>
    [Fact]
    public void GridLastDateTabLeavesToPreviousPeriodNavigationWithoutChangingTheDateOrPeriod()
    {
        using var context = new BunitContext();
        var weekActiveDate = new DateOnly(2024, 2, 29);
        var component = RenderCalendar(context, weekActiveDate);
        SelectMode(component, 1);
        var weekLabel = component.Find("table.calendar-grid").GetAttribute("aria-label");

        FindDateButton(component, GetWeekStart(weekActiveDate).AddDays(6)).KeyDown(new KeyboardEventArgs { Key = "Tab" });

        Assert.Multiple(
            () => AssertActiveDate(component, weekActiveDate),
            () => Assert.Equal(weekLabel, component.Find("table.calendar-grid").GetAttribute("aria-label")),
            () => AssertFocusWasRequested(context));

        var monthActiveDate = new DateOnly(2024, 2, 29);
        SelectMode(component, 2);
        var monthLabel = component.Find("table.calendar-grid").GetAttribute("aria-label");
        var focusRequestCount = GetFocusRequestCount(context);

        FindDateButton(component, monthActiveDate).KeyDown(new KeyboardEventArgs { Key = "Tab" });

        Assert.Multiple(
            () => AssertActiveDate(component, monthActiveDate),
            () => Assert.Equal(monthLabel, component.Find("table.calendar-grid").GetAttribute("aria-label")),
            () => AssertFocusWasRequested(context, focusRequestCount));
    }

    /// <summary>
    /// Verifies that Today exposes one native date control without grid keyboard movement or managed Tab behavior.
    /// </summary>
    [Fact]
    public void TodayUsesNativeSequentialAndActivationBehaviorForItsSingleSelectedDate()
    {
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 2, 29);
        var component = RenderCalendar(context, activeDate);
        SelectMode(component, 0);
        var todayDate = FindDateButton(component, activeDate);

        Assert.Throws<Bunit.MissingEventHandlerException>(
            () => todayDate.KeyDown(new KeyboardEventArgs { Key = "Tab" }));
        todayDate.Click();

        Assert.Multiple(
            () => AssertActiveDate(component, activeDate),
            () => Assert.Empty(component.FindAll("table.calendar-grid")),
            () => Assert.Equal("0", todayDate.GetAttribute("tabindex")),
            () => Assert.Empty(context.JSInterop.Invocations));

        MovePeriod(component, "Next");
        Assert.Multiple(
            () => AssertActiveDate(component, activeDate.AddDays(1)),
            () => Assert.Equal(activeDate.AddDays(1).ToString("D", CultureInfo.CurrentCulture), component.Find("#calendar-period-heading").TextContent),
            () => Assert.Equal($"Showing {activeDate.AddDays(1).ToString("D", CultureInfo.CurrentCulture)}.", component.Find(".calendar-period-status").TextContent));
    }

    /// <summary>
    /// Verifies that a Monday-first culture determines both weekday rendering and horizontal arrow direction.
    /// </summary>
    [Fact]
    public void WeekGridUsesCurrentCultureFirstDayForRenderedOrderAndHorizontalArrows()
    {
        using var cultureScope = new CultureScope("de-DE");
        using var context = new BunitContext();
        var activeDate = new DateOnly(2024, 3, 3);
        var component = RenderCalendar(context, activeDate);
        SelectMode(component, 1);

        var headers = component.FindAll("table.calendar-grid thead th[scope='col']").Select(header => header.TextContent).ToArray();
        Assert.Equal(MondayFirstGermanHeaders, headers);
        Assert.Equal(new DateOnly(2024, 2, 26).ToString("D", CultureInfo.CurrentCulture), FindDateButton(component, new DateOnly(2024, 2, 26)).GetAttribute("aria-label"));

        FindDateButton(component, activeDate).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        AssertActiveDate(component, activeDate.AddDays(-1));

        FindDateButton(component, activeDate.AddDays(-1)).KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        AssertActiveDate(component, activeDate);
    }

    /// <summary>
    /// Verifies that cross-week keyboard movement updates status and retains focus on the selected date.
    /// </summary>
    [Fact]
    public void WeekArrowBoundaryUpdatesContainingWeekStatusAndRetainsFocus()
    {
        using var context = new BunitContext();
        var activeDate = GetWeekStart(new DateOnly(2024, 2, 29));
        var component = RenderCalendar(context, activeDate);
        SelectMode(component, 1);
        var focusRequestCount = GetFocusRequestCount(context);

        FindDateButton(component, activeDate).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        var expectedDate = activeDate.AddDays(-1);
        Assert.Multiple(
            () => AssertActiveDate(component, expectedDate),
            () => Assert.Contains(expectedDate.ToString("D", CultureInfo.CurrentCulture), component.Find("table.calendar-grid").GetAttribute("aria-label"), StringComparison.Ordinal),
            () => Assert.Equal($"Showing the week containing {expectedDate.ToString("D", CultureInfo.CurrentCulture)}.", component.Find(".calendar-period-status").TextContent),
            () => AssertFocusWasRequested(context, focusRequestCount));
    }

    /// <summary>
    /// Verifies month keyboard and period navigation routes retain focus while changing periods across year and leap-day boundaries.
    /// </summary>
    [Fact]
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
    [Fact]
    public void PreviousAndNextMoveTheActiveDateByModeAcrossYearAndLeapMonthBoundaries()
    {
        using var context = new BunitContext();
        var component = RenderCalendar(context, new DateOnly(2024, 12, 31));
        SelectMode(component, 0);

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
        Assert.Equal("February 2024", leapComponent.Find("#calendar-period-heading").TextContent);
    }

    private static IRenderedComponent<CalendarPage> RenderCalendar(BunitContext context, DateOnly date)
    {
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(date));
        context.Services.AddSingleton<IMonthlyExpenseSummaryApiClient>(new StubMonthlyExpenseSummaryApiClient());
        context.Services.AddScoped<SelectedDateState>();
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
        Assert.Multiple(
            () => AssertActiveDate(component, expectedDate),
            () => Assert.Equal(expectedPeriod, component.Find("#calendar-period-heading").TextContent),
            () => Assert.Equal($"Showing {expectedPeriod}.", component.Find(".calendar-period-status").TextContent),
            () => AssertFocusWasRequested(context, focusRequestCount));
    }

    private static int GetFocusRequestCount(BunitContext context)
    {
        return context.JSInterop.Invocations.Count(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }

    private static void AssertFocusWasRequested(BunitContext context, int focusRequestCount = 0)
    {
        Assert.True(GetFocusRequestCount(context) > focusRequestCount);
    }

    private static void AssertActiveDate(IRenderedComponent<CalendarPage> component, DateOnly expectedDate)
    {
        var activeDates = component.FindAll(".calendar-day[aria-pressed='true']");

        Assert.Multiple(
            () => Assert.Single(activeDates),
            () => Assert.Equal(expectedDate.ToString("D", CultureInfo.CurrentCulture), activeDates[0].GetAttribute("aria-label")),
            () => Assert.Equal("0", activeDates[0].GetAttribute("tabindex")),
            () => Assert.Single(component.FindAll(".calendar-day[tabindex='0']")));

        if (component.FindAll(".calendar-grid").Count > 0)
        {
            Assert.NotEmpty(component.FindAll(".calendar-day[aria-pressed='false'][tabindex='-1']"));
        }
    }

    private sealed class FixedTimeProvider(DateOnly date) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    private sealed class StubMonthlyExpenseSummaryApiClient : IMonthlyExpenseSummaryApiClient
    {
        private readonly List<DailyExpenseSummaryResponse> summaries = [];

        public int GetCallCount { get; private set; }

        public Task<IReadOnlyList<DailyExpenseSummaryResponse>> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.GetCallCount++;
            return Task.FromResult<IReadOnlyList<DailyExpenseSummaryResponse>>(
                this.summaries.Where(summary => summary.Date.Year == year && summary.Date.Month == month).ToArray());
        }

        public void Seed(DailyExpenseSummaryResponse summary)
        {
            this.summaries.Add(summary);
        }
    }

    private sealed class ControllableMonthlyExpenseSummaryApiClient : IMonthlyExpenseSummaryApiClient
    {
        private readonly Dictionary<(int Year, int Month), TaskCompletionSource<IReadOnlyList<DailyExpenseSummaryResponse>>> responses = [];

        public Task<IReadOnlyList<DailyExpenseSummaryResponse>> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken)
        {
            var response = this.GetResponse(year, month);
            return response.Task;
        }

        public void Complete(int year, int month, params DailyExpenseSummaryResponse[] summaries)
        {
            this.GetResponse(year, month).TrySetResult(summaries);
        }

        public void CompleteImmediately(int year, int month, params DailyExpenseSummaryResponse[] summaries)
        {
            this.Complete(year, month, summaries);
        }

        public async Task WaitForRequestAsync(int year, int month)
        {
            while (!this.responses.ContainsKey((year, month)))
            {
                await Task.Yield();
            }
        }

        private TaskCompletionSource<IReadOnlyList<DailyExpenseSummaryResponse>> GetResponse(int year, int month)
        {
            if (!this.responses.TryGetValue((year, month), out var response))
            {
                response = new TaskCompletionSource<IReadOnlyList<DailyExpenseSummaryResponse>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                this.responses.Add((year, month), response);
            }

            return response;
        }
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
