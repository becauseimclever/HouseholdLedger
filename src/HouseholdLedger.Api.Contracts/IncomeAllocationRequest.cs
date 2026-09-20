// <copyright file="IncomeAllocationRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Requests the allocation of one income receipt amount to an account.</summary>
/// <param name="AccountId">The destination account identifier.</param>
/// <param name="Amount">The positive amount allocated to the account.</param>
public sealed record IncomeAllocationRequest(Guid AccountId, decimal Amount);
