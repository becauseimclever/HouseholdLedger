// <copyright file="TransactionInspectorTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Components;
using HouseholdLedger.Client.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Verifies selected-day transaction entry behavior.
/// </summary>
public sealed class TransactionInspectorTests
{
    /// <summary>Verifies a valid form save is followed by an authoritative reread.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ValidSaveRereadsAndDisplaysThePersistedTransaction()
    {
        using var context = new BunitContext();
        var selectedDate = new SelectedDateState();
        var api = new StubTransactionsApiClient();
        selectedDate.Select(new DateOnly(2026, 9, 1));
        context.Services.AddSingleton(selectedDate);
        context.Services.AddSingleton<ITransactionsApiClient>(api);

        var component = context.Render<TransactionInspector>();
        component.WaitForAssertion(() => Assert.Contains("No transactions recorded", component.Markup, StringComparison.Ordinal));

        await component.Find("#transaction-amount").InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "12.34" });
        await component.Find("#transaction-classification").ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "Culture" });
        await component.Find("form").SubmitAsync();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(2, api.ListCallCount);
            Assert.Contains("Culture", component.Markup, StringComparison.Ordinal);
            Assert.Contains("$12.34", component.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class StubTransactionsApiClient : ITransactionsApiClient
    {
        private readonly List<ExpenseTransactionResponse> transactions = [];

        public int ListCallCount { get; private set; }

        public Task<bool> CreateAsync(
            DateOnly ledgerDate,
            CreateExpenseTransactionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.transactions.Add(new ExpenseTransactionResponse(Guid.NewGuid(), ledgerDate, request.Amount, request.Classification));
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<ExpenseTransactionResponse>> ListAsync(
            DateOnly ledgerDate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ListCallCount++;
            return Task.FromResult<IReadOnlyList<ExpenseTransactionResponse>>(
                this.transactions.Where(transaction => transaction.Date == ledgerDate).ToArray());
        }
    }
}
