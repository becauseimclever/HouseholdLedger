// <copyright file="UpdateExpenseTransactionRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>
/// Requests correction of one expense without changing its ledger date.
/// </summary>
/// <param name="AccountId">The replacement owning account identifier.</param>
/// <param name="Amount">The replacement positive USD amount.</param>
/// <param name="Classification">The replacement Kakeibo-inspired classification.</param>
public sealed record UpdateExpenseTransactionRequest(Guid AccountId, decimal Amount, string Classification);
