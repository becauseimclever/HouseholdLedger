// <copyright file="UpdateThemeRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Supplies an update to the global workbench theme.</summary>
/// <param name="Theme">The supported workbench theme identifier.</param>
public sealed record UpdateThemeRequest(string? Theme);
