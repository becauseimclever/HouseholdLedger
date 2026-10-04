// <copyright file="MonthlyBudgetPlanRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using HouseholdLedger.Application.Planning;
using HouseholdLedger.Domain.Planning;
using Microsoft.EntityFrameworkCore;

/// <summary>Persists monthly budget intentions.</summary>
public sealed class MonthlyBudgetPlanRepository(HouseholdLedgerDbContext dbContext) : IMonthlyBudgetPlanRepository
{
    /// <inheritdoc/>
    public async Task<MonthlyBudgetPlan?> FindAsync(DateOnly month, CancellationToken cancellationToken)
    {
        var record = await dbContext.MonthlyBudgetPlanRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(plan => plan.Month == month, cancellationToken);
        return record is null ? null : Rehydrate(record);
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(MonthlyBudgetPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var record = await dbContext.MonthlyBudgetPlanRecords
            .SingleOrDefaultAsync(item => item.Month == plan.Month, cancellationToken);
        if (record is null)
        {
            dbContext.MonthlyBudgetPlanRecords.Add(Map(plan));
        }
        else
        {
            record.ExpectedIncome = plan.ExpectedIncome;
            record.IntendedSavings = plan.IntendedSavings;
            record.Necessities = plan.Necessities;
            record.Optional = plan.Optional;
            record.Culture = plan.Culture;
            record.Unexpected = plan.Unexpected;
            record.LastRevisedAt = plan.LastRevisedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MonthlyBudgetPlanRecord Map(MonthlyBudgetPlan plan) => new()
    {
        Month = plan.Month,
        ExpectedIncome = plan.ExpectedIncome,
        IntendedSavings = plan.IntendedSavings,
        Necessities = plan.Necessities,
        Optional = plan.Optional,
        Culture = plan.Culture,
        Unexpected = plan.Unexpected,
        LastRevisedAt = plan.LastRevisedAt,
    };

    private static MonthlyBudgetPlan Rehydrate(MonthlyBudgetPlanRecord record) => new(
        record.Month,
        record.ExpectedIncome,
        record.IntendedSavings,
        record.Necessities,
        record.Optional,
        record.Culture,
        record.Unexpected,
        record.LastRevisedAt);
}
