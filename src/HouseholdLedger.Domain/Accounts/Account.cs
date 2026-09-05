// <copyright file="Account.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Accounts;

/// <summary>
/// Identifies an account that owns ledger transactions.
/// </summary>
public sealed class Account
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Account"/> class.
    /// </summary>
    /// <param name="id">The backend-generated account identifier.</param>
    /// <param name="name">The account display name.</param>
    public Account(Guid id, string name)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An account identifier is required.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmedName = name.Trim();
        if (trimmedName.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "The account name cannot exceed 100 characters.");
        }

        this.Id = id;
        this.Name = trimmedName;
        this.NormalizedName = trimmedName.ToUpperInvariant();
    }

    /// <summary>Gets the account identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the trimmed account display name.</summary>
    public string Name { get; }

    /// <summary>Gets the invariant normalized name used for durable uniqueness.</summary>
    public string NormalizedName { get; }
}
