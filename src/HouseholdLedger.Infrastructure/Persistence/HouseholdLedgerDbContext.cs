// <copyright file="HouseholdLedgerDbContext.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Represents the HouseholdLedger PostgreSQL persistence boundary.
/// </summary>
/// <param name="options">The context configuration supplied by the composition root.</param>
public sealed class HouseholdLedgerDbContext(DbContextOptions<HouseholdLedgerDbContext> options)
    : DbContext(options)
{
}
