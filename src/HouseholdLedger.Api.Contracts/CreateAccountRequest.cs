// <copyright file="CreateAccountRequest.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Contracts;

/// <summary>Requests creation of one named account.</summary>
/// <param name="Name">The account display name.</param>
public sealed record CreateAccountRequest(string Name);
