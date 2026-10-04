// <copyright file="MonthlyBudgetPlanCommand.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Planning;

/// <summary>Contains the amounts for a deliberate monthly budget plan.</summary>
public sealed record MonthlyBudgetPlanCommand(
    decimal ExpectedIncome,
    decimal IntendedSavings,
    decimal Necessities,
    decimal Optional,
    decimal Culture,
    decimal Unexpected);
