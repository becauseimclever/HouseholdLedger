// <copyright file="DatabaseReadinessCheck.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.Hosting;

using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>Checks database reachability and applied schema without migrating it.</summary>
internal sealed class DatabaseReadinessCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetService<HouseholdLedgerDbContext>();
        if (database is null || !await database.Database.CanConnectAsync(cancellationToken))
        {
            return HealthCheckResult.Unhealthy("Database unavailable.");
        }

        var pending = await database.Database.GetPendingMigrationsAsync(cancellationToken);
        return pending.Any()
            ? HealthCheckResult.Unhealthy("Database schema requires migration.")
            : HealthCheckResult.Healthy();
    }
}
