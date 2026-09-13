// <copyright file="IncomeAllocationResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Describes one income allocation to an account.</summary>
/// <param name="AccountId">The destination account identifier.</param>
/// <param name="Amount">The allocated amount.</param>
public sealed record IncomeAllocationResponse(Guid AccountId, decimal Amount);
