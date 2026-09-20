// <copyright file="RevisePayScheduleRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Requests replacement of a pay schedule's future settings.</summary>
/// <param name="Name">The replacement schedule name.</param>
/// <param name="FirstPayDate">The first pay date using the replacement settings.</param>
/// <param name="Cadence">The replacement recurrence cadence.</param>
/// <param name="NetIncome">The replacement net income for each receipt.</param>
/// <param name="Allocations">The replacement account allocations for each receipt.</param>
/// <param name="SecondMonthlyPayDay">The replacement second pay day for a semimonthly schedule.</param>
public sealed record RevisePayScheduleRequest(
    string Name,
    DateOnly FirstPayDate,
    PayPeriodCadence Cadence,
    decimal NetIncome,
    IReadOnlyList<IncomeAllocationRequest> Allocations,
    int? SecondMonthlyPayDay = null);
