// <copyright file="DisplayCurrency.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Settings;

/// <summary>Identifies a supported named currency used for monetary presentation.</summary>
public enum DisplayCurrency
{
    /// <summary>US Dollar.</summary>
    USD,

    /// <summary>Canadian Dollar.</summary>
    CAD,

    /// <summary>Euro.</summary>
    EUR,

    /// <summary>Pound Sterling.</summary>
    GBP,

    /// <summary>Australian Dollar.</summary>
    AUD,

    /// <summary>No named currency.</summary>
    XXX,
}
