// <copyright file="MonthlyBudgetPlanRepositoryTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.IntegrationTests;

using HouseholdLedger.Application.Income;
using HouseholdLedger.Application.Planning;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Planning;
using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

/// <summary>Verifies monthly intention persistence against an isolated PostgreSQL database.</summary>
[Collection(PostgreSqlConnectivityTests.PostgreSqlCollectionDefinition.Name)]
public sealed class MonthlyBudgetPlanRepositoryTests
{
    private const string ConnectionStringEnvironmentVariable =
        "HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING";

    /// <summary>Reads a monthly review through repositories sharing one scoped database context.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReviewsMonthUsingSharedDatabaseContext()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Skip($"Set {ConnectionStringEnvironmentVariable} to an isolated PostgreSQL database.");
        }

        var services = new ServiceCollection();
        services.AddHouseholdLedgerInfrastructure(connectionString);
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HouseholdLedgerDbContext>();
        var cancellationToken = TestContext.Current.CancellationToken;
        await context.Database.MigrateAsync(cancellationToken);
        var service = new MonthlyBudgetPlanService(
            scope.ServiceProvider.GetRequiredService<IMonthlyBudgetPlanRepository>(),
            scope.ServiceProvider.GetRequiredService<IExpenseTransactionRepository>(),
            scope.ServiceProvider.GetRequiredService<IIncomeScheduleRepository>(),
            TimeProvider.System);

        var review = await service.ReviewAsync(2026, 10, cancellationToken);

        Assert.Multiple(
            () => Assert.Null(review.ExpectedIncome),
            () => Assert.Equal(0m, review.ActualIncome),
            () => Assert.Equal(4, review.Classifications.Count));
    }

    /// <summary>Persists monthly plans and enforces exact reconciliation in PostgreSQL.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PersistsAndRevisesPlanWithDatabaseReconciliationConstraint()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Skip($"Set {ConnectionStringEnvironmentVariable} to an isolated PostgreSQL database.");
        }

        var services = new ServiceCollection();
        services.AddHouseholdLedgerInfrastructure(connectionString);

        await using var provider = services.BuildServiceProvider(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HouseholdLedgerDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IMonthlyBudgetPlanRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var month = new DateOnly(2026, 10, 1);
        var firstRevisionAt = DateTimeOffset.UtcNow;
        firstRevisionAt = firstRevisionAt.AddTicks(-(firstRevisionAt.Ticks % 10));
        var firstPlan = new MonthlyBudgetPlan(month, 1000m, 200m, 400m, 100m, 100m, 200m, firstRevisionAt);
        var revisedPlan = new MonthlyBudgetPlan(month, 1200m, 300m, 400m, 150m, 150m, 200m, firstRevisionAt.AddMinutes(1));

        await context.Database.MigrateAsync(cancellationToken);
        try
        {
            await repository.UpsertAsync(firstPlan, cancellationToken);
            var firstRead = await repository.FindAsync(month, cancellationToken);
            await repository.UpsertAsync(revisedPlan, cancellationToken);
            var revisedRead = await repository.FindAsync(month, cancellationToken);
            var reconciliationException = await Assert.ThrowsAsync<PostgresException>(
                () => context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE monthly_budget_plans SET necessities = necessities + 1 WHERE month = {month}",
                    cancellationToken));

            Assert.Multiple(
                () => Assert.Equal(firstPlan.ExpectedIncome, firstRead?.ExpectedIncome),
                () => Assert.Equal(firstPlan.LastRevisedAt, firstRead?.LastRevisedAt),
                () => Assert.Equal(revisedPlan.ExpectedIncome, revisedRead?.ExpectedIncome),
                () => Assert.Equal(revisedPlan.IntendedSavings, revisedRead?.IntendedSavings),
                () => Assert.Equal(revisedPlan.LastRevisedAt, revisedRead?.LastRevisedAt),
                () => Assert.Equal(PostgresErrorCodes.CheckViolation, reconciliationException.SqlState),
                () => Assert.Equal("ck_monthly_budget_plans_reconciled", reconciliationException.ConstraintName));
        }
        finally
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM monthly_budget_plans WHERE month = {month}",
                cancellationToken);
        }
    }
}
