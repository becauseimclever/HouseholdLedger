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

        public Task<IReadOnlyList<ExpenseTransaction>> ListByDateAsync(
            DateOnly ledgerDate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ExpenseTransaction>>(
                this.transactions.Where(transaction => transaction.Date == ledgerDate).ToArray());
        }
    }
}
