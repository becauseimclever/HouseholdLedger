// <copyright file="AccountTransactionHistoryResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Represents one account and its complete transaction history.</summary>
/// <param name="Account">The selected account.</param>
/// <param name="Transactions">Every transaction owned by the account in newest-first order.</param>
public sealed record AccountTransactionHistoryResponse(
    AccountResponse Account,
    IReadOnlyList<ExpenseTransactionResponse> Transactions);
