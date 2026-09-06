// <copyright file="ExpenseTransactionDto.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Transactions;

using HouseholdLedger.Domain.Transactions;

/// <summary>
/// Carries one transaction from an Application use case.
/// </summary>
public sealed record ExpenseTransactionDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    DateOnly Date,
    decimal Amount,
    ExpenseClassification Classification);
