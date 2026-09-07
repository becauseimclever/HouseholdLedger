// <copyright file="DisplayCurrencyOption.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Formatting;

/// <summary>Describes a supported display currency.</summary>
/// <param name="Code">The stable ISO 4217 code.</param>
/// <param name="DisplayName">The user-facing name.</param>
/// <param name="CultureName">The culture used for formatting.</param>
public sealed record DisplayCurrencyOption(string Code, string DisplayName, string CultureName);
