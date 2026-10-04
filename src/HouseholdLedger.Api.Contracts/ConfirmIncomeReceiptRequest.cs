// <copyright file="ConfirmIncomeReceiptRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Records income the user confirms was actually received.</summary>
/// <param name="ReceivedDate">The actual date the income was received.</param>
/// <param name="Amount">The positive amount received.</param>
/// <param name="Allocations">The actual accounts holding the receipt.</param>
/// <param name="RequestId">An optional client-generated retry token; reuse only for the identical confirmation.</param>
public sealed record ConfirmIncomeReceiptRequest(
    DateOnly ReceivedDate,
    decimal Amount,
    IReadOnlyList<IncomeAllocationRequest> Allocations,
    Guid? RequestId = null);
