// <copyright file="IncomeScheduleCommand.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

using HouseholdLedger.Domain.Income;

/// <summary>Supplies values for creating or revising a recurring income schedule.</summary>
/// <param name="Name">The user-visible schedule name.</param>
/// <param name="FirstPayDate">The first pay date effective for the schedule.</param>
/// <param name="Cadence">The recurrence cadence.</param>
/// <param name="NetIncome">The positive net income for each receipt.</param>
/// <param name="Allocations">The intended account allocations.</param>
/// <param name="SecondMonthlyPayDay">The second monthly pay day for a semimonthly schedule.</param>
public sealed record IncomeScheduleCommand(
    string Name,
    DateOnly FirstPayDate,
    PayPeriodCadence Cadence,
    decimal NetIncome,
    IReadOnlyList<IncomeAllocationCommand> Allocations,
    int? SecondMonthlyPayDay = null);
