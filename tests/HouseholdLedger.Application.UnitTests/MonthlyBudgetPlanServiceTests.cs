// <copyright file="MonthlyBudgetPlanServiceTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.UnitTests;

using HouseholdLedger.Application.Income;
using HouseholdLedger.Application.Planning;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Income;
using HouseholdLedger.Domain.Planning;
using HouseholdLedger.Domain.Transactions;
using Xunit;

/// <summary>Tests monthly intention persistence and actual-versus-plan review.</summary>
public sealed class MonthlyBudgetPlanServiceTests
{
    /// <summary>Saves a reconciled monthly plan with its latest revision timestamp.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SavePersistsPlanWithCurrentTime()
    {
        var now = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        var repository = new StubPlanRepository();
        var service = CreateService(repository, clock: new FixedTimeProvider(now));

        var plan = await service.SaveAsync(
            2026,
            10,
            new MonthlyBudgetPlanCommand(1000m, 200m, 400m, 100m, 100m, 200m),
            TestContext.Current.CancellationToken);

        Assert.Equal(now, plan.LastRevisedAt);
        Assert.Equal(plan, repository.Plan);
    }

    /// <summary>Rejects an unreconciled plan without persisting it.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SaveRejectsUnreconciledAmountsBeforePersistence()
    {
        var repository = new StubPlanRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(
            2026,
            10,
            new MonthlyBudgetPlanCommand(1000m, 200m, 400m, 100m, 100m, 199.99m),
            TestContext.Current.CancellationToken));

        Assert.Null(repository.Plan);
    }

    /// <summary>Compares persisted expenses and explicitly confirmed income with the saved intention.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReviewUsesConfirmedIncomeAndExactClassifiedExpenses()
    {
        var month = new DateOnly(2026, 10, 1);
        var account = new Account(Guid.NewGuid(), "Checking");
        var plan = new MonthlyBudgetPlan(month, 1000m, 200m, 400m, 100m, 100m, 200m, DateTimeOffset.UtcNow);
        var planRepository = new StubPlanRepository(plan);
        var expenseRepository = new StubExpenseRepository(
            new ExpenseTransaction(Guid.NewGuid(), account.Id, new DateOnly(2026, 10, 4), 450m, ExpenseClassification.Necessities));
        var incomeRepository = new StubIncomeRepository(
            new IncomeReceipt(Guid.NewGuid(), null, new DateOnly(2026, 10, 2), 900m, [new IncomeAccountAllocation(account.Id, 900m)]));
        var service = new MonthlyBudgetPlanService(
            planRepository,
            expenseRepository,
            incomeRepository,
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var review = await service.ReviewAsync(2026, 10, TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(1000m, review.ExpectedIncome),
            () => Assert.Equal(900m, review.ActualIncome),
            () => Assert.Equal(-100m, review.IncomeVariance),
            () => Assert.Equal(450m, review.UnallocatedRemainder),
            () => Assert.Equal(50m, Assert.Single(review.Classifications, item => item.Classification == ExpenseClassification.Necessities).Variance),
            () => Assert.Equal(0m, review.Classifications.Single(item => item.Classification == ExpenseClassification.Optional).ActualAmount));
    }

    /// <summary>Preserves a signed shortfall while the unallocated remainder stays nonnegative.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReviewWithoutPlanReportsSignedCashflowShortfall()
    {
        var accountId = Guid.NewGuid();
        var service = new MonthlyBudgetPlanService(
            new StubPlanRepository(),
            new StubExpenseRepository(new ExpenseTransaction(Guid.NewGuid(), accountId, new DateOnly(2026, 9, 1), 150m, ExpenseClassification.Necessities)),
            new StubIncomeRepository(new IncomeReceipt(Guid.NewGuid(), null, new DateOnly(2026, 9, 1), 100m, [new IncomeAccountAllocation(accountId, 100m)])),
            TimeProvider.System);
        var review = await service.ReviewAsync(2026, 9, TestContext.Current.CancellationToken);
        Assert.Equal(-50m, review.CashflowDifference);
        Assert.Equal(0m, review.UnallocatedRemainder);
        Assert.Null(review.ExpectedIncome);
    }

    private static MonthlyBudgetPlanService CreateService(
        StubPlanRepository? planRepository = null,
        TimeProvider? clock = null) =>
        new(
            planRepository ?? new StubPlanRepository(),
            new StubExpenseRepository(),
            new StubIncomeRepository(),
            clock ?? new FixedTimeProvider(DateTimeOffset.UtcNow));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StubPlanRepository(MonthlyBudgetPlan? plan = null) : IMonthlyBudgetPlanRepository
    {
        public MonthlyBudgetPlan? Plan { get; private set; } = plan;

        public Task<MonthlyBudgetPlan?> FindAsync(DateOnly month, CancellationToken cancellationToken) =>
            Task.FromResult(this.Plan?.Month == month ? this.Plan : null);

        public Task UpsertAsync(MonthlyBudgetPlan savedPlan, CancellationToken cancellationToken)
        {
            this.Plan = savedPlan;
            return Task.CompletedTask;
        }
    }

    private sealed class StubExpenseRepository(params ExpenseTransaction[] expenses) : IExpenseTransactionRepository
    {
        public Task AddAsync(ExpenseTransaction transaction, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ExpenseTransaction?> FindAsync(DateOnly ledgerDate, Guid transactionId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTransactionDto>> ListByDateAsync(DateOnly ledgerDate, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTransactionDto>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTransaction>> ListByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ExpenseTransaction>>(expenses.Where(expense => expense.Date >= startDate && expense.Date <= endDate).ToArray());

        public Task UpdateAsync(ExpenseTransaction transaction, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RemoveAsync(ExpenseTransaction transaction, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class StubIncomeRepository(params IncomeReceipt[] receipts) : IIncomeScheduleRepository
    {
        public Task AddScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PaySchedule?> FindScheduleAsync(Guid scheduleId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<PaySchedule>> ListSchedulesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<IncomeReceipt>> ListReceiptsAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IncomeReceipt>>(receipts.Where(receipt => receipt.PayDate >= startDate && receipt.PayDate <= endDate).ToArray());

        public Task<IncomeReceipt?> FindReceiptAsync(Guid receiptId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task AddReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task UpdateScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IncomeReceipt> ConfirmReceiptAsync(IncomeReceipt receipt, Guid? requestId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> UpdateReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> DeleteReceiptAsync(Guid receiptId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
