// <copyright file="ExpenseTransactionServiceTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.UnitTests;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Transactions;
using Xunit;

/// <summary>
/// Verifies transaction Application use cases.
/// </summary>
public sealed class ExpenseTransactionServiceTests
{
    /// <summary>Verifies creation delegates durable storage to the repository.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreatePersistsAndReturnsTransaction()
    {
        var repository = new StubRepository();
        var account = new Account(Guid.NewGuid(), "Household Checking");
        var accountRepository = new StubAccountRepository(account);
        var service = new ExpenseTransactionService(repository, accountRepository);
        var date = new DateOnly(2026, 9, 1);

        var result = await service.CreateAsync(
            date,
            account.Id,
            42.50m,
            ExpenseClassification.Culture,
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.NotEqual(Guid.Empty, result.Id),
            () => Assert.Equal(account.Id, result.AccountId),
            () => Assert.Equal(account.Name, result.AccountName),
            () => Assert.Equal(date, result.Date),
            () => Assert.Equal(42.50m, result.Amount),
            () => Assert.Equal(ExpenseClassification.Culture, result.Classification),
            () => Assert.Same(repository.Added, repository.Items.Single()));
    }

    /// <summary>Verifies creation rejects an unknown account without writing a transaction.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreateRejectsNonexistentAccountWithoutPersistence()
    {
        var repository = new StubRepository();
        var service = new ExpenseTransactionService(repository, new StubAccountRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new DateOnly(2026, 9, 1),
            Guid.NewGuid(),
            42.50m,
            ExpenseClassification.Culture,
            TestContext.Current.CancellationToken));

