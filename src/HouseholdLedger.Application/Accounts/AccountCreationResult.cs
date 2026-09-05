// <copyright file="AccountCreationResult.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Accounts;

/// <summary>Describes the outcome of creating an account.</summary>
/// <param name="Account">The created account, or <see langword="null"/> for a duplicate name.</param>
public sealed record AccountCreationResult(AccountDto? Account)
{
    /// <summary>Gets a value indicating whether the account was created.</summary>
    public bool IsCreated => this.Account is not null;
}
