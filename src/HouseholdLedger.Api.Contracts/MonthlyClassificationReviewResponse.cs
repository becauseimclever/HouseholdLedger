// <copyright file="MonthlyClassificationReviewResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Compares planned spending to recorded spending for a classification.</summary>
/// <param name="Classification">The existing Kakeibo-inspired classification name.</param>
/// <param name="PlannedAmount">The amount planned, or zero when no plan exists.</param>
/// <param name="ActualAmount">The amount of persisted expenses.</param>
/// <param name="Variance">Actual minus planned spending.</param>
public sealed record MonthlyClassificationReviewResponse(
    string Classification,
    decimal PlannedAmount,
    decimal ActualAmount,
    decimal Variance);
