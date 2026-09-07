// <copyright file="GlobalSettingsResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Represents the authoritative global application settings.</summary>
/// <param name="DisplayCurrency">The supported ISO 4217 display-currency code.</param>
/// <param name="Theme">The supported workbench theme identifier.</param>
public sealed record GlobalSettingsResponse(string DisplayCurrency, string Theme);
