// <copyright file="ThemeOption.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.State;

/// <summary>Describes a selectable workbench theme.</summary>
/// <param name="Identifier">The stable theme identifier.</param>
/// <param name="DisplayName">The user-facing theme name.</param>
public sealed record ThemeOption(string Identifier, string DisplayName);