        Assert.Multiple(
            () => Assert.Null(repository.Added),
            () => Assert.Empty(repository.Items));
    }

    /// <summary>Verifies creation rejects an empty account identifier without writing a transaction.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreateRejectsEmptyAccountIdentifierWithoutPersistence()
    {
        var repository = new StubRepository();
        var service = new ExpenseTransactionService(repository, new StubAccountRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new DateOnly(2026, 9, 1),
            Guid.Empty,
            42.50m,
            ExpenseClassification.Culture,
            TestContext.Current.CancellationToken));

        Assert.Multiple(
            () => Assert.Null(repository.Added),
            () => Assert.Empty(repository.Items));
    }

    /// <summary>Verifies listing maps repository results without inventing records.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ListReturnsRepositoryResults()
    {
        var date = new DateOnly(2026, 9, 1);
        var repository = new StubRepository();
        repository.ListResults.Add(new ExpenseTransactionDto(
            Guid.NewGuid(), Guid.NewGuid(), "Cash Wallet", date, 5m, ExpenseClassification.Necessities));
        repository.ListResults.Add(new ExpenseTransactionDto(
            Guid.NewGuid(), Guid.NewGuid(), "Household Checking", date, 8m, ExpenseClassification.Unexpected));
        var service = new ExpenseTransactionService(repository, new StubAccountRepository());

        var results = await service.ListAsync(date, TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(2, results.Count),
            () => Assert.Equal("Cash Wallet", results[0].AccountName),
            () => Assert.Equal(5m, results[0].Amount),
            () => Assert.Equal(8m, results[1].Amount));
    }

    /// <summary>Verifies monthly summaries include zero days and cumulative spending.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task SummarizeMonthReturnsDailyTotalsAndMonthToDateSpending()
    {
        var accountId = Guid.NewGuid();
        var repository = new StubRepository();
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), accountId, new DateOnly(2026, 9, 1), 5m, ExpenseClassification.Necessities, 1));
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), accountId, new DateOnly(2026, 9, 1), 8m, ExpenseClassification.Unexpected, 2));
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), accountId, new DateOnly(2026, 9, 3), 2.50m, ExpenseClassification.Culture, 3));
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), accountId, new DateOnly(2026, 10, 1), 100m, ExpenseClassification.Optional, 4));
        var service = new ExpenseTransactionService(repository, new StubAccountRepository());

        var results = await service.SummarizeMonthAsync(2026, 9, TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(30, results.Count),
            () => Assert.Equal(new DailyExpenseSummaryDto(new DateOnly(2026, 9, 1), 13m, 13m), results[0]),
            () => Assert.Equal(new DailyExpenseSummaryDto(new DateOnly(2026, 9, 2), 0m, 13m), results[1]),
            () => Assert.Equal(new DailyExpenseSummaryDto(new DateOnly(2026, 9, 3), 2.50m, 15.50m), results[2]),
            () => Assert.Equal(new DailyExpenseSummaryDto(new DateOnly(2026, 9, 30), 0m, 15.50m), results[^1]));
    }

    /// <summary>Verifies the documented account-owned fixture and all-account September totals.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task DeterministicFixturePreservesOwnershipOrderingAndCalendarTotals()
    {
        var checkingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var cashId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var savingsId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var repository = new StubRepository();
        repository.Items.Add(new ExpenseTransaction(Guid.Parse("20000000-0000-0000-0000-000000000001"), checkingId, new DateOnly(2026, 9, 1), 82.45m, ExpenseClassification.Necessities, 1));
        repository.Items.Add(new ExpenseTransaction(Guid.Parse("20000000-0000-0000-0000-000000000002"), cashId, new DateOnly(2026, 9, 1), 12m, ExpenseClassification.Optional, 2));
        repository.Items.Add(new ExpenseTransaction(Guid.Parse("20000000-0000-0000-0000-000000000003"), checkingId, new DateOnly(2026, 9, 3), 14m, ExpenseClassification.Culture, 3));
        repository.Items.Add(new ExpenseTransaction(Guid.Parse("20000000-0000-0000-0000-000000000004"), checkingId, new DateOnly(2026, 9, 3), 6.50m, ExpenseClassification.Optional, 4));
        repository.Items.Add(new ExpenseTransaction(Guid.Parse("20000000-0000-0000-0000-000000000005"), cashId, new DateOnly(2026, 9, 15), 25.25m, ExpenseClassification.Necessities, 5));
        repository.Items.Add(new ExpenseTransaction(Guid.Parse("20000000-0000-0000-0000-000000000006"), checkingId, new DateOnly(2026, 9, 30), 40m, ExpenseClassification.Unexpected, 6));
        repository.Items.Add(new ExpenseTransaction(Guid.Parse("20000000-0000-0000-0000-000000000007"), cashId, new DateOnly(2026, 10, 1), 9.75m, ExpenseClassification.Culture, 7));
        var service = new ExpenseTransactionService(repository, new StubAccountRepository(
            new Account(checkingId, "Household Checking"),
            new Account(cashId, "Cash Wallet"),
            new Account(savingsId, "Rainy Day Savings")));

        var results = await service.SummarizeMonthAsync(2026, 9, TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(94.45m, results[0].DailyTotal),
            () => Assert.Equal(20.50m, results[2].DailyTotal),
            () => Assert.Equal(25.25m, results[14].DailyTotal),
            () => Assert.Equal(40m, results[29].DailyTotal),
            () => Assert.Equal(180.20m, results[29].MonthToDateTotal),
            () => Assert.Equal(Enumerable.Range(1, 7), repository.Items.Select(transaction => (int)transaction.Sequence)),
            () => Assert.DoesNotContain(repository.Items, transaction => transaction.AccountId == savingsId));
    }

    /// <summary>Verifies correction persists a date-scoped transaction.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReviseUpdatesAnExistingDateScopedTransaction()
    {
        var date = new DateOnly(2026, 9, 1);
        var originalAccountId = Guid.NewGuid();
        var replacementAccount = new Account(Guid.NewGuid(), "Cash Wallet");
        var transaction = new ExpenseTransaction(
            Guid.NewGuid(), originalAccountId, date, 5m, ExpenseClassification.Necessities, 7);
        var repository = new StubRepository();
        repository.Items.Add(transaction);
        var service = new ExpenseTransactionService(repository, new StubAccountRepository(replacementAccount));

        var result = await service.ReviseAsync(
            date,
            transaction.Id,
            replacementAccount.Id,
            8.75m,
            ExpenseClassification.Unexpected,
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.NotNull(result),
            () => Assert.Equal(transaction.Id, result!.Id),
            () => Assert.Equal(replacementAccount.Id, result!.AccountId),
            () => Assert.Equal(replacementAccount.Name, result!.AccountName),
            () => Assert.Equal(date, result!.Date),
            () => Assert.Equal(8.75m, result!.Amount),
            () => Assert.Equal(ExpenseClassification.Unexpected, result!.Classification),
            () => Assert.Equal(7, transaction.Sequence),
            () => Assert.Same(transaction, repository.Updated));
    }

    /// <summary>Verifies correction rejects an unknown account without changing the transaction.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReviseRejectsNonexistentAccountWithoutMutation()
    {
        var date = new DateOnly(2026, 9, 1);
        var originalAccountId = Guid.NewGuid();
        var transaction = new ExpenseTransaction(
            Guid.NewGuid(), originalAccountId, date, 5m, ExpenseClassification.Necessities, 7);
        var repository = new StubRepository();
        repository.Items.Add(transaction);
        var service = new ExpenseTransactionService(repository, new StubAccountRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.ReviseAsync(
            date,
            transaction.Id,
            Guid.NewGuid(),
            8.75m,
            ExpenseClassification.Unexpected,
            TestContext.Current.CancellationToken));

        Assert.Multiple(
            () => Assert.NotEqual(Guid.Empty, transaction.Id),
            () => Assert.Equal(originalAccountId, transaction.AccountId),
            () => Assert.Equal(date, transaction.Date),
            () => Assert.Equal(5m, transaction.Amount),
            () => Assert.Equal(ExpenseClassification.Necessities, transaction.Classification),
            () => Assert.Equal(7, transaction.Sequence),
            () => Assert.Null(repository.Updated));
    }

    /// <summary>Verifies removal does not cross the route-selected date.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task RemoveRequiresMatchingDateAndIdentifier()
    {
        var date = new DateOnly(2026, 9, 1);
        var transaction = new ExpenseTransaction(
            Guid.NewGuid(), Guid.NewGuid(), date, 5m, ExpenseClassification.Necessities, 1);
        var repository = new StubRepository();
        repository.Items.Add(transaction);
        var service = new ExpenseTransactionService(repository, new StubAccountRepository());

        var wrongDateResult = await service.RemoveAsync(
            date.AddDays(1),
            transaction.Id,
            TestContext.Current.CancellationToken);
        var removed = await service.RemoveAsync(date, transaction.Id, TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.False(wrongDateResult),
            () => Assert.True(removed),
            () => Assert.Empty(repository.Items));
    }

    private sealed class StubRepository : IExpenseTransactionRepository
    {
        public List<ExpenseTransaction> Items { get; } = [];

        public List<ExpenseTransactionDto> ListResults { get; } = [];

        public ExpenseTransaction? Added { get; private set; }

        public ExpenseTransaction? Updated { get; private set; }

        public Task AddAsync(ExpenseTransaction transaction, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.Added = transaction;
            this.Items.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<ExpenseTransaction?> FindAsync(
            DateOnly ledgerDate,
            Guid transactionId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(this.Items.SingleOrDefault(
                transaction => transaction.Date == ledgerDate && transaction.Id == transactionId));
        }

        public Task<IReadOnlyList<ExpenseTransactionDto>> ListByDateAsync(
            DateOnly ledgerDate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ExpenseTransactionDto>>(
                this.ListResults.Where(transaction => transaction.Date == ledgerDate).ToArray());
        }

        public Task<IReadOnlyList<ExpenseTransaction>> ListByDateRangeAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ExpenseTransaction>>(
                this.Items
                    .Where(transaction => transaction.Date >= startDate && transaction.Date < endDate)
                    .OrderBy(transaction => transaction.Date)
                    .ThenBy(transaction => transaction.Sequence)
                    .ToArray());
        }

        public Task UpdateAsync(ExpenseTransaction transaction, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.Updated = transaction;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(ExpenseTransaction transaction, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.Items.Remove(transaction);
            return Task.CompletedTask;
        }
    }

    private sealed class StubAccountRepository(params Account[] accounts) : IAccountRepository
    {
        public Task<Account?> FindAsync(Guid accountId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(accounts.SingleOrDefault(account => account.Id == accountId));
        }

        public Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
