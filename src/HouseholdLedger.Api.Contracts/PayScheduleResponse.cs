// <copyright file="PayScheduleResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Describes a recurring income pay schedule.</summary>
/// <param name="Id">The schedule identifier.</param>
/// <param name="Name">The user-visible schedule name.</param>
/// <param name="FirstPayDate">The first pay date.</param>
/// <param name="Cadence">The recurrence cadence.</param>
/// <param name="NetIncome">The net income for each receipt.</param>
/// <param name="Allocations">The account allocations for each receipt.</param>
/// <param name="SecondMonthlyPayDay">The second pay day for a semimonthly schedule.</param>
/// <param name="IsPaused">Whether future receipt creation is paused.</param>
public sealed record PayScheduleResponse(
    Guid Id,
    string Name,
    DateOnly FirstPayDate,
    PayPeriodCadence Cadence,
    decimal NetIncome,
    IReadOnlyList<IncomeAllocationResponse> Allocations,
    int? SecondMonthlyPayDay,
    bool IsPaused);
