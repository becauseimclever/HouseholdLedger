// <copyright file="AuthenticationMode.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Hosting;

/// <summary>Describes whether the host requires external owner authentication.</summary>
/// <param name="Enabled">Whether authentication is enabled.</param>
public sealed record AuthenticationMode(bool Enabled);
