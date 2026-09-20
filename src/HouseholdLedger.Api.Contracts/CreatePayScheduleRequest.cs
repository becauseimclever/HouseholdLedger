// <copyright file="CreatePayScheduleRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Requests creation of a recurring income pay schedule.</summary>
/// <param name="Name">The user-visible schedule name.</param>
/// <param name="FirstPayDate">The first pay date.</param>
/// <param name="Cadence">The schedule recurrence cadence.</param>
/// <param name="NetIncome">The positive net income for each receipt.</param>
/// <param name="Allocations">The account allocations for each receipt.</param>
/// <param name="SecondMonthlyPayDay">The second pay day for a semimonthly schedule.</param>
public sealed record CreatePayScheduleRequest(
    string Name,
    DateOnly FirstPayDate,
    PayPeriodCadence Cadence,
    decimal NetIncome,
    IReadOnlyList<IncomeAllocationRequest> Allocations,
    int? SecondMonthlyPayDay = null);
