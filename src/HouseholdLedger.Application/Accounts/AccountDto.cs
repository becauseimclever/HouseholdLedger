// <copyright file="AccountDto.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Accounts;

/// <summary>Represents an account returned by an Application use case.</summary>
/// <param name="Id">The account identifier.</param>
/// <param name="Name">The account display name.</param>
public sealed record AccountDto(Guid Id, string Name);
