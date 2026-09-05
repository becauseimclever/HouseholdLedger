// <copyright file="ExpenseTransactionServiceTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.UnitTests;

using HouseholdLedger.Application.Transactions;
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
        var service = new ExpenseTransactionService(repository);
        var date = new DateOnly(2026, 9, 1);

        var result = await service.CreateAsync(date, 42.50m, ExpenseClassification.Culture, TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.NotEqual(Guid.Empty, result.Id),
            () => Assert.Equal(date, result.Date),
            () => Assert.Equal(42.50m, result.Amount),
            () => Assert.Equal(ExpenseClassification.Culture, result.Classification),
            () => Assert.Same(repository.Added, repository.Items.Single()));
    }

    /// <summary>Verifies listing maps repository results without inventing records.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ListReturnsRepositoryResults()
    {
        var date = new DateOnly(2026, 9, 1);
        var repository = new StubRepository();
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), date, 5m, ExpenseClassification.Necessities, 1));
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), date, 8m, ExpenseClassification.Unexpected, 2));
        var service = new ExpenseTransactionService(repository);

        var results = await service.ListAsync(date, TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(2, results.Count),
            () => Assert.Equal(5m, results[0].Amount),
            () => Assert.Equal(8m, results[1].Amount));
    }

    /// <summary>Verifies monthly summaries include zero days and cumulative spending.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task SummarizeMonthReturnsDailyTotalsAndMonthToDateSpending()
    {
        var repository = new StubRepository();
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), new DateOnly(2026, 9, 1), 5m, ExpenseClassification.Necessities, 1));
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), new DateOnly(2026, 9, 1), 8m, ExpenseClassification.Unexpected, 2));
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), new DateOnly(2026, 9, 3), 2.50m, ExpenseClassification.Culture, 3));
        repository.Items.Add(new ExpenseTransaction(Guid.NewGuid(), new DateOnly(2026, 10, 1), 100m, ExpenseClassification.Optional, 4));
        var service = new ExpenseTransactionService(repository);

        var results = await service.SummarizeMonthAsync(2026, 9, TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(30, results.Count),
            () => Assert.Equal(new DailyExpenseSummaryDto(new DateOnly(2026, 9, 1), 13m, 13m), results[0]),
            () => Assert.Equal(new DailyExpenseSummaryDto(new DateOnly(2026, 9, 2), 0m, 13m), results[1]),
            () => Assert.Equal(new DailyExpenseSummaryDto(new DateOnly(2026, 9, 3), 2.50m, 15.50m), results[2]),
            () => Assert.Equal(new DailyExpenseSummaryDto(new DateOnly(2026, 9, 30), 0m, 15.50m), results[^1]));
    }

    /// <summary>Verifies correction persists a date-scoped transaction.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReviseUpdatesAnExistingDateScopedTransaction()
    {
        var date = new DateOnly(2026, 9, 1);
        var transaction = new ExpenseTransaction(Guid.NewGuid(), date, 5m, ExpenseClassification.Necessities, 1);
        var repository = new StubRepository();
        repository.Items.Add(transaction);
        var service = new ExpenseTransactionService(repository);

        var result = await service.ReviseAsync(
            date,
            transaction.Id,
            8.75m,
            ExpenseClassification.Unexpected,
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.NotNull(result),
            () => Assert.Equal(8.75m, result!.Amount),
            () => Assert.Equal(ExpenseClassification.Unexpected, result!.Classification),
            () => Assert.Same(transaction, repository.Updated));
    }

    /// <summary>Verifies removal does not cross the route-selected date.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task RemoveRequiresMatchingDateAndIdentifier()
    {
        var date = new DateOnly(2026, 9, 1);
        var transaction = new ExpenseTransaction(Guid.NewGuid(), date, 5m, ExpenseClassification.Necessities, 1);
        var repository = new StubRepository();
        repository.Items.Add(transaction);
        var service = new ExpenseTransactionService(repository);

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

        public Task<IReadOnlyList<ExpenseTransaction>> ListByDateAsync(DateOnly ledgerDate, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ExpenseTransaction>>(
            this.Items.Where(transaction => transaction.Date == ledgerDate).ToArray());
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
}
