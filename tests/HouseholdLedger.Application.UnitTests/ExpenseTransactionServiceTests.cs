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
    /// <summary>Verifies account-history criteria normalize search and reject unsafe ranges.</summary>
    [Fact]
    public void AccountTransactionCriteriaNormalizeAndValidateValues()
    {
        var criteria = AccountTransactionCriteria.Create(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            ExpenseClassification.Culture,
            5m,
            25m,
            "  culture  ");

        Assert.Multiple(
            () => Assert.Equal("culture", criteria.Search),
            () => Assert.Throws<ArgumentException>(() => AccountTransactionCriteria.Create(
                fromDate: new DateOnly(2026, 9, 2),
                toDate: new DateOnly(2026, 9, 1))),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => AccountTransactionCriteria.Create(minimumAmount: -1m)),
            () => Assert.Throws<ArgumentException>(() => AccountTransactionCriteria.Create(minimumAmount: 10m, maximumAmount: 5m)),
            () => Assert.Throws<ArgumentException>(() => AccountTransactionCriteria.Create(search: new string('x', 101))));
    }

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

    /// <summary>Verifies account history is isolated, newest first, and includes account identity.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetAccountHistoryReturnsOnlyTheSelectedAccountNewestFirst()
    {
        var selectedAccount = new Account(Guid.NewGuid(), "Household Checking");
        var otherAccount = new Account(Guid.NewGuid(), "Cash Wallet");
        var repository = new StubRepository();
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), selectedAccount.Id, new DateOnly(2026, 9, 3), 14m, ExpenseClassification.Culture, 3));
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), otherAccount.Id, new DateOnly(2026, 10, 1), 9.75m, ExpenseClassification.Culture, 7));
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), selectedAccount.Id, new DateOnly(2026, 9, 3), 6.50m, ExpenseClassification.Optional, 4));
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), selectedAccount.Id, new DateOnly(2026, 9, 30), 40m, ExpenseClassification.Unexpected, 6));
        var service = new ExpenseTransactionService(repository, new StubAccountRepository(selectedAccount, otherAccount));

        var result = await service.GetAccountHistoryAsync(
            selectedAccount.Id,
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.NotNull(result),
            () => Assert.Equal(selectedAccount.Id, result!.AccountId),
            () => Assert.Equal(selectedAccount.Name, result!.AccountName),
            () => Assert.Equal(3, result!.Transactions.Count),
            () => Assert.Equal([new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 3), new DateOnly(2026, 9, 3)], result!.Transactions.Select(transaction => transaction.Date)),
            () => Assert.Equal([6L, 4L, 3L], result!.Transactions.Select(transaction => repository.Items.Single(item => item.Id == transaction.Id).Sequence)),
            () => Assert.All(result!.Transactions, transaction => Assert.Equal(selectedAccount.Id, transaction.AccountId)));
    }

    /// <summary>Verifies account history passes validated criteria to the account-scoped repository query.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetAccountHistoryPassesCriteriaToRepository()
    {
        var account = new Account(Guid.NewGuid(), "Household Checking");
        var repository = new StubRepository();
        var service = new ExpenseTransactionService(repository, new StubAccountRepository(account));
        var criteria = AccountTransactionCriteria.Create(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            ExpenseClassification.Culture,
            5m,
            25m,
            "culture");

        await service.GetAccountHistoryAsync(account.Id, criteria, TestContext.Current.CancellationToken);

        Assert.Same(criteria, repository.LastAccountCriteria);
    }

    /// <summary>Verifies empty and missing accounts remain distinct.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetAccountHistoryDistinguishesEmptyFromMissingAccount()
    {
        var emptyAccount = new Account(Guid.NewGuid(), "Rainy Day Savings");
        var repository = new StubRepository();
        var service = new ExpenseTransactionService(repository, new StubAccountRepository(emptyAccount));

        var empty = await service.GetAccountHistoryAsync(emptyAccount.Id, TestContext.Current.CancellationToken);
        var missing = await service.GetAccountHistoryAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.NotNull(empty),
            () => Assert.Empty(empty!.Transactions),
            () => Assert.Null(missing),
            () => Assert.Equal(1, repository.AccountListCalls));
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
        var checkingHistory = await service.GetAccountHistoryAsync(
            checkingId,
            TestContext.Current.CancellationToken);
        var cashHistory = await service.GetAccountHistoryAsync(
            cashId,
            TestContext.Current.CancellationToken);
        var savingsHistory = await service.GetAccountHistoryAsync(
            savingsId,
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(94.45m, results[0].DailyTotal),
            () => Assert.Equal(20.50m, results[2].DailyTotal),
            () => Assert.Equal(25.25m, results[14].DailyTotal),
            () => Assert.Equal(40m, results[29].DailyTotal),
            () => Assert.Equal(180.20m, results[29].MonthToDateTotal),
            () => Assert.Equal(Enumerable.Range(1, 7), repository.Items.Select(transaction => (int)transaction.Sequence)),
            () => Assert.DoesNotContain(repository.Items, transaction => transaction.AccountId == savingsId),
            () => Assert.Equal([6L, 4L, 3L, 1L], checkingHistory!.Transactions.Select(transaction => repository.Items.Single(item => item.Id == transaction.Id).Sequence)),
            () => Assert.All(checkingHistory!.Transactions, transaction => Assert.Equal(checkingId, transaction.AccountId)),
            () => Assert.Equal([7L, 5L, 2L], cashHistory!.Transactions.Select(transaction => repository.Items.Single(item => item.Id == transaction.Id).Sequence)),
            () => Assert.All(cashHistory!.Transactions, transaction => Assert.Equal(cashId, transaction.AccountId)),
            () => Assert.Empty(savingsHistory!.Transactions));
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

        public int AccountListCalls { get; private set; }

        public AccountTransactionCriteria? LastAccountCriteria { get; private set; }

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

        public Task<IReadOnlyList<ExpenseTransactionDto>> ListByAccountAsync(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            return this.ListByAccountAsync(
                accountId,
                AccountTransactionCriteria.Create(),
                cancellationToken);
        }

        public Task<IReadOnlyList<ExpenseTransactionDto>> ListByAccountAsync(
            Guid accountId,
            AccountTransactionCriteria criteria,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.AccountListCalls++;
            this.LastAccountCriteria = criteria;
            return Task.FromResult<IReadOnlyList<ExpenseTransactionDto>>(
                this.Items
                    .Where(transaction => transaction.AccountId == accountId)
                    .OrderByDescending(transaction => transaction.Date)
                    .ThenByDescending(transaction => transaction.Sequence)
                    .Select(transaction => new ExpenseTransactionDto(
                        transaction.Id,
                        transaction.AccountId,
                        string.Empty,
                        transaction.Date,
                        transaction.Amount,
                        transaction.Classification))
                    .ToArray());
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
