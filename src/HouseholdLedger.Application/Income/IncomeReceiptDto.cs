// <copyright file="IncomeReceiptDto.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

/// <summary>Represents an immutable materialized income receipt returned by an Application read use case.</summary>
/// <param name="Id">The stable receipt identifier.</param>
/// <param name="ScheduleId">The originating pay schedule identifier.</param>
/// <param name="PayDate">The calendar date on which income was received.</param>
/// <param name="NetIncome">The exact net income received.</param>
/// <param name="Allocations">The immutable account allocation snapshot.</param>
public sealed record IncomeReceiptDto(
    Guid Id,
    Guid ScheduleId,
    DateOnly PayDate,
    decimal NetIncome,
    IReadOnlyList<IncomeAllocationDto> Allocations);
