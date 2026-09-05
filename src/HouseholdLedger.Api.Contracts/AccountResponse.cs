// <copyright file="AccountResponse.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Represents one persisted account.</summary>
/// <param name="Id">The account identifier.</param>
/// <param name="Name">The account display name.</param>
public sealed record AccountResponse(Guid Id, string Name);
