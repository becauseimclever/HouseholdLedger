// <copyright file="AccountTransactionEndpointTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Transactions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

/// <summary>Verifies account transaction history through the ASP.NET Core host.</summary>
public sealed class AccountTransactionEndpointTests
{
    private static readonly Account Checking = new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Household Checking");
    private static readonly Account Savings = new(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Rainy Day Savings");

    /// <summary>Verifies populated, empty, and missing histories have distinct responses.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetReturnsSelectedAccountHistoryEmptyAndNotFoundStates()
    {
        await using var factory = new AccountTransactionApiFactory();
        using var client = ApiTestClient.Create(factory);
        var cancellationToken = TestContext.Current.CancellationToken;

        var populated = await client.GetFromJsonAsync<AccountTransactionHistoryResponse>(
            $"/api/v1/accounts/{Checking.Id}/transactions",
            cancellationToken);
        var empty = await client.GetFromJsonAsync<AccountTransactionHistoryResponse>(
            $"/api/v1/accounts/{Savings.Id}/transactions",
            cancellationToken);
        using var missingResponse = await client.GetAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/transactions",
            cancellationToken);
        var missing = await missingResponse.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);

        Assert.Multiple(
            () => Assert.Equal(Checking.Id, populated!.Account.Id),
            () => Assert.Equal(Checking.Name, populated!.Account.Name),
            () => Assert.Equal([new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 3), new DateOnly(2026, 9, 3)], populated!.Transactions.Select(transaction => transaction.Date)),
            () => Assert.All(populated!.Transactions, transaction => Assert.Equal(Checking.Id, transaction.AccountId)),
            () => Assert.Equal(Savings.Id, empty!.Account.Id),
            () => Assert.Empty(empty!.Transactions),
            () => Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode),
            () => Assert.Equal("Account not found", missing!.Title));
    }

    /// <summary>Verifies combined filters bind to typed criteria and return only matching account rows.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task GetAppliesCombinedAccountTransactionFilters()
    {
        await using var factory = new AccountTransactionApiFactory();
        using var client = ApiTestClient.Create(factory);

        var history = await client.GetFromJsonAsync<AccountTransactionHistoryResponse>(
            $"/api/v1/accounts/{Checking.Id}/transactions?from=2026-09-01&to=2026-09-05&classification=culture&minimumAmount=10&maximumAmount=20&search=14",
            TestContext.Current.CancellationToken);

        var transaction = Assert.Single(history!.Transactions);
        Assert.Multiple(
            () => Assert.Equal(new DateOnly(2026, 9, 3), transaction.Date),
            () => Assert.Equal(14m, transaction.Amount),
            () => Assert.Equal("Culture", transaction.Classification),
            () => Assert.Equal(1, factory.Repository.ListCalls));
    }

    /// <summary>Verifies invalid filter values return validation details without querying transactions.</summary>
    /// <param name="query">The invalid query string.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData("classification=unknown")]
    [InlineData("from=2026-09-30&to=2026-09-01")]
    [InlineData("minimumAmount=-1")]
    [InlineData("minimumAmount=20&maximumAmount=10")]
    public async Task GetRejectsInvalidFiltersWithoutQueryingHistory(string query)
    {
        await using var factory = new AccountTransactionApiFactory();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.GetAsync(
            $"/api/v1/accounts/{Checking.Id}/transactions?{query}",
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
            () => Assert.NotEmpty(problem!.Errors),
            () => Assert.Equal(0, factory.Repository.ListCalls));
    }

    private sealed class AccountTransactionApiFactory : WebApplicationFactory<Program>
    {
        public HistoryRepository Repository { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Path.Combine(FindRepositoryRoot(), "src", "HouseholdLedger.Api"));
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ExpenseTransactionService>();
                services.RemoveAll<IExpenseTransactionRepository>();
                services.AddSingleton<IExpenseTransactionRepository>(this.Repository);
                services.RemoveAll<IAccountRepository>();
                services.AddSingleton<IAccountRepository, HistoryAccountRepository>();
            });
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HouseholdLedger.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName
                ?? throw new DirectoryNotFoundException("Could not locate the HouseholdLedger repository root.");
        }
    }

    private sealed class HistoryAccountRepository : IAccountRepository
    {
        private static readonly IReadOnlyList<Account> Accounts = [Checking, Savings];

        public Task<Account?> FindAsync(Guid accountId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Accounts.SingleOrDefault(account => account.Id == accountId));
        }

        public Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class HistoryRepository : IExpenseTransactionRepository
    {
        private static readonly IReadOnlyList<ExpenseTransaction> Transactions =
        [
            new(Guid.NewGuid(), Checking.Id, new DateOnly(2026, 9, 3), 14m, ExpenseClassification.Culture, 3),
            new(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 10, 1), 9.75m, ExpenseClassification.Culture, 7),
            new(Guid.NewGuid(), Checking.Id, new DateOnly(2026, 9, 3), 6.50m, ExpenseClassification.Optional, 4),
            new(Guid.NewGuid(), Checking.Id, new DateOnly(2026, 9, 30), 40m, ExpenseClassification.Unexpected, 6),
        ];

        public int ListCalls { get; private set; }

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
            this.ListCalls++;
            var search = criteria.Search;
            return Task.FromResult<IReadOnlyList<ExpenseTransactionDto>>(
                Transactions
                    .Where(transaction => transaction.AccountId == accountId)
                    .Where(transaction => criteria.FromDate is null || transaction.Date >= criteria.FromDate)
                    .Where(transaction => criteria.ToDate is null || transaction.Date <= criteria.ToDate)
                    .Where(transaction => criteria.Classification is null || transaction.Classification == criteria.Classification)
                    .Where(transaction => criteria.MinimumAmount is null || transaction.Amount >= criteria.MinimumAmount)
                    .Where(transaction => criteria.MaximumAmount is null || transaction.Amount <= criteria.MaximumAmount)
                    .Where(transaction => search is null
                        || transaction.Classification.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                        || transaction.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase)
                        || transaction.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(transaction => transaction.Date)
                    .ThenByDescending(transaction => transaction.Sequence)
                    .Select(transaction => new ExpenseTransactionDto(
                        transaction.Id,
                        transaction.AccountId,
                        Checking.Name,
                        transaction.Date,
                        transaction.Amount,
                        transaction.Classification))
                    .ToArray());
        }

        public Task AddAsync(ExpenseTransaction transaction, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ExpenseTransaction?> FindAsync(DateOnly ledgerDate, Guid transactionId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTransactionDto>> ListByDateAsync(DateOnly ledgerDate, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTransaction>> ListByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpdateAsync(ExpenseTransaction transaction, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RemoveAsync(ExpenseTransaction transaction, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
