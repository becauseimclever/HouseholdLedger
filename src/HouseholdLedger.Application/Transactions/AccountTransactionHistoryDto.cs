// <copyright file="AccountTransactionHistoryDto.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Transactions;

/// <summary>Carries one account and its complete transaction history.</summary>
public sealed record AccountTransactionHistoryDto(
    Guid AccountId,
    string AccountName,
    IReadOnlyList<ExpenseTransactionDto> Transactions);
