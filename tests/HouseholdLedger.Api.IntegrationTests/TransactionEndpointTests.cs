// <copyright file="TransactionEndpointTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Transactions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

/// <summary>
/// Verifies transaction endpoints through the ASP.NET Core host.
/// </summary>
public sealed class TransactionEndpointTests
{
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
            new CreateExpenseTransactionRequest(18.25m, "Necessities"),
            TestContext.Current.CancellationToken);
        var transactions = await client.GetFromJsonAsync<ExpenseTransactionResponse[]>(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions",
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode),
            () => Assert.Single(transactions!),
            () => Assert.Equal(18.25m, transactions![0].Amount),
            () => Assert.Equal("Necessities", transactions![0].Classification));
    }

    /// <summary>Verifies correction and removal are persisted under the selected date.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReviseThenRemoveUpdatesTheAuthoritativeSelectedDayList()
    {
        await using var factory = new TransactionApiFactory();
        using var client = ApiTestClient.Create(factory);
        var ledgerDate = new DateOnly(2026, 9, 1);
        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions",
            new CreateExpenseTransactionRequest(18.25m, "Necessities"),
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTransactionResponse>(
            TestContext.Current.CancellationToken);

        using var reviseResponse = await client.PutAsJsonAsync(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions/{created!.Id}",
            new UpdateExpenseTransactionRequest(21.50m, "Culture"),
            TestContext.Current.CancellationToken);
        var revised = await reviseResponse.Content.ReadFromJsonAsync<ExpenseTransactionResponse>(
            TestContext.Current.CancellationToken);
        using var removeResponse = await client.DeleteAsync(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions/{created.Id}",
            TestContext.Current.CancellationToken);
        var transactions = await client.GetFromJsonAsync<ExpenseTransactionResponse[]>(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions",
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, reviseResponse.StatusCode),
            () => Assert.Equal(21.50m, revised!.Amount),
            () => Assert.Equal("Culture", revised!.Classification),
            () => Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode),
            () => Assert.Empty(transactions!));
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
            new CreateExpenseTransactionRequest(18.25m, "Necessities"),
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseTransactionResponse>(
            TestContext.Current.CancellationToken);

        using var invalidResponse = await client.PutAsJsonAsync(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions/{created!.Id}",
            new UpdateExpenseTransactionRequest(0m, "Culture"),
            TestContext.Current.CancellationToken);
        using var wrongDateResponse = await client.PutAsJsonAsync(
            $"/api/v1/days/{ledgerDate.AddDays(1):yyyy-MM-dd}/transactions/{created.Id}",
            new UpdateExpenseTransactionRequest(21.50m, "Culture"),
            TestContext.Current.CancellationToken);
        var transactions = await client.GetFromJsonAsync<ExpenseTransactionResponse[]>(
            $"/api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions",
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode),
            () => Assert.Equal(HttpStatusCode.NotFound, wrongDateResponse.StatusCode),
            () => Assert.Equal(18.25m, Assert.Single(transactions!).Amount),
            () => Assert.Equal("Necessities", transactions![0].Classification));
    }

    private sealed class TransactionApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ExpenseTransactionService>();
                services.RemoveAll<IExpenseTransactionRepository>();
                services.AddSingleton<IExpenseTransactionRepository, InMemoryRepository>();
            });
        }
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

        public Task<IReadOnlyList<ExpenseTransaction>> ListByDateAsync(
            DateOnly ledgerDate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ExpenseTransaction>>(
                this.transactions.Where(transaction => transaction.Date == ledgerDate).ToArray());
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
