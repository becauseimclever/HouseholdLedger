// <copyright file="ExpenseTransactionResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>
/// Describes one persisted USD expense.
/// </summary>
/// <param name="Id">The opaque transaction identifier.</param>
/// <param name="AccountId">The owning account identifier.</param>
/// <param name="AccountName">The owning account name.</param>
/// <param name="Date">The ledger date.</param>
/// <param name="Amount">The positive USD amount.</param>
/// <param name="Classification">The Kakeibo-inspired classification.</param>
public sealed record ExpenseTransactionResponse(
    Guid Id,
    Guid AccountId,
    string AccountName,
    DateOnly Date,
    decimal Amount,
    string Classification);
