// <copyright file="AccountCreationResult.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

/// <summary>Describes an account creation HTTP outcome.</summary>
public enum AccountCreationResult
{
    /// <summary>The account was created.</summary>
    Success,

    /// <summary>The account name was rejected.</summary>
    Invalid,
}
