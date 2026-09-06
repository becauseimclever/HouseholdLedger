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
    private static readonly AccountResponse HouseholdChecking = new(
        Guid.Parse("10000000-0000-0000-0000-000000000001"),
        "Household Checking");

    private static readonly AccountResponse CashWallet = new(
        Guid.Parse("10000000-0000-0000-0000-000000000002"),
        "Cash Wallet");

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
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([HouseholdChecking]));

        var component = context.Render<TransactionInspector>();
        component.WaitForAssertion(() => Assert.Contains("No transactions recorded", component.Markup, StringComparison.Ordinal));

        await component.Find("#transaction-account").ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = HouseholdChecking.Id.ToString() });
        await component.Find("#transaction-amount").InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "12.34" });
        await component.Find("#transaction-classification").ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "Culture" });
        await component.Find("form").SubmitAsync();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(2, api.ListCallCount);
            Assert.Equal(HouseholdChecking.Id, api.LastCreateRequest!.AccountId);
            Assert.Contains("Culture", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Household Checking", component.Markup, StringComparison.Ordinal);
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
        api.Seed(new ExpenseTransactionResponse(Guid.NewGuid(), HouseholdChecking.Id, HouseholdChecking.Name, date, 12.34m, "Necessities"));
        selectedDate.Select(date);
        context.Services.AddSingleton(selectedDate);
        context.Services.AddSingleton<ITransactionsApiClient>(api);
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([HouseholdChecking, CashWallet]));

        var component = context.Render<TransactionInspector>();
        component.WaitForAssertion(() => Assert.Contains("$12.34", component.Markup, StringComparison.Ordinal));
        component.FindAll("button").Single(button => button.TextContent == "Edit").Click();
        await component.Find("select[id^='edit-account']").ChangeAsync(
            new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = CashWallet.Id.ToString() });
        await component.Find("input[id^='edit-amount']").InputAsync(
            new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "19.75" });
        await component.Find("select[id^='edit-classification']").ChangeAsync(
            new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "Culture" });
        await component.Find("form.transaction-edit-form").SubmitAsync();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(2, api.ListCallCount);
            Assert.Equal(1, api.ReviseCallCount);
            Assert.Equal(CashWallet.Id, api.LastUpdateRequest!.AccountId);
            Assert.Contains("$19.75", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Cash Wallet", component.Markup, StringComparison.Ordinal);
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
        api.Seed(new ExpenseTransactionResponse(Guid.NewGuid(), HouseholdChecking.Id, HouseholdChecking.Name, date, 12.34m, "Necessities"));
        selectedDate.Select(date);
        context.Services.AddSingleton(selectedDate);
        context.Services.AddSingleton<ITransactionsApiClient>(api);
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([HouseholdChecking]));

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

    /// <summary>Verifies create and edit forms expose required labeled account fields.</summary>
    [Fact]
    public void AccountFieldsAreLabeledRequiredAndValidatedLocally()
    {
        using var context = new BunitContext();
        var date = new DateOnly(2026, 9, 1);
        var selectedDate = new SelectedDateState();
        var api = new StubTransactionsApiClient();
        api.Seed(new ExpenseTransactionResponse(Guid.NewGuid(), HouseholdChecking.Id, HouseholdChecking.Name, date, 12.34m, "Necessities"));
        selectedDate.Select(date);
        context.Services.AddSingleton(selectedDate);
        context.Services.AddSingleton<ITransactionsApiClient>(api);
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([HouseholdChecking]));

        var component = context.Render<TransactionInspector>();
        component.WaitForElement("#transaction-account");
        component.Find("form[aria-label='Record an expense']").Submit();
        component.FindAll("button").Single(button => button.TextContent == "Edit").Click();
        component.Find("select[id^='edit-account']").Change(string.Empty);
        component.Find("form.transaction-edit-form").Submit();

        Assert.Multiple(
            () => Assert.Equal("Account", component.Find("label[for='transaction-account']").TextContent),
            () => Assert.True(component.Find("#transaction-account").HasAttribute("required")),
            () => Assert.Contains("Choose an account", component.Markup, StringComparison.Ordinal),
            () => Assert.True(component.Find("select[id^='edit-account']").HasAttribute("required")),
            () => Assert.Equal(0, api.CreateCallCount),
            () => Assert.Equal(0, api.ReviseCallCount));
    }

    /// <summary>Verifies an empty catalog truthfully directs the user to account creation.</summary>
    [Fact]
    public void EmptyAccountCatalogShowsNativeAccountsLinkWithoutPlaceholderForm()
    {
        using var context = new BunitContext();
        var selectedDate = new SelectedDateState();
        selectedDate.Select(new DateOnly(2026, 9, 1));
        context.Services.AddSingleton(selectedDate);
        context.Services.AddSingleton<ITransactionsApiClient>(new StubTransactionsApiClient());
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([]));

        var component = context.Render<TransactionInspector>();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Contains("Add an account before recording an expense", component.Markup, StringComparison.Ordinal),
            () => Assert.Equal("/accounts", component.Find(".account-load-state a").GetAttribute("href")),
            () => Assert.Empty(component.FindAll("#transaction-account")),
            () => Assert.Empty(component.FindAll("form[aria-label='Record an expense']"))));
    }

    /// <summary>Verifies account failures are distinct from transaction state and can be retried.</summary>
    [Fact]
    public void AccountLoadFailureIsDistinctAndRetryable()
    {
        using var context = new BunitContext();
        var selectedDate = new SelectedDateState();
        var accountsApi = new StubAccountsApiClient([HouseholdChecking]) { FailNextList = true };
        selectedDate.Select(new DateOnly(2026, 9, 1));
        context.Services.AddSingleton(selectedDate);
        context.Services.AddSingleton<ITransactionsApiClient>(new StubTransactionsApiClient());
        context.Services.AddSingleton<IAccountsApiClient>(accountsApi);

        var component = context.Render<TransactionInspector>();
        component.WaitForAssertion(() => Assert.Contains("Accounts are unavailable", component.Markup, StringComparison.Ordinal));
        component.FindAll("button").Single(button => button.TextContent == "Try again").Click();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal(2, accountsApi.ListCallCount),
            () => Assert.Single(component.FindAll("#transaction-account")),
            () => Assert.Contains("No transactions recorded", component.Markup, StringComparison.Ordinal)));
    }

    /// <summary>Verifies obsolete account and transaction responses cannot replace the current date's data.</summary>
    [Fact]
    public void LateResponsesDoNotReplaceTheCurrentDatesAccountsOrTransactions()
    {
        using var context = new BunitContext();
        var initialDate = new DateOnly(2026, 9, 1);
        var obsoleteDate = new DateOnly(2026, 9, 2);
        var currentDate = new DateOnly(2026, 9, 3);
        var selectedDate = new SelectedDateState();
        var accountsApi = new ControllableAccountsApiClient([HouseholdChecking]);
        var transactionsApi = new ControllableTransactionsApiClient(initialDate, []);
        selectedDate.Select(initialDate);
        context.Services.AddSingleton(selectedDate);
        context.Services.AddSingleton<ITransactionsApiClient>(transactionsApi);
        context.Services.AddSingleton<IAccountsApiClient>(accountsApi);
        var component = context.Render<TransactionInspector>();
        component.WaitForElement("#transaction-account");

        selectedDate.Select(obsoleteDate);
        component.WaitForAssertion(() => Assert.Equal(2, accountsApi.ListCallCount));
        selectedDate.Select(currentDate);
        component.WaitForAssertion(() => Assert.Equal(3, accountsApi.ListCallCount));

        accountsApi.Complete(3, [CashWallet]);
        transactionsApi.Complete(
            currentDate,
            [new ExpenseTransactionResponse(Guid.NewGuid(), CashWallet.Id, CashWallet.Name, currentDate, 7m, "Culture")]);
        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Contains("Cash Wallet", component.Markup, StringComparison.Ordinal),
            () => Assert.Contains("Culture", component.Markup, StringComparison.Ordinal)));

        accountsApi.Complete(2, [HouseholdChecking]);
        transactionsApi.Complete(
            obsoleteDate,
            [new ExpenseTransactionResponse(Guid.NewGuid(), HouseholdChecking.Id, HouseholdChecking.Name, obsoleteDate, 99m, "Unexpected")]);

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Contains(currentDate.ToString("D", System.Globalization.CultureInfo.CurrentCulture), component.Markup, StringComparison.Ordinal),
            () => Assert.Contains("Cash Wallet", component.Markup, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Household Checking", component.Markup, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Unexpected", component.Find(".transaction-list").TextContent, StringComparison.Ordinal)));
    }

    private sealed class StubTransactionsApiClient : ITransactionsApiClient
    {
        private readonly List<ExpenseTransactionResponse> transactions = [];

        public int ListCallCount { get; private set; }

        public int CreateCallCount { get; private set; }

        public int ReviseCallCount { get; private set; }

        public int RemoveCallCount { get; private set; }

        public CreateExpenseTransactionRequest? LastCreateRequest { get; private set; }

        public UpdateExpenseTransactionRequest? LastUpdateRequest { get; private set; }

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
            this.CreateCallCount++;
            this.LastCreateRequest = request;
            var accountName = request.AccountId == HouseholdChecking.Id ? HouseholdChecking.Name : CashWallet.Name;
            this.transactions.Add(new ExpenseTransactionResponse(Guid.NewGuid(), request.AccountId, accountName, ledgerDate, request.Amount, request.Classification));
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
            this.LastUpdateRequest = request;
            var index = this.transactions.FindIndex(
                transaction => transaction.Date == ledgerDate && transaction.Id == transactionId);
            if (index < 0)
            {
                return Task.FromResult(TransactionMutationResult.NotFound);
            }

            this.transactions[index] = this.transactions[index] with
            {
                AccountId = request.AccountId,
                AccountName = request.AccountId == HouseholdChecking.Id ? HouseholdChecking.Name : CashWallet.Name,
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

    private sealed class StubAccountsApiClient(IReadOnlyList<AccountResponse> accounts) : IAccountsApiClient
    {
        public bool FailNextList { get; set; }

        public int ListCallCount { get; private set; }

        public Task<AccountCreationResult> CreateAsync(
            CreateAccountRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ListCallCount++;
            if (this.FailNextList)
            {
                this.FailNextList = false;
                throw new HttpRequestException("Unavailable");
            }

            return Task.FromResult(accounts);
        }
    }

    private sealed class ControllableAccountsApiClient(IReadOnlyList<AccountResponse> initialAccounts) : IAccountsApiClient
    {
        private readonly Dictionary<int, TaskCompletionSource<IReadOnlyList<AccountResponse>>> requests = [];

        public int ListCallCount { get; private set; }

        public Task<AccountCreationResult> CreateAsync(
            CreateAccountRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken)
        {
            this.ListCallCount++;
            if (this.ListCallCount == 1)
            {
                return Task.FromResult(initialAccounts);
            }

            var completion = new TaskCompletionSource<IReadOnlyList<AccountResponse>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            this.requests.Add(this.ListCallCount, completion);
            return completion.Task;
        }

        public void Complete(int callNumber, IReadOnlyList<AccountResponse> accounts)
        {
            this.requests[callNumber].SetResult(accounts);
        }
    }

    private sealed class ControllableTransactionsApiClient(
        DateOnly initialDate,
        IReadOnlyList<ExpenseTransactionResponse> initialTransactions) : ITransactionsApiClient
    {
        private readonly Dictionary<DateOnly, TaskCompletionSource<IReadOnlyList<ExpenseTransactionResponse>>> requests = [];

        public Task<bool> CreateAsync(
            DateOnly ledgerDate,
            CreateExpenseTransactionRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTransactionResponse>> ListAsync(
            DateOnly ledgerDate,
            CancellationToken cancellationToken)
        {
            if (ledgerDate == initialDate)
            {
                return Task.FromResult(initialTransactions);
            }

            var completion = new TaskCompletionSource<IReadOnlyList<ExpenseTransactionResponse>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            this.requests.Add(ledgerDate, completion);
            return completion.Task;
        }

        public Task<TransactionMutationResult> ReviseAsync(
            DateOnly ledgerDate,
            Guid transactionId,
            UpdateExpenseTransactionRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TransactionMutationResult> RemoveAsync(
            DateOnly ledgerDate,
            Guid transactionId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public void Complete(DateOnly ledgerDate, IReadOnlyList<ExpenseTransactionResponse> transactions)
        {
            this.requests[ledgerDate].SetResult(transactions);
        }
    }
}
