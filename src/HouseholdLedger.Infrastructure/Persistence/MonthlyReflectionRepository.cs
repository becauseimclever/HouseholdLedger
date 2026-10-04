// <copyright file="MonthlyReflectionRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using HouseholdLedger.Application.Planning;
using HouseholdLedger.Domain.Planning;
using Microsoft.EntityFrameworkCore;

/// <summary>Persists standalone monthly reflections.</summary>
public sealed class MonthlyReflectionRepository(HouseholdLedgerDbContext dbContext) : IMonthlyReflectionRepository
{
    /// <inheritdoc/>
    public async Task<MonthlyReflection?> FindAsync(DateOnly month, CancellationToken cancellationToken)
    {
        var record = await dbContext.MonthlyReflectionRecords.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Month == month, cancellationToken);
        return record is null ? null : new MonthlyReflection(record.Month, record.WhatWorked, record.NextMonthIntention, record.LastRevisedAt);
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(MonthlyReflection reflection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reflection);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO monthly_reflections (month, what_worked, next_month_intention, last_revised_at)
            VALUES ({reflection.Month}, {reflection.WhatWorked}, {reflection.NextMonthIntention}, {reflection.LastRevisedAt})
            ON CONFLICT (month) DO UPDATE SET
                what_worked = EXCLUDED.what_worked,
                next_month_intention = EXCLUDED.next_month_intention,
                last_revised_at = EXCLUDED.last_revised_at
            """,
            cancellationToken);
    }
}
