// <copyright file="MonthlyClassificationReviewDto.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Planning;

using HouseholdLedger.Domain.Transactions;

/// <summary>Compares planned and recorded spending for one expense classification.</summary>
public sealed record MonthlyClassificationReviewDto(
    ExpenseClassification Classification,
    decimal PlannedAmount,
    decimal ActualAmount,
    decimal Variance);
