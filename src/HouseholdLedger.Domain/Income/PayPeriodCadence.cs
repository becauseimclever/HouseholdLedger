// <copyright file="PayPeriodCadence.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Income;

/// <summary>Defines the recurrence cadence for a pay schedule.</summary>
public enum PayPeriodCadence
{
    /// <summary>Every week.</summary>
    Weekly = 0,

    /// <summary>Every 14 days.</summary>
    Biweekly = 1,

    /// <summary>Twice each month on configured dates.</summary>
    Semimonthly = 2,

    /// <summary>Every 28 days.</summary>
    FourWeekly = 3,

    /// <summary>Once each month on a configured date.</summary>
    Monthly = 4,
}
