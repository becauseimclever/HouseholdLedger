// <copyright file="AccountTransactionFilterRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Represents optional filters for one account's transaction history.</summary>
public sealed class AccountTransactionFilterRequest
{
    /// <summary>Gets or initializes the inclusive first ledger date.</summary>
    public DateOnly? From { get; init; }

    /// <summary>Gets or initializes the inclusive final ledger date.</summary>
    public DateOnly? To { get; init; }

    /// <summary>Gets or initializes the required expense classification.</summary>
    public string? Classification { get; init; }

    /// <summary>Gets or initializes the inclusive minimum stored amount.</summary>
    public decimal? MinimumAmount { get; init; }

    /// <summary>Gets or initializes the inclusive maximum stored amount.</summary>
    public decimal? MaximumAmount { get; init; }

    /// <summary>Gets or initializes text matched against visible transaction fields.</summary>
    public string? Search { get; init; }
}
