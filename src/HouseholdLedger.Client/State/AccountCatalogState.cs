// <copyright file="AccountCatalogState.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.State;

/// <summary>Notifies client surfaces when the persisted account catalog changes.</summary>
public sealed class AccountCatalogState
{
    /// <summary>Occurs after a successful authoritative account-catalog refresh.</summary>
    public event Action? Changed;

    /// <summary>Notifies subscribers that the account catalog changed.</summary>
    public void NotifyChanged() => this.Changed?.Invoke();
}
