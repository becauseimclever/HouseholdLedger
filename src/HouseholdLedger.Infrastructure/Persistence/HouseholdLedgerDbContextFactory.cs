// <copyright file="HouseholdLedgerDbContextFactory.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>Creates the persistence context for Entity Framework Core design-time tooling.</summary>
public sealed class HouseholdLedgerDbContextFactory : IDesignTimeDbContextFactory<HouseholdLedgerDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__HouseholdLedger";

    /// <inheritdoc/>
    public HouseholdLedgerDbContext CreateDbContext(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} to create the design-time context.");
        }

        var options = new DbContextOptionsBuilder<HouseholdLedgerDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new HouseholdLedgerDbContext(options);
    }
}
