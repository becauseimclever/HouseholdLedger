// <copyright file="CalendarPage.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Pages;

using System.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Presents the calendar workspace surface.
/// </summary>
public partial class CalendarPage : ComponentBase
{
    private DateOnly currentDate;
    private ElementReference activeDateControl;
    private ElementReference monthModeControl;
    private FocusTarget pendingFocus;
    private ElementReference previousPeriodControl;
    private ElementReference todayModeControl;
    private ElementReference weekModeControl;

    private enum CalendarMode
    {
        Today,
        Week,
        Month,
    }

    private enum FocusTarget
    {
        None,
        ActiveDate,
        ActiveMode,
        PreviousPeriod,
    }

    private static CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    private static IReadOnlyList<string> WeekdayHeaders => Enumerable.Range(0, 7)
        .Select(offset => CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(
            (DayOfWeek)(((int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek + offset) % 7)))
        .ToArray();

    /// <summary>
    /// Gets or sets the client-local clock used to determine the initial active date.
    /// </summary>
    [Inject]
    private TimeProvider Clock { get; set; } = null!;

    private DateOnly ActiveDate { get; set; }

    private CalendarMode CurrentMode { get; set; } = CalendarMode.Today;

    private IReadOnlyList<IReadOnlyList<DateOnly?>> MonthWeeks => this.GetMonthWeeks();

    private string MonthTableLabel => $"Month of {this.ActiveDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture)}";

    private string PeriodHeading => this.CurrentMode switch
    {
        CalendarMode.Today => this.ActiveDate.ToString("D", CultureInfo.CurrentCulture),
        CalendarMode.Week => $"Week of {GetWeekStart(this.ActiveDate).ToString("MMMM d", CultureInfo.CurrentCulture)}",
        CalendarMode.Month => this.ActiveDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
        _ => throw new InvalidOperationException("The calendar mode is not supported."),
    };

    private string PeriodStatus => this.CurrentMode switch
    {
        CalendarMode.Today => $"Showing {AccessibleDateName(this.ActiveDate)}.",
        CalendarMode.Week => $"Showing the week containing {AccessibleDateName(this.ActiveDate)}.",
        CalendarMode.Month => $"Showing {this.ActiveDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture)}.",
        _ => throw new InvalidOperationException("The calendar mode is not supported."),
    };

    private DateOnly[] WeekDates => Enumerable.Range(0, 7)
        .Select(offset => GetWeekStart(this.ActiveDate).AddDays(offset))
        .ToArray();

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        this.currentDate = DateOnly.FromDateTime(this.Clock.GetLocalNow().DateTime);
        this.ActiveDate = this.currentDate;
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _ = firstRender;

        switch (this.pendingFocus)
        {
            case FocusTarget.ActiveDate:
                await this.activeDateControl.FocusAsync();
                break;
            case FocusTarget.ActiveMode:
                await this.GetActiveModeControl().FocusAsync();
                break;
            case FocusTarget.PreviousPeriod:
                await this.previousPeriodControl.FocusAsync();
                break;
        }

        this.pendingFocus = FocusTarget.None;
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var firstDayOfWeek = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var offset = ((int)date.DayOfWeek - firstDayOfWeek + 7) % 7;
        return date.AddDays(-offset);
    }

    private static string AccessibleDateName(DateOnly date) => date.ToString("D", CultureInfo.CurrentCulture);

    private string? CurrentDateState(DateOnly date) => date == this.currentDate ? "date" : null;

    private ElementReference GetActiveModeControl() => this.CurrentMode switch
    {
        CalendarMode.Today => this.todayModeControl,
        CalendarMode.Week => this.weekModeControl,
        CalendarMode.Month => this.monthModeControl,
        _ => throw new InvalidOperationException("The calendar mode is not supported."),
    };

    private List<IReadOnlyList<DateOnly?>> GetMonthWeeks()
    {
        var firstOfMonth = new DateOnly(this.ActiveDate.Year, this.ActiveDate.Month, 1);
        var firstVisibleDate = GetWeekStart(firstOfMonth);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        var dayCount = lastOfMonth.Day;
        var cellCount = GetWeekStart(lastOfMonth).DayNumber - firstVisibleDate.DayNumber + 7;
        var weeks = new List<IReadOnlyList<DateOnly?>>();

        for (var cellOffset = 0; cellOffset < cellCount; cellOffset += 7)
        {
            var week = new List<DateOnly?>();
            for (var dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var date = firstVisibleDate.AddDays(cellOffset + dayOffset);
                week.Add(date.Month == this.ActiveDate.Month && date.Day <= dayCount ? date : null);
            }

            weeks.Add(week);
        }

        return weeks;
    }

    private void HandleGridKeyDown(DateOnly date, KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "Enter":
            case " ":
            case "Spacebar":
                this.SelectDate(date, true);
                break;
            case "ArrowLeft":
                this.SelectDate(date.AddDays(-1), true);
                break;
            case "ArrowRight":
                this.SelectDate(date.AddDays(1), true);
                break;
            case "ArrowUp":
                this.SelectDate(date.AddDays(-7), true);
                break;
            case "ArrowDown":
                this.SelectDate(date.AddDays(7), true);
                break;
            case "Tab" when args.ShiftKey && date == this.GetFirstDisplayedDate():
                this.pendingFocus = FocusTarget.ActiveMode;
                break;
            case "Tab" when !args.ShiftKey && date == this.GetLastDisplayedDate():
                this.pendingFocus = FocusTarget.PreviousPeriod;
                break;
            case "Tab":
                this.SelectDate(date.AddDays(args.ShiftKey ? -1 : 1), true);
                break;
        }
    }

    private DateOnly GetFirstDisplayedDate() => this.CurrentMode == CalendarMode.Week
        ? this.WeekDates[0]
        : new DateOnly(this.ActiveDate.Year, this.ActiveDate.Month, 1);

    private DateOnly GetLastDisplayedDate() => this.CurrentMode == CalendarMode.Week
        ? this.WeekDates[^1]
        : new DateOnly(this.ActiveDate.Year, this.ActiveDate.Month, 1).AddMonths(1).AddDays(-1);

    private void MoveActiveDate(int direction)
    {
        var targetDate = this.CurrentMode switch
        {
            CalendarMode.Today => this.ActiveDate.AddDays(direction),
            CalendarMode.Week => this.ActiveDate.AddDays(direction * 7),
            CalendarMode.Month => this.ActiveDate.AddMonths(direction),
            _ => throw new InvalidOperationException("The calendar mode is not supported."),
        };

        this.SelectDate(targetDate, true);
    }

    private void SelectDate(DateOnly date, bool shouldFocus)
    {
        this.ActiveDate = date;
        this.pendingFocus = shouldFocus && this.CurrentMode != CalendarMode.Today
            ? FocusTarget.ActiveDate
            : FocusTarget.None;
    }

    private void SetMode(CalendarMode mode)
    {
        this.CurrentMode = mode;
    }
}
