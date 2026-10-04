// <copyright file="MonthlyBudgetPlanRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Supplies the user-entered amounts for one calendar month's budget plan.</summary>
/// <param name="ExpectedIncome">Expected take-home income.</param>
/// <param name="IntendedSavings">The amount the user intends to save.</param>
/// <param name="Necessities">Planned Necessities spending.</param>
/// <param name="Optional">Planned Optional spending.</param>
/// <param name="Culture">Planned Culture spending.</param>
/// <param name="Unexpected">Planned Unexpected spending.</param>
public sealed record MonthlyBudgetPlanRequest(
    decimal ExpectedIncome,
    decimal IntendedSavings,
    decimal Necessities,
    decimal Optional,
    decimal Culture,
    decimal Unexpected);
