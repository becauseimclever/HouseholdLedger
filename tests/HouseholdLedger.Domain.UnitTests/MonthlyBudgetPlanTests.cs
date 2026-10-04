// <copyright file="MonthlyBudgetPlanTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.UnitTests;

using HouseholdLedger.Domain.Planning;
using Xunit;

/// <summary>Tests deliberate monthly plan invariants.</summary>
public sealed class MonthlyBudgetPlanTests
{
    /// <summary>Accepts zero-valued categories when the monthly plan reconciles exactly.</summary>
    [Fact]
    public void CreateWithReconciledAmountsPreservesIntent()
    {
        var revisedAt = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        var plan = new MonthlyBudgetPlan(
            new DateOnly(2026, 10, 1),
            3000m,
            500m,
            1200m,
            400m,
            300m,
            600m,
            revisedAt);

        Assert.Equal(3000m, plan.ExpectedIncome);
        Assert.Equal(500m, plan.IntendedSavings);
        Assert.Equal(1200m, plan.Necessities);
        Assert.Equal(400m, plan.Optional);
        Assert.Equal(300m, plan.Culture);
        Assert.Equal(600m, plan.Unexpected);
        Assert.Equal(revisedAt, plan.LastRevisedAt);
    }

    /// <summary>Rejects plans that do not reconcile expected income exactly.</summary>
    [Fact]
    public void CreateWithUnreconciledAmountsThrows()
    {
        Assert.Throws<ArgumentException>(() => CreatePlan(expectedIncome: 3000m, culture: 299.99m));
    }

    /// <summary>Rejects negative amounts, excess precision, and amounts over the ledger maximum.</summary>
    /// <param name="necessities">The deliberately invalid planned amount.</param>
    [Theory]
    [InlineData(-0.01)]
    [InlineData(0.001)]
    [InlineData(10000000000000000.00)]
    public void CreateWithInvalidAmountThrows(decimal necessities)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePlan(
            expectedIncome: necessities + 1800m,
            necessities: necessities));
    }

    /// <summary>Requires the identifying month to be its first calendar day.</summary>
    [Fact]
    public void CreateWithNonFirstMonthDayThrows()
    {
        Assert.Throws<ArgumentException>(() => new MonthlyBudgetPlan(
            new DateOnly(2026, 10, 2),
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            DateTimeOffset.UtcNow));
    }

    private static MonthlyBudgetPlan CreatePlan(
        decimal expectedIncome,
        decimal necessities = 1200m,
        decimal culture = 300m) =>
        new(
            new DateOnly(2026, 10, 1),
            expectedIncome,
            500m,
            necessities,
            400m,
            culture,
            600m,
            DateTimeOffset.UtcNow);
}
