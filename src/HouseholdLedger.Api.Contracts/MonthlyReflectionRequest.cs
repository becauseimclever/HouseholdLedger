// <copyright file="MonthlyReflectionRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Replaces a standalone monthly reflection; both fields are optional.</summary>
/// <param name="WhatWorked">Optional observations, at most 1000 characters after trimming.</param>
/// <param name="NextMonthIntention">Optional next-month intention, at most 1000 characters after trimming.</param>
public sealed record MonthlyReflectionRequest(string? WhatWorked = null, string? NextMonthIntention = null);
