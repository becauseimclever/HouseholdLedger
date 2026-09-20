// <copyright file="IncomeScheduleDto.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

using HouseholdLedger.Domain.Income;

/// <summary>Represents a recurring income schedule returned by an Application use case.</summary>
public sealed record IncomeScheduleDto(
    Guid Id,
    string Name,
    DateOnly FirstPayDate,
    PayPeriodCadence Cadence,
    decimal NetIncome,
    IReadOnlyList<IncomeAllocationCommand> Allocations,
    int? SecondMonthlyPayDay,
    bool IsPaused);
