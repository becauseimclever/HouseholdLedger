// <copyright file="PayScheduleTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.UnitTests;

using HouseholdLedger.Domain.Income;
using Xunit;

/// <summary>Tests recurring pay schedule invariants.</summary>
public sealed class PayScheduleTests
{
    /// <summary>Creates a schedule when its account allocations equal its net income.</summary>
    [Fact]
    public void CreateWithExactMultipleAccountAllocationsPreservesSchedule()
    {
        var checkingAccountId = Guid.NewGuid();
        var savingsAccountId = Guid.NewGuid();

        var schedule = new PaySchedule(
            Guid.NewGuid(),
            "Primary pay",
            new DateOnly(2026, 9, 15),
            PayPeriodCadence.Biweekly,
            2500.00m,
            [
                new IncomeAccountAllocation(checkingAccountId, 1800.00m),
                new IncomeAccountAllocation(savingsAccountId, 700.00m),
            ]);

        Assert.Equal("Primary pay", schedule.Name);
        Assert.Equal(2500.00m, schedule.NetIncome);
        Assert.Collection(
            schedule.Allocations,
            allocation => Assert.Equal(1800.00m, allocation.Amount),
            allocation => Assert.Equal(700.00m, allocation.Amount));
    }

    /// <summary>Rejects multiple allocations for the same account.</summary>
    [Fact]
    public void CreateWithDuplicateAccountAllocationThrows()
    {
        var accountId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new PaySchedule(
            Guid.NewGuid(),
            "Primary pay",
            new DateOnly(2026, 9, 15),
            PayPeriodCadence.Biweekly,
            2500.00m,
            [
                new IncomeAccountAllocation(accountId, 1800.00m),
                new IncomeAccountAllocation(accountId, 700.00m),
            ]));
    }

    /// <summary>Rejects allocations whose total differs from the net income.</summary>
    [Fact]
    public void CreateWithAllocationTotalDifferentFromIncomeThrows()
    {
        Assert.Throws<ArgumentException>(() => new PaySchedule(
            Guid.NewGuid(),
            "Primary pay",
            new DateOnly(2026, 9, 15),
            PayPeriodCadence.Biweekly,
            2500.00m,
            [new IncomeAccountAllocation(Guid.NewGuid(), 2499.99m)]));
    }
}
