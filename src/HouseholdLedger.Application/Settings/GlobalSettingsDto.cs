// <copyright file="GlobalSettingsDto.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Settings;

using HouseholdLedger.Domain.Settings;

/// <summary>Represents the authoritative global application settings.</summary>
/// <param name="DisplayCurrency">The currency used for monetary presentation.</param>
/// <param name="Theme">The application-wide workbench theme.</param>
public sealed record GlobalSettingsDto(DisplayCurrency DisplayCurrency, WorkbenchTheme Theme);
