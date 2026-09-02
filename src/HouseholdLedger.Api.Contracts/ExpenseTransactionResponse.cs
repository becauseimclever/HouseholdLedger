// <copyright file="ExpenseTransactionResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>
/// Describes one persisted USD expense.
/// </summary>
/// <param name="Id">The opaque transaction identifier.</param>
/// <param name="Date">The ledger date.</param>
/// <param name="Amount">The positive USD amount.</param>
/// <param name="Classification">The Kakeibo-inspired classification.</param>
public sealed record ExpenseTransactionResponse(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    string Classification);
