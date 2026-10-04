// <copyright file="IncomeReceiptCommand.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

/// <summary>Describes income the user confirms was received.</summary>
public sealed record IncomeReceiptCommand(
    DateOnly ReceivedDate,
    decimal Amount,
    IReadOnlyList<IncomeAllocationCommand> Allocations,
    Guid? RequestId = null);
