// <copyright file="TransactionMutationResult.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

/// <summary>
/// Describes the expected outcome of a transaction mutation request.
/// </summary>
public enum TransactionMutationResult
{
    /// <summary>The mutation completed.</summary>
    Success,

    /// <summary>The submitted values were invalid.</summary>
    Invalid,

    /// <summary>The date-scoped transaction no longer exists.</summary>
    NotFound,
}
