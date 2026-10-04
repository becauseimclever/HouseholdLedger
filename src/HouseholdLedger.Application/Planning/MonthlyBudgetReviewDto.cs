// <copyright file="MonthlyBudgetReviewDto.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Planning;

/// <summary>Summarizes a month's intention and authoritative actuals.</summary>
public sealed record MonthlyBudgetReviewDto(
    decimal? ExpectedIncome,
    decimal? IntendedSavings,
    decimal ActualIncome,
    decimal? IncomeVariance,
    decimal UnallocatedRemainder,
    DateTimeOffset? LastRevisedAt,
    IReadOnlyList<MonthlyClassificationReviewDto> Classifications,
    decimal CashflowDifference = 0m);
