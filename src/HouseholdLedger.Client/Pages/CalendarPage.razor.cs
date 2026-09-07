// <copyright file="CalendarPage.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Pages;

using System.Globalization;

using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Formatting;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Presents the calendar workspace surface.
/// </summary>
public partial class CalendarPage : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private IReadOnlyDictionary<DateOnly, DailyExpenseSummaryResponse> expenseSummaries =
        new Dictionary<DateOnly, DailyExpenseSummaryResponse>();

    private CancellationTokenSource? summaryRequestCancellation;

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

    /// <summary>
    /// Gets or sets the monthly expense summary client.
    /// </summary>
    [Inject]
    private IMonthlyExpenseSummaryApiClient MonthlyExpenseSummaryApi { get; set; } = null!;

    /// <summary>Gets or sets the global display-currency state.</summary>
    [CascadingParameter]
    private GlobalSettingsState? DisplayCurrency { get; set; }

    /// <summary>
    /// Gets or sets the shared selected-date state.
    /// </summary>
    [Inject]
    private SelectedDateState SelectedDate { get; set; } = null!;

    private DateOnly ActiveDate { get; set; }

    private CalendarMode CurrentMode { get; set; } = CalendarMode.Month;

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
    public void Dispose()
    {
        this.SelectedDate.TransactionsChanged -= this.OnTransactionsChanged;
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed -= this.RefreshCurrency;
        }

        this.summaryRequestCancellation?.Cancel();
        this.summaryRequestCancellation?.Dispose();
        this.lifetimeCancellation.Cancel();
        this.lifetimeCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        this.currentDate = DateOnly.FromDateTime(this.Clock.GetLocalNow().DateTime);
        this.ActiveDate = this.currentDate;
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed += this.RefreshCurrency;
            _ = this.DisplayCurrency.EnsureLoadedAsync(this.lifetimeCancellation.Token);
        }

        this.SelectedDate.TransactionsChanged += this.OnTransactionsChanged;
        this.SelectedDate.Select(this.currentDate);
        await this.LoadExpenseSummariesAsync();
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

    private static string GetSummaryId(DateOnly date) => $"expense-summary-{date:yyyyMMdd}";

    private string FormatCurrency(decimal amount) => MoneyFormatter.Format(
        amount,
        this.DisplayCurrency?.CurrentCode ?? MoneyFormatter.DefaultCurrencyCode);

    private void RefreshCurrency() => _ = this.InvokeAsync(this.StateHasChanged);

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

    private DailyExpenseSummaryResponse? GetExpenseSummary(DateOnly date)
    {
        var summary = this.expenseSummaries.GetValueOrDefault(date);
        return summary?.DailyTotal > 0 ? summary : null;
    }

    private async Task LoadExpenseSummariesAsync()
    {
        var year = this.ActiveDate.Year;
        var month = this.ActiveDate.Month;
        this.summaryRequestCancellation?.Cancel();
        this.summaryRequestCancellation?.Dispose();
        var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.lifetimeCancellation.Token);
        this.summaryRequestCancellation = requestCancellation;
        this.expenseSummaries = new Dictionary<DateOnly, DailyExpenseSummaryResponse>();

        try
        {
            var summaries = await this.MonthlyExpenseSummaryApi.GetAsync(
                year,
                month,
                requestCancellation.Token);
            if (!requestCancellation.IsCancellationRequested
                && this.ActiveDate.Year == year
                && this.ActiveDate.Month == month)
            {
                this.expenseSummaries = summaries.ToDictionary(summary => summary.Date);
            }
        }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            if (!requestCancellation.IsCancellationRequested
                && this.ActiveDate.Year == year
                && this.ActiveDate.Month == month)
            {
                this.expenseSummaries = new Dictionary<DateOnly, DailyExpenseSummaryResponse>();
            }
        }
        finally
        {
            if (ReferenceEquals(this.summaryRequestCancellation, requestCancellation))
            {
                this.summaryRequestCancellation = null;
            }

            requestCancellation.Dispose();
        }
    }

    private async Task HandleGridKeyDown(DateOnly date, KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "Enter":
            case " ":
            case "Spacebar":
                await this.SelectDateAsync(date, true);
                break;
            case "ArrowLeft":
                await this.SelectDateAsync(date.AddDays(-1), true);
                break;
            case "ArrowRight":
                await this.SelectDateAsync(date.AddDays(1), true);
                break;
            case "ArrowUp":
                await this.SelectDateAsync(date.AddDays(-7), true);
                break;
            case "ArrowDown":
                await this.SelectDateAsync(date.AddDays(7), true);
                break;
            case "Tab" when args.ShiftKey && date == this.GetFirstDisplayedDate():
                this.pendingFocus = FocusTarget.ActiveMode;
                break;
            case "Tab" when !args.ShiftKey && date == this.GetLastDisplayedDate():
                this.pendingFocus = FocusTarget.PreviousPeriod;
                break;
            case "Tab":
                await this.SelectDateAsync(date.AddDays(args.ShiftKey ? -1 : 1), true);
                break;
        }
    }

    private DateOnly GetFirstDisplayedDate() => this.CurrentMode == CalendarMode.Week
        ? this.WeekDates[0]
        : new DateOnly(this.ActiveDate.Year, this.ActiveDate.Month, 1);

    private DateOnly GetLastDisplayedDate() => this.CurrentMode == CalendarMode.Week
        ? this.WeekDates[^1]
        : new DateOnly(this.ActiveDate.Year, this.ActiveDate.Month, 1).AddMonths(1).AddDays(-1);

    private async Task MoveActiveDate(int direction)
    {
        var targetDate = this.CurrentMode switch
        {
            CalendarMode.Today => this.ActiveDate.AddDays(direction),
            CalendarMode.Week => this.ActiveDate.AddDays(direction * 7),
            CalendarMode.Month => this.ActiveDate.AddMonths(direction),
            _ => throw new InvalidOperationException("The calendar mode is not supported."),
        };

        await this.SelectDateAsync(targetDate, true);
    }

    private void OnTransactionsChanged(DateOnly ledgerDate)
    {
        if (ledgerDate.Year == this.ActiveDate.Year && ledgerDate.Month == this.ActiveDate.Month)
        {
            _ = this.InvokeAsync(async () =>
            {
                await this.LoadExpenseSummariesAsync();
                this.StateHasChanged();
            });
        }
    }

    private async Task SelectDateAsync(DateOnly date, bool shouldFocus)
    {
        var monthChanged = date.Year != this.ActiveDate.Year || date.Month != this.ActiveDate.Month;
        this.ActiveDate = date;
        this.SelectedDate.Select(date);
        this.pendingFocus = shouldFocus && this.CurrentMode != CalendarMode.Today
            ? FocusTarget.ActiveDate
            : FocusTarget.None;

        if (monthChanged && this.CurrentMode == CalendarMode.Month)
        {
            await this.LoadExpenseSummariesAsync();
        }
    }

    private async Task SetMode(CalendarMode mode)
    {
        this.CurrentMode = mode;
        if (mode == CalendarMode.Month)
        {
            await this.LoadExpenseSummariesAsync();
        }
    }
}
