// <copyright file="HealthResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>
/// Describes API service availability.
/// </summary>
/// <param name="Status">The stable availability status.</param>
public sealed record HealthResponse(string Status);
