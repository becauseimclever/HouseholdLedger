// <copyright file="MonthlyBudgetPlanRecord.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

internal sealed class MonthlyBudgetPlanRecord
{
    public DateOnly Month { get; set; }

    public decimal ExpectedIncome { get; set; }

    public decimal IntendedSavings { get; set; }

    public decimal Necessities { get; set; }

    public decimal Optional { get; set; }

    public decimal Culture { get; set; }

    public decimal Unexpected { get; set; }

    public DateTimeOffset LastRevisedAt { get; set; }
}
