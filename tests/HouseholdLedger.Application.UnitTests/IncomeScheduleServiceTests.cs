// <copyright file="IncomeScheduleServiceTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.UnitTests;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Application.Income;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Income;
using Xunit;

/// <summary>Tests recurring income schedule application behavior.</summary>
public sealed class IncomeScheduleServiceTests
{
    /// <summary>Verifies schedule reads return the requested schedule and the complete schedule list as DTOs.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetAndListReturnScheduleDtos()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        var firstSchedule = new PaySchedule(
            Guid.NewGuid(),
            "Salary",
            new DateOnly(2026, 9, 15),
            PayPeriodCadence.Biweekly,
            2500m,
            [new IncomeAccountAllocation(account.Id, 2500m)]);
        var secondSchedule = new PaySchedule(
            Guid.NewGuid(),
            "Contract",
            new DateOnly(2026, 9, 30),
            PayPeriodCadence.Monthly,
            1200m,
            [new IncomeAccountAllocation(account.Id, 1200m)]);
        var repository = new StubIncomeScheduleRepository([firstSchedule, secondSchedule]);
        var service = new IncomeScheduleService(repository, new StubAccountRepository(account));

        var found = await service.GetAsync(firstSchedule.Id, TestContext.Current.CancellationToken);
        var schedules = await service.ListAsync(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(firstSchedule.Id, found?.Id),
            () => Assert.Equal("Salary", found?.Name),
            () => Assert.Collection(
                schedules,
                schedule => Assert.Equal(firstSchedule.Id, schedule.Id),
                schedule => Assert.Equal(secondSchedule.Id, schedule.Id)));
    }

    /// <summary>Verifies calendar receipt reads are inclusive and return immutable allocation snapshots.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListReceiptsReturnsInclusiveCalendarRangeWithAllocationSnapshots()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        var scheduleId = Guid.NewGuid();
        var firstReceipt = new IncomeReceipt(
            Guid.NewGuid(),
            scheduleId,
            new DateOnly(2026, 9, 1),
            2500m,
            [new IncomeAccountAllocation(account.Id, 2500m)]);
        var finalReceipt = new IncomeReceipt(
            Guid.NewGuid(),
            scheduleId,
            new DateOnly(2026, 9, 30),
            2600m,
            [new IncomeAccountAllocation(account.Id, 2600m)]);
        var outsideReceipt = new IncomeReceipt(
            Guid.NewGuid(),
            scheduleId,
            new DateOnly(2026, 10, 1),
            2700m,
            [new IncomeAccountAllocation(account.Id, 2700m)]);
        var repository = new StubIncomeScheduleRepository(receipts: [firstReceipt, finalReceipt, outsideReceipt]);
        var service = new IncomeScheduleService(repository, new StubAccountRepository(account));

        var receipts = await service.ListReceiptsAsync(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Collection(
                receipts,
                receipt => Assert.Equal(firstReceipt.Id, receipt.Id),
                receipt => Assert.Equal(finalReceipt.Id, receipt.Id)),
            () => Assert.Equal(scheduleId, receipts[0].ScheduleId),
            () => Assert.Equal(new DateOnly(2026, 9, 1), receipts[0].PayDate),
            () => Assert.Equal(2500m, receipts[0].NetIncome),
            () => Assert.Collection(
                receipts[0].Allocations,
                allocation =>
                {
                    Assert.Equal(account.Id, allocation.AccountId);
                    Assert.Equal(2500m, allocation.Amount);
                }));
    }

    /// <summary>Verifies calendar receipt reads reject an inverted date range without querying persistence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListReceiptsRejectsInvertedCalendarRange()
    {
        var repository = new StubIncomeScheduleRepository();
        var service = new IncomeScheduleService(repository, new StubAccountRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.ListReceiptsAsync(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 9, 30),
            TestContext.Current.CancellationToken));

        Assert.False(repository.ListReceiptsCalled);
    }

    /// <summary>Verifies an unknown allocation account prevents schedule persistence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CreateRejectsUnknownAllocationAccountWithoutPersistence()
    {
        var repository = new StubIncomeScheduleRepository();
        var service = new IncomeScheduleService(repository, new StubAccountRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new IncomeScheduleCommand("Salary", new DateOnly(2026, 9, 15), PayPeriodCadence.Biweekly, 2500m, [new IncomeAllocationCommand(Guid.NewGuid(), 2500m)]),
            TestContext.Current.CancellationToken));

        Assert.Empty(repository.Schedules);
    }

    private sealed class StubAccountRepository(params Account[] accounts) : IAccountRepository
    {
        public Task<Account?> FindAsync(Guid accountId, CancellationToken cancellationToken) => Task.FromResult(accounts.SingleOrDefault(account => account.Id == accountId));

        public Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Account>>(accounts);
    }

    private sealed class StubIncomeScheduleRepository : IIncomeScheduleRepository
    {
        public StubIncomeScheduleRepository(
            IEnumerable<PaySchedule>? schedules = null,
            IEnumerable<IncomeReceipt>? receipts = null)
        {
            this.Schedules.AddRange(schedules ?? []);
            this.Receipts.AddRange(receipts ?? []);
        }

        public List<PaySchedule> Schedules { get; } = [];

        public List<IncomeReceipt> Receipts { get; } = [];

        public bool ListReceiptsCalled { get; private set; }

        public Task AddScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken)
        {
            this.Schedules.Add(schedule);
            return Task.CompletedTask;
        }

        public Task<PaySchedule?> FindScheduleAsync(Guid scheduleId, CancellationToken cancellationToken) => Task.FromResult(this.Schedules.SingleOrDefault(schedule => schedule.Id == scheduleId));

        public Task<IReadOnlyList<PaySchedule>> ListSchedulesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PaySchedule>>(this.Schedules);

        public Task<IReadOnlyList<IncomeReceipt>> ListReceiptsAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
        {
            this.ListReceiptsCalled = true;
            return Task.FromResult<IReadOnlyList<IncomeReceipt>>(this.Receipts.Where(receipt => receipt.PayDate >= startDate && receipt.PayDate <= endDate).ToArray());
        }

        public Task<IncomeReceipt?> FindReceiptAsync(Guid receiptId, CancellationToken cancellationToken) =>
            Task.FromResult(this.Receipts.SingleOrDefault(receipt => receipt.Id == receiptId));

        public Task AddReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken)
        {
            this.Receipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task UpdateScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IncomeReceipt> ConfirmReceiptAsync(IncomeReceipt receipt, Guid? requestId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> UpdateReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> DeleteReceiptAsync(Guid receiptId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
