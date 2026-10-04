// <copyright file="MonthlyBudgetReviewResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Summarizes saved monthly intentions and authoritative ledger actuals.</summary>
/// <param name="ExpectedIncome">Expected income, or null when no plan exists.</param>
/// <param name="IntendedSavings">Intended savings, or null when no plan exists.</param>
/// <param name="ActualIncome">Income explicitly confirmed as received.</param>
/// <param name="IncomeVariance">Actual minus expected income, or null when no plan exists.</param>
/// <param name="UnallocatedRemainder">Positive confirmed income remaining after recorded expenses.</param>
/// <param name="LastRevisedAt">When the plan was last saved, or null when no plan exists.</param>
/// <param name="Classifications">Planned and actual spending comparisons.</param>
/// <param name="CashflowDifference">Signed confirmed income minus recorded expenses; not a balance or savings claim.</param>
public sealed record MonthlyBudgetReviewResponse(
    decimal? ExpectedIncome,
    decimal? IntendedSavings,
    decimal ActualIncome,
    decimal? IncomeVariance,
    decimal UnallocatedRemainder,
    DateTimeOffset? LastRevisedAt,
    IReadOnlyList<MonthlyClassificationReviewResponse> Classifications,
    decimal CashflowDifference = 0m);
