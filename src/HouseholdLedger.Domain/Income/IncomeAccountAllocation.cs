// <copyright file="IncomeAccountAllocation.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Income;

/// <summary>Represents the portion of an income receipt credited to an account.</summary>
public sealed class IncomeAccountAllocation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncomeAccountAllocation"/> class.
    /// </summary>
    /// <param name="accountId">The existing account receiving the allocation.</param>
    /// <param name="amount">The positive allocated amount.</param>
    public IncomeAccountAllocation(Guid accountId, decimal amount)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("An allocation account is required.", nameof(accountId));
        }

        IncomeAmount.Validate(amount, nameof(amount));
        this.AccountId = accountId;
        this.Amount = amount;
    }

    /// <summary>Gets the account receiving the allocation.</summary>
    public Guid AccountId { get; }

    /// <summary>Gets the positive allocated amount.</summary>
    public decimal Amount { get; }
}
