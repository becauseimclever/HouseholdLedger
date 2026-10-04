// <copyright file="MonthlyBudgetPlanResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Describes a saved monthly plan.</summary>
/// <param name="Year">The calendar year.</param>
/// <param name="Month">The calendar month.</param>
/// <param name="ExpectedIncome">Expected take-home income.</param>
/// <param name="IntendedSavings">The intended savings amount.</param>
/// <param name="Necessities">Planned Necessities spending.</param>
/// <param name="Optional">Planned Optional spending.</param>
/// <param name="Culture">Planned Culture spending.</param>
/// <param name="Unexpected">Planned Unexpected spending.</param>
/// <param name="LastRevisedAt">When the plan was last saved.</param>
public sealed record MonthlyBudgetPlanResponse(
    int Year,
    int Month,
    decimal ExpectedIncome,
    decimal IntendedSavings,
    decimal Necessities,
    decimal Optional,
    decimal Culture,
    decimal Unexpected,
    DateTimeOffset LastRevisedAt);
