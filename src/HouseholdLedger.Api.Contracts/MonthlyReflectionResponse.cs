// <copyright file="MonthlyReflectionResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Describes a saved standalone monthly reflection.</summary>
/// <param name="WhatWorked">Optional normalized observations about what worked.</param>
/// <param name="NextMonthIntention">The optional normalized next-month intention.</param>
/// <param name="LastRevisedAt">The last save time.</param>
public sealed record MonthlyReflectionResponse(string? WhatWorked, string? NextMonthIntention, DateTimeOffset LastRevisedAt);
