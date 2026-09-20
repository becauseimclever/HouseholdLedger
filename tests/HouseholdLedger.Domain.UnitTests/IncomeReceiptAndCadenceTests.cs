// <copyright file="IncomeReceiptAndCadenceTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.UnitTests;

using HouseholdLedger.Domain.Income;
using Xunit;

/// <summary>Tests income receipt snapshots and pay-date recurrence.</summary>
public sealed class IncomeReceiptAndCadenceTests
{
    /// <summary>Verifies a receipt preserves an exact allocation snapshot.</summary>
    [Fact]
    public void CreateWithExactAllocationsPreservesSnapshot()
    {
        var checkingAccountId = Guid.NewGuid();
        var savingsAccountId = Guid.NewGuid();

        var receipt = new IncomeReceipt(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 15),
            2500m,
            [
                new IncomeAccountAllocation(checkingAccountId, 1800m),
                new IncomeAccountAllocation(savingsAccountId, 700m),
            ]);

        Assert.Multiple(
            () => Assert.Equal(2500m, receipt.NetIncome),
            () => Assert.Equal(2500m, receipt.Allocations.Sum(allocation => allocation.Amount)),
            () => Assert.Equal([checkingAccountId, savingsAccountId], receipt.Allocations.Select(allocation => allocation.AccountId)));
    }

    /// <summary>Verifies receipts reject an inexact allocation sum.</summary>
    [Fact]
    public void CreateWithInexactAllocationsThrows()
    {
        Assert.Throws<ArgumentException>(() => new IncomeReceipt(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 15),
            2500m,
            [new IncomeAccountAllocation(Guid.NewGuid(), 2499.99m)]));
    }

    /// <summary>Verifies interval and monthly cadences retain the first date as their anchor.</summary>
    [Fact]
    public void GetDuePayDatesUsesScheduleAnchorForSupportedCadences()
    {
        var firstPayDate = new DateOnly(2026, 1, 30);
        IncomeAccountAllocation[] allocations = [new IncomeAccountAllocation(Guid.NewGuid(), 100m)];
        var monthly = new PaySchedule(Guid.NewGuid(), "Monthly", firstPayDate, PayPeriodCadence.Monthly, 100m, allocations);
        var weekly = new PaySchedule(Guid.NewGuid(), "Weekly", firstPayDate, PayPeriodCadence.Weekly, 100m, allocations);

        var monthlyDates = PayScheduleCalendar.GetDuePayDates(monthly, firstPayDate, new DateOnly(2026, 3, 31));
        var weeklyDates = PayScheduleCalendar.GetDuePayDates(weekly, firstPayDate, new DateOnly(2026, 2, 14));

        Assert.Equal([new DateOnly(2026, 1, 30), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 30)], monthlyDates);
        Assert.Equal([new DateOnly(2026, 1, 30), new DateOnly(2026, 2, 6), new DateOnly(2026, 2, 13)], weeklyDates);
    }

    /// <summary>Verifies semimonthly pay days are explicit and clamp to each month end.</summary>
    [Fact]
    public void GetDuePayDatesClampsSemimonthlyDaysAtMonthEnd()
    {
        var schedule = new PaySchedule(
            Guid.NewGuid(),
            "Semimonthly",
            new DateOnly(2026, 1, 15),
            PayPeriodCadence.Semimonthly,
            100m,
            [new IncomeAccountAllocation(Guid.NewGuid(), 100m)],
            31);

        var dates = PayScheduleCalendar.GetDuePayDates(
            schedule,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 31));

        Assert.Equal(
            [
                new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 15), new DateOnly(2026, 2, 28),
                new DateOnly(2026, 3, 15), new DateOnly(2026, 3, 31),
            ],
            dates);
    }
}
