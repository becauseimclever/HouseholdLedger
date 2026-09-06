// <copyright file="TransactionEndpointTests.cs" company="HouseholdLedger">
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

/// <summary>
/// Verifies transaction endpoints through the ASP.NET Core host.
/// </summary>
public sealed class TransactionEndpointTests
{
    private static readonly Account PrimaryAccount = new(Guid.NewGuid(), "Household Checking");
    private static readonly Account SecondaryAccount = new(Guid.NewGuid(), "Cash Wallet");

    /// <summary>Verifies valid creation is persisted and returned by the selected-day read.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreateThenListReturnsThePersistedSelectedDayTransaction()
    {
        await using var factory = new TransactionApiFactory();
        using var client = ApiTestClient.Create(factory);
        var ledgerDate = new DateOnly(2026, 9, 1);

        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions",
            new CreateExpenseTransactionRequest(PrimaryAccount.Id, 18.25m, "Necessities"),
            TestContext.Current.CancellationToken);
        var transactions = await client.GetFromJsonAsync<ExpenseTransactionResponse[]>(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions",
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode),
            () => Assert.Single(transactions!),
            () => Assert.Equal(PrimaryAccount.Id, transactions![0].AccountId),
            () => Assert.Equal(PrimaryAccount.Name, transactions![0].AccountName),
            () => Assert.Equal(18.25m, transactions![0].Amount),
            () => Assert.Equal("Necessities", transactions![0].Classification));
    }

    /// <summary>Verifies the calendar summary returns daily and month-to-date expense totals.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task MonthlySummaryReturnsDailyAndMonthToDateTotals()
    {
        await using var factory = new TransactionApiFactory();
        using var client = ApiTestClient.Create(factory);
        var cancellationToken = TestContext.Current.CancellationToken;

        using var firstCreateResponse = await client.PostAsJsonAsync(
            "/api/v1/days/2026-09-01/transactions",
            new CreateExpenseTransactionRequest(PrimaryAccount.Id, 12.25m, "Necessities"),
            cancellationToken);
        using var secondCreateResponse = await client.PostAsJsonAsync(
            "/api/v1/days/2026-09-03/transactions",
            new CreateExpenseTransactionRequest(PrimaryAccount.Id, 7.75m, "Culture"),
            cancellationToken);
        var summaries = await client.GetFromJsonAsync<DailyExpenseSummaryResponse[]>(
            "/api/v1/months/2026/9/expense-summary",
            cancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Created, firstCreateResponse.StatusCode),
            () => Assert.Equal(HttpStatusCode.Created, secondCreateResponse.StatusCode),
            () => Assert.Equal(30, summaries!.Length),
            () => Assert.Equal(new DailyExpenseSummaryResponse(new DateOnly(2026, 9, 1), 12.25m, 12.25m), summaries![0]),
            () => Assert.Equal(new DailyExpenseSummaryResponse(new DateOnly(2026, 9, 2), 0m, 12.25m), summaries![1]),
            () => Assert.Equal(new DailyExpenseSummaryResponse(new DateOnly(2026, 9, 3), 7.75m, 20m), summaries![2]));
    }

    /// <summary>Verifies correction and removal update selected-day records and monthly summaries.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReviseThenRemoveUpdatesTheAuthoritativeSelectedDayList()
    {
        await using var factory = new TransactionApiFactory();
        using var client = ApiTestClient.Create(factory);
        var ledgerDate = new DateOnly(2026, 9, 1);
        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions",
            new CreateExpenseTransactionRequest(PrimaryAccount.Id, 18.25m, "Necessities"),
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTransactionResponse>(
            TestContext.Current.CancellationToken);

        using var reviseResponse = await client.PutAsJsonAsync(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions/{created!.Id}",
            new UpdateExpenseTransactionRequest(SecondaryAccount.Id, 21.50m, "Culture"),
            TestContext.Current.CancellationToken);
        var revised = await reviseResponse.Content.ReadFromJsonAsync<ExpenseTransactionResponse>(
            TestContext.Current.CancellationToken);
        var revisedSummaries = await client.GetFromJsonAsync<DailyExpenseSummaryResponse[]>(
            "/api/v1/months/2026/9/expense-summary",
            TestContext.Current.CancellationToken);
        using var removeResponse = await client.DeleteAsync(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions/{created.Id}",
            TestContext.Current.CancellationToken);
        var transactions = await client.GetFromJsonAsync<ExpenseTransactionResponse[]>(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions",
            TestContext.Current.CancellationToken);
        var removedSummaries = await client.GetFromJsonAsync<DailyExpenseSummaryResponse[]>(
            "/api/v1/months/2026/9/expense-summary",
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, reviseResponse.StatusCode),
            () => Assert.Equal(SecondaryAccount.Id, revised!.AccountId),
            () => Assert.Equal(SecondaryAccount.Name, revised!.AccountName),
            () => Assert.Equal(21.50m, revised!.Amount),
            () => Assert.Equal("Culture", revised!.Classification),
            () => Assert.Equal(21.50m, revisedSummaries![0].DailyTotal),
            () => Assert.Equal(21.50m, revisedSummaries![0].MonthToDateTotal),
            () => Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode),
            () => Assert.Empty(transactions!),
            () => Assert.Equal(0m, removedSummaries![0].DailyTotal),
            () => Assert.Equal(0m, removedSummaries![0].MonthToDateTotal));
    }

    /// <summary>Verifies invalid and wrong-date corrections do not mutate the transaction.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReviseRejectsInvalidValuesAndWrongDateWithoutMutation()
    {
        await using var factory = new TransactionApiFactory();
        using var client = ApiTestClient.Create(factory);
        var ledgerDate = new DateOnly(2026, 9, 1);
        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions",
            new CreateExpenseTransactionRequest(PrimaryAccount.Id, 18.25m, "Necessities"),
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTransactionResponse>(
            TestContext.Current.CancellationToken);

        using var invalidResponse = await client.PutAsJsonAsync(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions/{created!.Id}",
            new UpdateExpenseTransactionRequest(SecondaryAccount.Id, 0m, "Culture"),
            TestContext.Current.CancellationToken);
        using var wrongDateResponse = await client.PutAsJsonAsync(
            $"/api/v1/days/{ledgerDate.AddDays(1):yyyy-MM-dd}/transactions/{created.Id}",
            new UpdateExpenseTransactionRequest(SecondaryAccount.Id, 21.50m, "Culture"),
            TestContext.Current.CancellationToken);
        var transactions = await client.GetFromJsonAsync<ExpenseTransactionResponse[]>(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions",
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode),
            () => Assert.Equal(HttpStatusCode.NotFound, wrongDateResponse.StatusCode),
            () => Assert.Equal(PrimaryAccount.Id, Assert.Single(transactions!).AccountId),
            () => Assert.Equal(18.25m, Assert.Single(transactions!).Amount),
            () => Assert.Equal("Necessities", transactions![0].Classification));
    }

    /// <summary>Verifies creation rejects empty and unknown accounts without persistence.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreateRejectsInvalidAccountsWithoutPersistence()
    {
        await using var factory = new TransactionApiFactory();
        using var client = ApiTestClient.Create(factory);
        var cancellationToken = TestContext.Current.CancellationToken;

        using var emptyResponse = await client.PostAsJsonAsync(
            "/api/v1/days/2026-09-01/transactions",
            new CreateExpenseTransactionRequest(Guid.Empty, 18.25m, "Necessities"),
            cancellationToken);
        var emptyProblem = await emptyResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        using var missingResponse = await client.PostAsJsonAsync(
            "/api/v1/days/2026-09-01/transactions",
            new CreateExpenseTransactionRequest(Guid.NewGuid(), 18.25m, "Necessities"),
            cancellationToken);
        var missingProblem = await missingResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        var transactions = await client.GetFromJsonAsync<ExpenseTransactionResponse[]>(
            "/api/v1/days/2026-09-01/transactions",
            cancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, emptyResponse.StatusCode),
            () => Assert.Contains(nameof(CreateExpenseTransactionRequest.AccountId), emptyProblem!.Errors.Keys),
            () => Assert.Equal(HttpStatusCode.BadRequest, missingResponse.StatusCode),
            () => Assert.Contains(nameof(CreateExpenseTransactionRequest.AccountId), missingProblem!.Errors.Keys),
            () => Assert.Empty(transactions!));
    }

    /// <summary>Verifies correction rejects an unknown account without changing persisted values.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReviseRejectsUnknownAccountWithoutMutation()
    {
        await using var factory = new TransactionApiFactory();
        using var client = ApiTestClient.Create(factory);
        var cancellationToken = TestContext.Current.CancellationToken;
        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/days/2026-09-01/transactions",
            new CreateExpenseTransactionRequest(PrimaryAccount.Id, 18.25m, "Necessities"),
            cancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTransactionResponse>(cancellationToken);

        using var reviseResponse = await client.PutAsJsonAsync(
            $"/api/v1/days/2026-09-01/transactions/{created!.Id}",
            new UpdateExpenseTransactionRequest(Guid.NewGuid(), 21.50m, "Culture"),
            cancellationToken);
        var problem = await reviseResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        var transactions = await client.GetFromJsonAsync<ExpenseTransactionResponse[]>(
            "/api/v1/days/2026-09-01/transactions",
            cancellationToken);
        var persisted = Assert.Single(transactions!);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, reviseResponse.StatusCode),
            () => Assert.Contains(nameof(UpdateExpenseTransactionRequest.AccountId), problem!.Errors.Keys),
            () => Assert.Equal(PrimaryAccount.Id, persisted.AccountId),
            () => Assert.Equal(18.25m, persisted.Amount),
            () => Assert.Equal("Necessities", persisted.Classification));
    }

    private sealed class TransactionApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Path.Combine(FindRepositoryRoot(), "src", "HouseholdLedger.Api"));
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ExpenseTransactionService>();
                services.RemoveAll<IExpenseTransactionRepository>();
                services.AddSingleton<IExpenseTransactionRepository, InMemoryRepository>();
                services.RemoveAll<IAccountRepository>();
                services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
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

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly IReadOnlyList<Account> accounts = [PrimaryAccount, SecondaryAccount];

        public Task<Account?> FindAsync(Guid accountId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(this.accounts.SingleOrDefault(account => account.Id == accountId));
        }

        public Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class InMemoryRepository : IExpenseTransactionRepository
    {
        private readonly List<ExpenseTransaction> transactions = [];

        public Task AddAsync(ExpenseTransaction transaction, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<ExpenseTransaction?> FindAsync(
            DateOnly ledgerDate,
            Guid transactionId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(this.transactions.SingleOrDefault(
                transaction => transaction.Date == ledgerDate && transaction.Id == transactionId));
        }

        public Task<IReadOnlyList<ExpenseTransactionDto>> ListByDateAsync(
            DateOnly ledgerDate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ExpenseTransactionDto>>(
                this.transactions
                    .Where(transaction => transaction.Date == ledgerDate)
                    .Select(transaction => new ExpenseTransactionDto(
                        transaction.Id,
                        transaction.AccountId,
                        transaction.AccountId == PrimaryAccount.Id ? PrimaryAccount.Name : SecondaryAccount.Name,
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
                this.transactions
                    .Where(transaction => transaction.Date >= startDate && transaction.Date < endDate)
                    .OrderBy(transaction => transaction.Date)
                    .ThenBy(transaction => transaction.Sequence)
                    .ToArray());
        }

        public Task UpdateAsync(ExpenseTransaction transaction, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task RemoveAsync(ExpenseTransaction transaction, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.transactions.Remove(transaction);
            return Task.CompletedTask;
        }
    }
}
