// <copyright file="ResumePayScheduleRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Requests resumption of a pay schedule from a date.</summary>
/// <param name="ResumeDate">The date from which future receipts are eligible.</param>
public sealed record ResumePayScheduleRequest(DateOnly ResumeDate);
