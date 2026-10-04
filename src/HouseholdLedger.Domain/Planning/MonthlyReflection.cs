// <copyright file="MonthlyReflection.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Planning;

/// <summary>Represents an optional monthly reflection independent of a budget plan.</summary>
public sealed class MonthlyReflection
{
    /// <summary>Initializes a new instance of the <see cref="MonthlyReflection"/> class.</summary>
    /// <param name="month">The first calendar day of the reflected month.</param>
    /// <param name="whatWorked">Optional observations about what worked.</param>
    /// <param name="nextMonthIntention">Optional intention for the next month.</param>
    /// <param name="lastRevisedAt">The last save time.</param>
    public MonthlyReflection(DateOnly month, string? whatWorked, string? nextMonthIntention, DateTimeOffset lastRevisedAt)
    {
        if (month.Day != 1)
        {
            throw new ArgumentException("Use the first day of the calendar month.", nameof(month));
        }

        this.Month = month;
        this.WhatWorked = Normalize(whatWorked, nameof(whatWorked));
        this.NextMonthIntention = Normalize(nextMonthIntention, nameof(nextMonthIntention));
        this.LastRevisedAt = lastRevisedAt;
    }

    /// <summary>Gets the reflected calendar month.</summary>
    public DateOnly Month { get; }

    /// <summary>Gets optional observations about what worked.</summary>
    public string? WhatWorked { get; }

    /// <summary>Gets the optional next-month intention.</summary>
    public string? NextMonthIntention { get; }

    /// <summary>Gets the last save time.</summary>
    public DateTimeOffset LastRevisedAt { get; }

    private static string? Normalize(string? value, string parameterName)
    {
        var normalized = value?.Trim();
        if (normalized?.Length > 1000)
        {
            throw new ArgumentException("Reflection text cannot exceed 1000 characters.", parameterName);
        }

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
