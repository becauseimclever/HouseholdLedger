// <copyright file="UpdateGlobalSettingsRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Supplies an update to the global application settings.</summary>
/// <param name="DisplayCurrency">The supported ISO 4217 display-currency code.</param>
public sealed record UpdateGlobalSettingsRequest(string? DisplayCurrency);
