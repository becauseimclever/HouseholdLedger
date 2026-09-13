// <copyright file="IncomeAllocationDto.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

/// <summary>Represents one account allocation returned by an Application read use case.</summary>
/// <param name="AccountId">The destination account identifier.</param>
/// <param name="Amount">The exact allocated amount.</param>
public sealed record IncomeAllocationDto(Guid AccountId, decimal Amount);
