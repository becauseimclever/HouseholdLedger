// <copyright file="CreateExpenseTransactionRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>
/// Requests creation of one expense for a route-selected date.
/// </summary>
/// <param name="AccountId">The owning account identifier.</param>
/// <param name="Amount">The positive USD amount.</param>
/// <param name="Classification">The Kakeibo-inspired classification.</param>
public sealed record CreateExpenseTransactionRequest(Guid AccountId, decimal Amount, string Classification);
