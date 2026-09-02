// <copyright file="ExpenseClassification.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Transactions;

/// <summary>
/// Classifies an expense using the initial Kakeibo-inspired vocabulary.
/// </summary>
public enum ExpenseClassification
{
    /// <summary>Essential household spending.</summary>
    Necessities,

    /// <summary>Discretionary household spending.</summary>
    Optional,

    /// <summary>Cultural and learning spending.</summary>
    Culture,

    /// <summary>Unplanned household spending.</summary>
    Unexpected,
}
