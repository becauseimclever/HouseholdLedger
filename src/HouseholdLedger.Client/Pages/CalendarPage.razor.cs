// <copyright file="CalendarPage.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Pages;

using System.Globalization;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Presents an empty calendar frame for the selected month.
/// </summary>
public partial class CalendarPage : ComponentBase
{
    private static readonly CalendarWeekday[] WeekdayLabels =
    [
        new("Sunday", "Sun"),
        new("Monday", "Mon"),
        new("Tuesday", "Tue"),
        new("Wednesday", "Wed"),
        new("Thursday", "Thu"),
        new("Friday", "Fri"),
        new("Saturday", "Sat"),
    ];

    private DateOnly displayedMonth;

    /// <summary>
    /// Gets the weekday column labels.
    /// </summary>
    protected static IReadOnlyList<CalendarWeekday> Weekdays => WeekdayLabels;

    /// <summary>
    /// Gets or sets the clock used to identify the current month and date.
    /// </summary>
    [Inject]
    protected TimeProvider TimeProvider { get; set; } = null!;

    /// <summary>
    /// Gets the displayed period label.
    /// </summary>
    protected string PeriodLabel => this.displayedMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the six complete weeks displayed by the calendar frame.
    /// </summary>
    protected IReadOnlyList<CalendarDay> CalendarDays { get; private set; } = [];

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        this.SetDisplayedMonth(this.GetCurrentDate());
    }

    /// <summary>
    /// Shows the month before the displayed period.
    /// </summary>
    protected void ShowPreviousMonth()
    {
        this.SetDisplayedMonth(this.displayedMonth.AddMonths(-1));
    }

    /// <summary>
    /// Shows the current month.
    /// </summary>
    protected void ShowCurrentMonth()
    {
        this.SetDisplayedMonth(this.GetCurrentDate());
    }

    /// <summary>
    /// Shows the month after the displayed period.
    /// </summary>
    protected void ShowNextMonth()
    {
        this.SetDisplayedMonth(this.displayedMonth.AddMonths(1));
    }

    private DateOnly GetCurrentDate()
    {
        var localNow = TimeZoneInfo.ConvertTime(this.TimeProvider.GetUtcNow(), this.TimeProvider.LocalTimeZone);
        return DateOnly.FromDateTime(localNow.DateTime);
    }

    private void SetDisplayedMonth(DateOnly date)
    {
        this.displayedMonth = new DateOnly(date.Year, date.Month, 1);
        var firstDisplayedDate = this.displayedMonth.AddDays(-(int)this.displayedMonth.DayOfWeek);
        var currentDate = this.GetCurrentDate();

        this.CalendarDays = Enumerable.Range(0, 42)
            .Select(offset => firstDisplayedDate.AddDays(offset))
            .Select(day => new CalendarDay(
                day.Day,
                day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                day.Month == this.displayedMonth.Month && day.Year == this.displayedMonth.Year,
                day == currentDate))
            .ToArray();
    }

    /// <summary>
    /// Describes one weekday heading.
    /// </summary>
    /// <param name="FullName">The unabbreviated weekday name.</param>
    /// <param name="ShortName">The compact weekday label.</param>
    protected sealed record CalendarWeekday(string FullName, string ShortName);

    /// <summary>
    /// Describes one date cell in the presentation calendar.
    /// </summary>
    /// <param name="DayNumber">The numeric day of the month.</param>
    /// <param name="IsoDate">The machine-readable calendar date.</param>
    /// <param name="IsInPeriod">Whether the date belongs to the selected month.</param>
    /// <param name="IsToday">Whether the date is today according to the client clock.</param>
    protected sealed record CalendarDay(int DayNumber, string IsoDate, bool IsInPeriod, bool IsToday);
}
