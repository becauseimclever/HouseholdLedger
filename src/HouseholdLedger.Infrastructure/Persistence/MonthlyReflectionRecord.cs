// <copyright file="MonthlyReflectionRecord.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

internal sealed class MonthlyReflectionRecord
{
    public DateOnly Month { get; set; }

    public string? WhatWorked { get; set; }

    public string? NextMonthIntention { get; set; }

    public DateTimeOffset LastRevisedAt { get; set; }
}
