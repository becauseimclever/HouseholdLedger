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

    /// <summary>Verifies correction and confirmed removal each reread backend state.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReviseAndConfirmedRemoveRereadAuthoritativeState()
    {
        using var context = new BunitContext();
        var date = new DateOnly(2026, 9, 1);
        var selectedDate = new SelectedDateState();
        var api = new StubTransactionsApiClient();
        api.Seed(new ExpenseTransactionResponse(Guid.NewGuid(), date, 12.34m, "Necessities"));
        selectedDate.Select(date);
        context.Services.AddSingleton(selectedDate);
        context.Services.AddSingleton<ITransactionsApiClient>(api);

        var component = context.Render<TransactionInspector>();
        component.WaitForAssertion(() => Assert.Contains("$12.34", component.Markup, StringComparison.Ordinal));
        component.FindAll("button").Single(button => button.TextContent == "Edit").Click();
        await component.Find("input[id^='edit-amount']").InputAsync(
            new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "19.75" });
        await component.Find("select[id^='edit-classification']").ChangeAsync(
            new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "Culture" });
        await component.Find("form.transaction-edit-form").SubmitAsync();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(2, api.ListCallCount);
            Assert.Equal(1, api.ReviseCallCount);
            Assert.Contains("$19.75", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Expense updated", component.Markup, StringComparison.Ordinal);
        });

        component.FindAll("button").Single(button => button.TextContent == "Remove").Click();
        component.FindAll("button").Single(button => button.TextContent == "Cancel").Click();
        Assert.Contains("$19.75", component.Markup, StringComparison.Ordinal);
        Assert.Equal(0, api.RemoveCallCount);

        component.FindAll("button").Single(button => button.TextContent == "Remove").Click();
        component.FindAll("button").Single(button => button.TextContent == "Remove permanently").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(3, api.ListCallCount);
            Assert.Equal(1, api.RemoveCallCount);
            Assert.Contains("No transactions recorded", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Expense removed", component.Markup, StringComparison.Ordinal);
        });
    }

    /// <summary>Verifies invalid correction remains local and preserves entered values.</summary>
    [Fact]
    public void InvalidRevisionDoesNotCallTheApi()
    {
        using var context = new BunitContext();
        var date = new DateOnly(2026, 9, 1);
        var selectedDate = new SelectedDateState();
        var api = new StubTransactionsApiClient();
        api.Seed(new ExpenseTransactionResponse(Guid.NewGuid(), date, 12.34m, "Necessities"));
        selectedDate.Select(date);
        context.Services.AddSingleton(selectedDate);
        context.Services.AddSingleton<ITransactionsApiClient>(api);

        var component = context.Render<TransactionInspector>();
        component.WaitForAssertion(() => Assert.Contains("$12.34", component.Markup, StringComparison.Ordinal));
        component.FindAll("button").Single(button => button.TextContent == "Edit").Click();
        component.Find("input[id^='edit-amount']").Input("0");
        component.Find("form.transaction-edit-form").Submit();

        Assert.Multiple(
            () => Assert.Equal(0, api.ReviseCallCount),
            () => Assert.Equal("0", component.Find("input[id^='edit-amount']").GetAttribute("value")),
            () => Assert.Contains("positive amount", component.Markup, StringComparison.Ordinal));
    }

    private sealed class StubTransactionsApiClient : ITransactionsApiClient
    {
        private readonly List<ExpenseTransactionResponse> transactions = [];

        public int ListCallCount { get; private set; }

        public int ReviseCallCount { get; private set; }

        public int RemoveCallCount { get; private set; }

        public void Seed(ExpenseTransactionResponse transaction)
        {
            this.transactions.Add(transaction);
        }

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

        public Task<TransactionMutationResult> ReviseAsync(
            DateOnly ledgerDate,
            Guid transactionId,
            UpdateExpenseTransactionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ReviseCallCount++;
            var index = this.transactions.FindIndex(
                transaction => transaction.Date == ledgerDate && transaction.Id == transactionId);
            if (index < 0)
            {
                return Task.FromResult(TransactionMutationResult.NotFound);
            }

            this.transactions[index] = this.transactions[index] with
            {
                Amount = request.Amount,
                Classification = request.Classification,
            };
            return Task.FromResult(TransactionMutationResult.Success);
        }

        public Task<TransactionMutationResult> RemoveAsync(
            DateOnly ledgerDate,
            Guid transactionId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.RemoveCallCount++;
            var removed = this.transactions.RemoveAll(
                transaction => transaction.Date == ledgerDate && transaction.Id == transactionId);
            return Task.FromResult(
                removed == 0 ? TransactionMutationResult.NotFound : TransactionMutationResult.Success);
        }
    }
}
