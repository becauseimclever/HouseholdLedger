// <copyright file="PayScheduleCalendar.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Income;

/// <summary>Calculates calendar pay dates from an anchored schedule.</summary>
public static class PayScheduleCalendar
{
    /// <summary>Returns due pay dates in an inclusive calendar range.</summary>
    /// <param name="schedule">The anchored pay schedule.</param>
    /// <param name="startDate">The inclusive first date to consider.</param>
    /// <param name="endDate">The inclusive final date to consider.</param>
    /// <returns>The matching pay dates in ascending order.</returns>
    public static IReadOnlyList<DateOnly> GetDuePayDates(PaySchedule schedule, DateOnly startDate, DateOnly endDate)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        if (endDate < startDate)
        {
            throw new ArgumentException("The end date must not precede the start date.", nameof(endDate));
        }

        return schedule.Cadence switch
        {
            PayPeriodCadence.Weekly => GetIntervalDates(schedule.FirstPayDate, startDate, endDate, 7),
            PayPeriodCadence.Biweekly => GetIntervalDates(schedule.FirstPayDate, startDate, endDate, 14),
            PayPeriodCadence.FourWeekly => GetIntervalDates(schedule.FirstPayDate, startDate, endDate, 28),
            PayPeriodCadence.Monthly => GetMonthlyDates(schedule, startDate, endDate, [schedule.FirstPayDate.Day]),
            PayPeriodCadence.Semimonthly => GetMonthlyDates(schedule, startDate, endDate, [schedule.FirstPayDate.Day, schedule.SecondMonthlyPayDay!.Value]),
            _ => throw new ArgumentOutOfRangeException(nameof(schedule)),
        };
    }

    private static List<DateOnly> GetIntervalDates(DateOnly anchor, DateOnly startDate, DateOnly endDate, int days)
    {
        var dates = new List<DateOnly>();
        for (var date = anchor; date <= endDate;)
        {
            if (date >= startDate)
            {
                dates.Add(date);
            }

            if (DateOnly.MaxValue.DayNumber - date.DayNumber < days)
            {
                break;
            }

            date = date.AddDays(days);
        }

        return dates;
    }

    private static List<DateOnly> GetMonthlyDates(PaySchedule schedule, DateOnly startDate, DateOnly endDate, int[] payDays)
    {
        var dates = new List<DateOnly>();
        for (var year = schedule.FirstPayDate.Year; year <= endDate.Year; year++)
        {
            var firstMonth = year == schedule.FirstPayDate.Year ? schedule.FirstPayDate.Month : 1;
            var lastMonth = year == endDate.Year ? endDate.Month : 12;
            for (var month = firstMonth; month <= lastMonth; month++)
            {
                var daysInMonth = DateTime.DaysInMonth(year, month);
                foreach (var payDay in payDays.Order())
                {
                    var date = new DateOnly(year, month, Math.Min(payDay, daysInMonth));
                    if (date >= schedule.FirstPayDate && date >= startDate && date <= endDate && !dates.Contains(date))
                    {
                        dates.Add(date);
                    }
                }
            }
        }

        return dates;
    }
}
