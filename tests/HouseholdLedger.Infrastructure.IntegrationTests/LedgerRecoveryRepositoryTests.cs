// <copyright file="LedgerRecoveryRepositoryTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.IntegrationTests;

using HouseholdLedger.Application.Income;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Income;
using HouseholdLedger.Domain.Planning;
using HouseholdLedger.Domain.Transactions;
using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

/// <summary>Verifies recovery persistence only in a newly created disposable PostgreSQL database.</summary>
[Collection(PostgreSqlConnectivityTests.PostgreSqlCollectionDefinition.Name)]
public sealed class LedgerRecoveryRepositoryTests
{
    /// <summary>Tests concurrent token retries, durable tombstones, corrections, descriptions and standalone reflections.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task DisposableDatabasePreservesRetryAndRecoveryIntegrity()
    {
        var supplied = Environment.GetEnvironmentVariable("HOUSEHOLDLEDGER_TEST_POSTGRES_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(supplied))
        {
            Assert.Skip("Supply an explicit PostgreSQL test connection with permission to create a disposable database.");
        }

        var databaseName = $"ledger_recovery_test_{Guid.NewGuid():N}";
        var builder = new NpgsqlConnectionStringBuilder(supplied) { Database = databaseName, Pooling = false };
        await using var admin = new NpgsqlConnection(supplied);
        var cancellationToken = TestContext.Current.CancellationToken;
        await admin.OpenAsync(cancellationToken);
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin))
        {
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        try
        {
            var options = new DbContextOptionsBuilder<HouseholdLedgerDbContext>().UseNpgsql(builder.ConnectionString).Options;
            var checking = new Account(Guid.NewGuid(), "Checking");
            var savings = new Account(Guid.NewGuid(), "Savings");
            var cash = new Account(Guid.NewGuid(), "Cash");
            var legacyReceipt = new IncomeReceipt(
                Guid.NewGuid(),
                null,
                new DateOnly(2026, 8, 15),
                75m,
                [new IncomeAccountAllocation(checking.Id, 75m)]);
            await using (var setup = new HouseholdLedgerDbContext(options))
            {
                await setup.GetService<IMigrator>().MigrateAsync("20261004044748_AddMonthlyBudgetPlansAndConfirmedIncome", cancellationToken);
                setup.Accounts.AddRange(checking, savings, cash);
                await setup.SaveChangesAsync(cancellationToken);
                await new IncomeScheduleRepository(setup).AddReceiptAsync(legacyReceipt, cancellationToken);
                await setup.Database.MigrateAsync(cancellationToken);
                Assert.False(setup.Database.HasPendingModelChanges());
                var preserved = (await new IncomeScheduleRepository(setup).FindReceiptAsync(legacyReceipt.Id, cancellationToken))!;
                Assert.Equal(75m, preserved.NetIncome);
                Assert.Equal(checking.Id, Assert.Single(preserved.Allocations).AccountId);
            }

            var requestId = Guid.NewGuid();
            var date = new DateOnly(2026, 9, 15);
            var receipts = await Task.WhenAll(Enumerable.Range(0, 12).Select(async _ =>
            {
                await using var context = new HouseholdLedgerDbContext(options);
                var repository = new IncomeScheduleRepository(context);
                return await repository.ConfirmReceiptAsync(
                    new IncomeReceipt(
                        Guid.NewGuid(),
                        null,
                        date,
                        100m,
                        [new IncomeAccountAllocation(checking.Id, 70m), new IncomeAccountAllocation(savings.Id, 30m)]),
                    requestId,
                    cancellationToken);
            }));
            var receiptId = Assert.Single(receipts.Select(item => item.Id).Distinct());
            await using var verification = new HouseholdLedgerDbContext(options);
            var incomeRepository = new IncomeScheduleRepository(verification);
            Assert.Single(await incomeRepository.ListReceiptsAsync(date, date, cancellationToken));
            var reversed = new IncomeReceipt(
                Guid.NewGuid(),
                null,
                date,
                100.00m,
                [new IncomeAccountAllocation(savings.Id, 30.00m), new IncomeAccountAllocation(checking.Id, 70.00m)]);
            Assert.Equal(receiptId, (await incomeRepository.ConfirmReceiptAsync(reversed, requestId, cancellationToken)).Id);
            await Assert.ThrowsAsync<IncomeReceiptRequestConflictException>(() => incomeRepository.ConfirmReceiptAsync(
                new IncomeReceipt(Guid.NewGuid(), null, date, 101m, [new IncomeAccountAllocation(checking.Id, 101m)]),
                requestId,
                cancellationToken));
            var conflictingToken = Guid.NewGuid();
            var conflictingDate = new DateOnly(2026, 7, 15);
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var conflictingRetries = Enumerable.Range(0, 12).Select(async index =>
            {
                await gate.Task;
                await using var context = new HouseholdLedgerDbContext(options);
                var amount = index % 2 == 0 ? 200m : 201m;
                try
                {
                    var accepted = await new IncomeScheduleRepository(context).ConfirmReceiptAsync(
                        new IncomeReceipt(Guid.NewGuid(), null, conflictingDate, amount, [new IncomeAccountAllocation(checking.Id, amount)]),
                        conflictingToken,
                        cancellationToken);
                    return (Receipt: accepted, Conflict: false);
                }
                catch (IncomeReceiptRequestConflictException)
                {
                    return (Receipt: (IncomeReceipt?)null, Conflict: true);
                }
            }).ToArray();
            gate.SetResult();
            var conflictingResults = await Task.WhenAll(conflictingRetries);
            Assert.Equal(6, conflictingResults.Count(item => item.Conflict));
            var acceptedRetries = conflictingResults.Where(item => !item.Conflict).Select(item => item.Receipt!).ToArray();
            var conflictWinnerId = Assert.Single(acceptedRetries.Select(item => item.Id).Distinct());
            var conflictWinnerAmount = Assert.Single(acceptedRetries.Select(item => item.NetIncome).Distinct());
            Assert.Equal(conflictWinnerId, Assert.Single(await incomeRepository.ListReceiptsAsync(conflictingDate, conflictingDate, cancellationToken)).Id);
            Assert.True(conflictWinnerAmount is 200m or 201m);
            var separate = await incomeRepository.ConfirmReceiptAsync(reversed, Guid.NewGuid(), cancellationToken);
            Assert.NotEqual(receiptId, separate.Id);
            Assert.Equal(2, (await incomeRepository.ListReceiptsAsync(date, date, cancellationToken)).Count);
            var corrected = new IncomeReceipt(
                receiptId,
                null,
                date.AddDays(1),
                120m,
                [new IncomeAccountAllocation(savings.Id, 40m), new IncomeAccountAllocation(cash.Id, 80m)]);
            Assert.True(await incomeRepository.UpdateReceiptAsync(corrected, cancellationToken));
            var reloaded = (await incomeRepository.FindReceiptAsync(receiptId, cancellationToken))!;
            Assert.Equal(120m, reloaded.NetIncome);
            Assert.Equal(date.AddDays(1), reloaded.PayDate);
            Assert.Equal(2, reloaded.Allocations.Count);
            Assert.DoesNotContain(reloaded.Allocations, item => item.AccountId == checking.Id);
            Assert.Equal(40m, reloaded.Allocations.Single(item => item.AccountId == savings.Id).Amount);
            await using (var retryAfterUpdate = new HouseholdLedgerDbContext(options))
            {
                var retryRepository = new IncomeScheduleRepository(retryAfterUpdate);
                var replay = await retryRepository.ConfirmReceiptAsync(reversed, requestId, cancellationToken);
                Assert.Equal(receiptId, replay.Id);
                Assert.Equal(100m, replay.NetIncome);
                var stillCorrected = (await retryRepository.FindReceiptAsync(receiptId, cancellationToken))!;
                Assert.Equal(120m, stillCorrected.NetIncome);
                Assert.Equal(date.AddDays(1), stillCorrected.PayDate);
                Assert.DoesNotContain(stillCorrected.Allocations, item => item.AccountId == checking.Id);
            }

            Assert.True(await incomeRepository.DeleteReceiptAsync(receiptId, cancellationToken));
            await using (var retryAfterDelete = new HouseholdLedgerDbContext(options))
            {
                var retryRepository = new IncomeScheduleRepository(retryAfterDelete);
                Assert.Equal(receiptId, (await retryRepository.ConfirmReceiptAsync(reversed, requestId, cancellationToken)).Id);
                Assert.Null(await retryRepository.FindReceiptAsync(receiptId, cancellationToken));
                await Assert.ThrowsAsync<IncomeReceiptRequestConflictException>(() => retryRepository.ConfirmReceiptAsync(corrected, requestId, cancellationToken));
            }

            Assert.Null(await incomeRepository.FindReceiptAsync(receiptId, cancellationToken));
            Assert.False(await incomeRepository.DeleteReceiptAsync(receiptId, cancellationToken));
            Assert.Single(await incomeRepository.ListReceiptsAsync(date, date.AddDays(1), cancellationToken));

            var reflectionRepository = new MonthlyReflectionRepository(verification);
            var month = new DateOnly(2026, 9, 1);
            var savedAt = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
            await reflectionRepository.UpsertAsync(new MonthlyReflection(month, "Cooked", "Plan meals", savedAt), cancellationToken);
            var reflection = (await reflectionRepository.FindAsync(month, cancellationToken))!;
            Assert.Equal("Cooked", reflection.WhatWorked);
            Assert.Equal(savedAt, reflection.LastRevisedAt);
            await reflectionRepository.UpsertAsync(new MonthlyReflection(month, null, null, savedAt.AddMinutes(1)), cancellationToken);
            Assert.Null((await reflectionRepository.FindAsync(month, cancellationToken))!.WhatWorked);

            var expenseRepository = new ExpenseTransactionRepository(verification);
            var expense = new ExpenseTransaction(Guid.NewGuid(), checking.Id, date, 10m, ExpenseClassification.Necessities, description: "  100% groceries  ");
            await expenseRepository.AddAsync(expense, cancellationToken);
            var history = await expenseRepository.ListByAccountAsync(checking.Id, AccountTransactionCriteria.Create(search: "100%"), cancellationToken);
            Assert.Equal("100% groceries", Assert.Single(history).Description);
            var tracked = (await expenseRepository.FindAsync(date, expense.Id, cancellationToken))!;
            tracked.Revise(checking.Id, 10m, ExpenseClassification.Necessities, "Lunch");
            await expenseRepository.UpdateAsync(tracked, cancellationToken);
            await using var fresh = new HouseholdLedgerDbContext(options);
            Assert.Equal("Lunch", Assert.Single(await new ExpenseTransactionRepository(fresh).ListByDateAsync(date, cancellationToken)).Description);
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }
}
