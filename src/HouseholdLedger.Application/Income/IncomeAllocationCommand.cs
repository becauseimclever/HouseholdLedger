// <copyright file="IncomeAllocationCommand.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

/// <summary>Supplies one intended account allocation for a recurring income schedule.</summary>
/// <param name="AccountId">The destination account identifier.</param>
/// <param name="Amount">The positive amount allocated to the account.</param>
public sealed record IncomeAllocationCommand(Guid AccountId, decimal Amount);
