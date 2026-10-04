// <copyright file="UpdateIncomeReceiptRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Corrects an explicitly confirmed income receipt.</summary>
/// <param name="ReceivedDate">The actual past or current receipt date.</param>
/// <param name="Amount">The positive amount received.</param>
/// <param name="Allocations">The corrected account destinations, totaling the amount.</param>
public sealed record UpdateIncomeReceiptRequest(
    DateOnly ReceivedDate,
    decimal Amount,
    IReadOnlyList<IncomeAllocationRequest> Allocations);
