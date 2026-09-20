// <copyright file="IncomeReceiptResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Describes a materialized income receipt.</summary>
/// <param name="Id">The receipt identifier.</param>
/// <param name="ScheduleId">The originating pay schedule identifier.</param>
/// <param name="PayDate">The receipt pay date.</param>
/// <param name="NetIncome">The received net income.</param>
/// <param name="Allocations">The immutable account allocation snapshot.</param>
public sealed record IncomeReceiptResponse(
    Guid Id,
    Guid ScheduleId,
    DateOnly PayDate,
    decimal NetIncome,
    IReadOnlyList<IncomeAllocationResponse> Allocations);
