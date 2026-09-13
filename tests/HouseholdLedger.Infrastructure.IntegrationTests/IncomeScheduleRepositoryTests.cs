// <copyright file="IncomeScheduleRepositoryTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.IntegrationTests;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Application.Income;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Income;
using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

/// <summary>Verifies income persistence against an isolated PostgreSQL database.</summary>
[Collection(PostgreSqlConnectivityTests.PostgreSqlCollectionDefinition.Name)]
public sealed class IncomeScheduleRepositoryTests
{
    private const string ConnectionStringEnvironmentVariable =
        "HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING";

    /// <summary>Persists schedules and receipts with their immutable allocation snapshots.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PersistsSchedulesAndReceiptsWithDatabaseIntegrityConstraints()
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
        var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var repository = scope.ServiceProvider.GetRequiredService<IIncomeScheduleRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var firstAccount = new Account(Guid.NewGuid(), $"Income checking {Guid.NewGuid():N}");
        var secondAccount = new Account(Guid.NewGuid(), $"Income savings {Guid.NewGuid():N}");
        var scheduleId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var payDate = new DateOnly(2026, 9, 15);
        var schedule = new PaySchedule(
            scheduleId,
            "Primary salary",
            payDate,
            PayPeriodCadence.Semimonthly,
            1000.00m,
            [new IncomeAccountAllocation(firstAccount.Id, 700.00m), new IncomeAccountAllocation(secondAccount.Id, 300.00m)],
            30);
        schedule.Pause();
        schedule.Resume(new DateOnly(2026, 10, 1));
        var receipt = new IncomeReceipt(
            receiptId,
            scheduleId,
            payDate,
            1000.00m,
            [new IncomeAccountAllocation(firstAccount.Id, 700.00m), new IncomeAccountAllocation(secondAccount.Id, 300.00m)]);

        await context.Database.MigrateAsync(cancellationToken);
        try
        {
            Assert.True(await accountRepository.TryAddAsync(firstAccount, cancellationToken));
            Assert.True(await accountRepository.TryAddAsync(secondAccount, cancellationToken));
            await repository.AddScheduleAsync(schedule, cancellationToken);
            var found = await repository.FindScheduleAsync(scheduleId, cancellationToken);
            schedule.Pause();
            await repository.UpdateScheduleAsync(schedule, cancellationToken);
            var paused = await repository.FindScheduleAsync(scheduleId, cancellationToken);
            var schedules = await repository.ListSchedulesAsync(cancellationToken);
            var storedReceipt = await repository.GetOrAddReceiptAsync(receipt, cancellationToken);
            var firstReceipt = new IncomeReceipt(
                Guid.NewGuid(),
                scheduleId,
                new DateOnly(2026, 9, 1),
                900.00m,
                [new IncomeAccountAllocation(firstAccount.Id, 600.00m), new IncomeAccountAllocation(secondAccount.Id, 300.00m)]);
            var finalReceipt = new IncomeReceipt(
                Guid.NewGuid(),
                scheduleId,
                new DateOnly(2026, 9, 30),
                1100.00m,
                [new IncomeAccountAllocation(firstAccount.Id, 800.00m), new IncomeAccountAllocation(secondAccount.Id, 300.00m)]);
            var outsideReceipt = new IncomeReceipt(
                Guid.NewGuid(),
                scheduleId,
                new DateOnly(2026, 10, 1),
                1200.00m,
                [new IncomeAccountAllocation(firstAccount.Id, 900.00m), new IncomeAccountAllocation(secondAccount.Id, 300.00m)]);
            await repository.GetOrAddReceiptAsync(firstReceipt, cancellationToken);
            await repository.GetOrAddReceiptAsync(finalReceipt, cancellationToken);
            await repository.GetOrAddReceiptAsync(outsideReceipt, cancellationToken);
            var receipts = await repository.ListReceiptsAsync(
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 9, 30),
                cancellationToken);
            var duplicateReceipt = await repository.GetOrAddReceiptAsync(
                new IncomeReceipt(Guid.NewGuid(), scheduleId, payDate, 1000.00m, receipt.Allocations),
                cancellationToken);
            var duplicateAllocationException = await Assert.ThrowsAsync<PostgresException>(
                () => context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO income_schedule_allocations (schedule_id, account_id, amount) VALUES ({scheduleId}, {firstAccount.Id}, {1m})",
                    cancellationToken));
            var invalidCadenceException = await Assert.ThrowsAsync<PostgresException>(
                () => context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE pay_schedules SET cadence = 'Unsupported' WHERE id = {scheduleId}",
                    cancellationToken));

            Assert.Multiple(
                () => Assert.Equal("Primary salary", found?.Name),
                () => Assert.Equal(PayPeriodCadence.Semimonthly, found?.Cadence),
                () => Assert.Equal(30, found?.SecondMonthlyPayDay),
                () => Assert.False(found?.IsPaused),
                () => Assert.Equal(new DateOnly(2026, 10, 1), found?.ReceiptEligibleFrom),
                () => Assert.Equal(1000.00m, found?.NetIncome),
                () => Assert.Equal(2, found?.Allocations.Count),
                () => Assert.True(paused?.IsPaused),
                () => Assert.Equal(new DateOnly(2026, 10, 1), paused?.ReceiptEligibleFrom),
                () => Assert.Single(schedules),
                () => Assert.Equal(receiptId, storedReceipt?.Id),
                () => Assert.Null(duplicateReceipt),
                () => Assert.Collection(
                    receipts,
                    item => Assert.Equal(firstReceipt.Id, item.Id),
                    item => Assert.Equal(receiptId, item.Id),
                    item => Assert.Equal(finalReceipt.Id, item.Id)),
                () => Assert.Contains(
                    receipts[0].Allocations,
                    allocation => allocation.AccountId == firstAccount.Id && allocation.Amount == 600.00m),
                () => Assert.Contains(
                    receipts[0].Allocations,
                    allocation => allocation.AccountId == secondAccount.Id && allocation.Amount == 300.00m),
                () => Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicateAllocationException.SqlState),
                () => Assert.Equal("ux_income_schedule_allocations_schedule_id_account_id", duplicateAllocationException.ConstraintName),
                () => Assert.Equal(PostgresErrorCodes.CheckViolation, invalidCadenceException.SqlState),
                () => Assert.Equal("ck_pay_schedules_cadence", invalidCadenceException.ConstraintName));
        }
        finally
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM income_receipts WHERE schedule_id = {scheduleId}",
                cancellationToken);
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM pay_schedules WHERE id = {scheduleId}",
                cancellationToken);
            await context.Accounts
                .Where(account => account.Id == firstAccount.Id || account.Id == secondAccount.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
