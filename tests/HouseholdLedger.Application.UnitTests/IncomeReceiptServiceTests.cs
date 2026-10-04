// <copyright file="IncomeReceiptServiceTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.UnitTests;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Application.Income;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Income;
using Xunit;

/// <summary>Verifies receipt dates and account allocation integrity at the application boundary.</summary>
public sealed class IncomeReceiptServiceTests
{
    /// <summary>Accepts today's explicit receipt and forwards its optional retry token.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreatesTodaysReceiptAndForwardsRetryToken()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        var repository = new ReceiptRepository();
        var service = new IncomeReceiptService(repository, new AccountRepository(account), new FixedClock());
        var token = Guid.NewGuid();
        var receipt = await service.CreateAsync(
            new IncomeReceiptCommand(new DateOnly(2026, 10, 4), 100m, [new IncomeAllocationCommand(account.Id, 100m)], token),
            TestContext.Current.CancellationToken);
        Assert.Equal(token, repository.RequestId);
        Assert.Equal(100m, receipt.NetIncome);
        Assert.Null(receipt.ScheduleId);
    }

    /// <summary>Rejects a future or unspecified date and mismatched account destinations before persistence.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task RejectsInvalidActualsBeforePersistence()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        var repository = new ReceiptRepository();
        var service = new IncomeReceiptService(repository, new AccountRepository(account), new FixedClock());
        var command = new IncomeReceiptCommand(new DateOnly(2026, 10, 4), 100m, [new IncomeAllocationCommand(account.Id, 100m)]);
        foreach (var invalid in new[]
        {
            command with { ReceivedDate = new DateOnly(2026, 10, 5) },
            command with { ReceivedDate = default },
            command with { RequestId = Guid.Empty },
            command with { Amount = 101m },
            command with { Allocations = [new IncomeAllocationCommand(Guid.NewGuid(), 100m)] },
        })
        {
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(invalid, TestContext.Current.CancellationToken));
        }

        Assert.Equal(0, repository.ConfirmationCount);
    }

    private sealed class FixedClock : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class AccountRepository(Account account) : IAccountRepository
    {
        public Task<Account?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult(accountId == account.Id ? account : null);

        public Task<bool> TryAddAsync(Account value, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class ReceiptRepository : IIncomeScheduleRepository
    {
        public Guid? RequestId { get; private set; }

        public int ConfirmationCount { get; private set; }

        public Task<IncomeReceipt> ConfirmReceiptAsync(IncomeReceipt receipt, Guid? requestId, CancellationToken cancellationToken)
        {
            this.RequestId = requestId;
            this.ConfirmationCount++;
            return Task.FromResult(receipt);
        }

        public Task AddScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PaySchedule?> FindScheduleAsync(Guid scheduleId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<PaySchedule>> ListSchedulesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<IncomeReceipt>> ListReceiptsAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IncomeReceipt?> FindReceiptAsync(Guid receiptId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task AddReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task UpdateScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> UpdateReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> DeleteReceiptAsync(Guid receiptId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
