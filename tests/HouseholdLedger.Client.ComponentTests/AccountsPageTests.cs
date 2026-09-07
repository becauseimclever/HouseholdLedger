// <copyright file="AccountsPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Pages;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>Verifies the account-management page.</summary>
public sealed class AccountsPageTests
{
    /// <summary>Verifies an empty backend catalog renders only the creation card.</summary>
    [Fact]
    public void EmptyCatalogRendersTruthfulStateAndCreationCard()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<AccountCatalogState>();
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient(Array.Empty<AccountResponse>()));
        context.Services.AddSingleton<IAccountTransactionsApiClient>(new StubAccountTransactionsApiClient());

        var component = context.Render<AccountsPage>();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("No accounts yet.", component.Find(".accounts-load-state").TextContent),
            () => Assert.Empty(component.FindAll("article.account-card")),
            () => Assert.Single(component.FindAll(".new-account-card")),
            () => Assert.Equal("Name", component.Find("label[for='account-name']").TextContent),
            () => Assert.Equal("submit", component.Find("button").GetAttribute("type"))));
    }

    /// <summary>Verifies persisted accounts remain in backend order and use native detail links.</summary>
    [Fact]
    public void PopulatedCatalogRendersAccountCardsInBackendOrder()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<AccountCatalogState>();
        var accounts = new AccountResponse[]
        {
            new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Cash Wallet"),
            new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Household Checking"),
        };
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient(accounts));
        context.Services.AddSingleton<IAccountTransactionsApiClient>(new StubAccountTransactionsApiClient());

        var component = context.Render<AccountsPage>();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal(2, component.FindAll("article.account-card").Count),
            () => Assert.Equal("Cash Wallet", component.FindAll("article.account-card h2")[0].TextContent),
            () => Assert.Equal("Household Checking", component.FindAll("article.account-card h2")[1].TextContent),
            () => Assert.Equal("/accounts/10000000-0000-0000-0000-000000000002", component.FindAll("article.account-card a")[0].GetAttribute("href")),
            () => Assert.Equal("/accounts/10000000-0000-0000-0000-000000000001", component.FindAll("article.account-card a")[1].GetAttribute("href"))));
    }

    /// <summary>Verifies blank names fail locally without an API mutation.</summary>
    [Fact]
    public void BlankNameShowsAccessibleFieldErrorWithoutCallingApi()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<AccountCatalogState>();
        var apiClient = new StubAccountsApiClient(Array.Empty<AccountResponse>());
        context.Services.AddSingleton<IAccountsApiClient>(apiClient);
        context.Services.AddSingleton<IAccountTransactionsApiClient>(new StubAccountTransactionsApiClient());
        var component = context.Render<AccountsPage>();
        component.WaitForElement("form");

        component.Find("#account-name").Input("   ");
        component.Find("form").Submit();

        Assert.Multiple(
            () => Assert.Equal("true", component.Find("#account-name").GetAttribute("aria-invalid")),
            () => Assert.Equal("account-name-error", component.Find("#account-name").GetAttribute("aria-describedby")),
            () => Assert.Contains("Enter an account name", component.Find("#account-name-error").TextContent, StringComparison.Ordinal),
            () => Assert.Equal(0, apiClient.CreateCalls));
    }

    /// <summary>Verifies successful creation rereads backend state before announcing success.</summary>
    [Fact]
    public void ValidCreationUsesTrimmedNameAndRendersAuthoritativeReread()
    {
        using var context = new BunitContext();
        var accountCatalogState = new AccountCatalogState();
        context.Services.AddSingleton(accountCatalogState);
        var catalogChanges = 0;
        accountCatalogState.Changed += () => catalogChanges++;
        var created = new AccountResponse(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "Household Checking");
        var apiClient = new StubAccountsApiClient(Array.Empty<AccountResponse>(), [created]);
        context.Services.AddSingleton<IAccountsApiClient>(apiClient);
        context.Services.AddSingleton<IAccountTransactionsApiClient>(new StubAccountTransactionsApiClient());
        var component = context.Render<AccountsPage>();
        component.WaitForElement("form");

        component.Find("#account-name").Input("  Household Checking  ");
        component.Find("form").Submit();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal(1, apiClient.CreateCalls),
            () => Assert.Equal(2, apiClient.ListCalls),
            () => Assert.Equal("Household Checking", apiClient.LastRequest!.Name),
            () => Assert.Equal("Household Checking", component.Find("article.account-card h2").TextContent),
            () => Assert.Equal(string.Empty, component.Find("#account-name").GetAttribute("value")),
            () => Assert.Equal("Account created.", component.Find(".success-state").TextContent),
            () => Assert.Equal(1, catalogChanges)));
    }

    /// <summary>Verifies route selection and accessible transaction table content.</summary>
    [Fact]
    public void SelectedAccountRendersCurrentCardAndCompleteHistory()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<AccountCatalogState>();
        var account = new AccountResponse(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Household Checking");
        var history = new AccountTransactionHistoryResponse(
            account,
            [
                new(Guid.NewGuid(), account.Id, account.Name, new DateOnly(2026, 9, 30), 40m, "Unexpected"),
                new(Guid.NewGuid(), account.Id, account.Name, new DateOnly(2026, 9, 3), 6.50m, "Optional"),
            ]);
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([account]));
        context.Services.AddSingleton<IAccountTransactionsApiClient>(new StubAccountTransactionsApiClient(history));

        var component = context.Render<AccountsPage>(parameters => parameters.Add(page => page.AccountId, account.Id));

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("page", component.Find("article.account-card a").GetAttribute("aria-current")),
            () => Assert.Equal("Selected", component.Find(".selected-account-marker").TextContent),
            () => Assert.Equal(account.Name, component.Find("#account-history-heading").TextContent),
            () => Assert.Equal(2, component.FindAll("tbody tr").Count),
            () => Assert.Equal("Sep 30, 2026", component.FindAll("tbody tr")[0].Children[0].TextContent),
            () => Assert.Equal("$40.00", component.FindAll("tbody tr")[0].Children[1].TextContent),
            () => Assert.Equal("Unexpected", component.FindAll("tbody tr")[0].Children[2].TextContent),
            () => Assert.Equal($"Transactions for {account.Name}", component.Find("caption").TextContent)));
    }

    /// <summary>Verifies an existing history view responds to the shared display-currency state.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CurrencyChangeRerendersVisibleHistoryAndFilterLabels()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<AccountCatalogState>();
        var account = new AccountResponse(Guid.NewGuid(), "Household Checking");
        var history = new AccountTransactionHistoryResponse(
            account,
            [new(Guid.NewGuid(), account.Id, account.Name, new DateOnly(2026, 9, 3), 82.45m, "Optional")]);
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([account]));
        context.Services.AddSingleton<IAccountTransactionsApiClient>(new StubAccountTransactionsApiClient(history));
        using var currencyState = new GlobalSettingsState(new StubGlobalSettingsApiClient());

        var component = context.Render<CascadingValue<GlobalSettingsState>>(parameters => parameters
            .Add(value => value.Value, currencyState)
            .AddChildContent<AccountsPage>(childParameters => childParameters.Add(page => page.AccountId, account.Id)));

        component.WaitForAssertion(() => Assert.Equal("$82.45", component.Find("tbody tr").Children[1].TextContent));
        await currencyState.SaveCurrencyAsync("EUR", CancellationToken.None);

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("82,45 €", component.Find("tbody tr").Children[1].TextContent),
            () => Assert.Equal("Minimum amount (EUR)", component.Find("label[for='filter-minimum-amount']").TextContent),
            () => Assert.Equal("Maximum amount (EUR)", component.Find("label[for='filter-maximum-amount']").TextContent)));

        await currencyState.SaveCurrencyAsync("XXX", CancellationToken.None);

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("¤82.45", component.Find("tbody tr").Children[1].TextContent),
            () => Assert.Equal("Minimum amount (¤)", component.Find("label[for='filter-minimum-amount']").TextContent),
            () => Assert.Equal("Maximum amount (¤)", component.Find("label[for='filter-maximum-amount']").TextContent)));
    }

    /// <summary>Verifies empty, missing, and retryable unavailable states remain distinct.</summary>
    [Fact]
    public void AccountHistoryRendersDistinctHonestStatesAndRetries()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<AccountCatalogState>();
        var account = new AccountResponse(Guid.NewGuid(), "Rainy Day Savings");
        var historyClient = new StubAccountTransactionsApiClient(
            new HttpRequestException("Unavailable"),
            new AccountTransactionHistoryResponse(account, []),
            null);
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([account]));
        context.Services.AddSingleton<IAccountTransactionsApiClient>(historyClient);
        var component = context.Render<AccountsPage>(parameters => parameters.Add(page => page.AccountId, account.Id));

        component.WaitForElement(".history-state button").Click();
        component.WaitForAssertion(() => Assert.Equal(
            "No transactions recorded for this account.",
            component.Find(".account-history .history-state").TextContent));

        component.Render(parameters => parameters.Add(page => page.AccountId, Guid.NewGuid()));

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("Account not found", component.Find("#account-history-heading").TextContent),
            () => Assert.Empty(component.FindAll("tbody tr")),
            () => Assert.Equal(3, historyClient.GetCalls)));
    }

    /// <summary>Verifies an obsolete response cannot replace the latest route selection.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task RouteChangeRejectsStaleAccountHistoryResponse()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<AccountCatalogState>();
        var first = new AccountResponse(Guid.NewGuid(), "Household Checking");
        var second = new AccountResponse(Guid.NewGuid(), "Cash Wallet");
        var historyClient = new DeferredAccountTransactionsApiClient();
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([first, second]));
        context.Services.AddSingleton<IAccountTransactionsApiClient>(historyClient);
        var component = context.Render<AccountsPage>(parameters => parameters.Add(page => page.AccountId, first.Id));
        component.WaitForAssertion(() => Assert.Single(historyClient.Requests));

        component.Render(parameters => parameters.Add(page => page.AccountId, second.Id));
        component.WaitForAssertion(() => Assert.Equal(2, historyClient.Requests.Count));
        historyClient.Requests[1].Completion.SetResult(new AccountTransactionHistoryResponse(second, []));
        await component.InvokeAsync(() => Task.CompletedTask);
        component.WaitForAssertion(() => Assert.Equal(second.Name, component.Find("#account-history-heading").TextContent));

        historyClient.Requests[0].Completion.SetResult(new AccountTransactionHistoryResponse(first, []));
        await component.InvokeAsync(() => Task.CompletedTask);

        component.WaitForAssertion(() => Assert.Equal(second.Name, component.Find("#account-history-heading").TextContent));
    }

    /// <summary>Verifies URL criteria restore the form, reach the API, and report a filtered result count.</summary>
    [Fact]
    public void QueryCriteriaRestoreFilterFormAndResults()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<AccountCatalogState>();
        var account = new AccountResponse(Guid.NewGuid(), "Household Checking");
        var historyClient = new StubAccountTransactionsApiClient(new AccountTransactionHistoryResponse(
            account,
            [new(Guid.NewGuid(), account.Id, account.Name, new DateOnly(2026, 9, 3), 14m, "Culture")]));
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([account]));
        context.Services.AddSingleton<IAccountTransactionsApiClient>(historyClient);
        context.Services.GetRequiredService<NavigationManager>().NavigateTo(
            $"/accounts/{account.Id}?from=2026-09-01&classification=Culture&search=%20%2014%20%20");
        var component = context.Render<AccountsPage>(parameters => parameters.Add(page => page.AccountId, account.Id));

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("2026-09-01", component.Find("#filter-from").GetAttribute("value")),
            () => Assert.Equal("Culture", component.Find("#filter-classification").GetAttribute("value")),
            () => Assert.Equal("  14  ", component.Find("#filter-search").GetAttribute("value")),
            () => Assert.Equal("1 transaction", component.Find(".result-count").TextContent),
            () => Assert.Equal(new DateOnly(2026, 9, 1), historyClient.LastFilters!.From),
            () => Assert.Equal("14", historyClient.LastFilters!.Search)));
    }

    /// <summary>Verifies Apply normalizes the address and Clear restores complete history.</summary>
    [Fact]
    public void ApplyAndClearSynchronizeUrlAndRequests()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<AccountCatalogState>();
        var account = new AccountResponse(Guid.NewGuid(), "Household Checking");
        var historyClient = new StubAccountTransactionsApiClient(
            new AccountTransactionHistoryResponse(account, []),
            new AccountTransactionHistoryResponse(account, []),
            new AccountTransactionHistoryResponse(account, []));
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([account]));
        context.Services.AddSingleton<IAccountTransactionsApiClient>(historyClient);
        var component = context.Render<AccountsPage>(parameters => parameters.Add(page => page.AccountId, account.Id));
        component.WaitForElement(".transaction-filters");

        component.Find("#filter-from").Change("2026-09-01");
        component.Find("#filter-minimum-amount").Change("5.00");
        component.Find("#filter-search").Input("  culture  ");
        component.Find(".transaction-filters").Submit();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Contains("from=2026-09-01", context.Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal),
            () => Assert.Contains("minimumAmount=5.00", context.Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal),
            () => Assert.Contains("search=culture", context.Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal),
            () => Assert.Equal(5m, historyClient.LastFilters!.MinimumAmount),
            () => Assert.Equal("culture", historyClient.LastFilters!.Search),
            () => Assert.Equal("No transactions match these filters.", component.Find(".history-state").TextContent)));

        component.Find(".secondary-action").Click();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.DoesNotContain('?', context.Services.GetRequiredService<NavigationManager>().Uri),
            () => Assert.Null(historyClient.LastFilters!.MinimumAmount),
            () => Assert.Null(historyClient.LastFilters!.Search),
            () => Assert.Equal("No transactions recorded for this account.", component.Find(".history-state").TextContent)));
    }

    /// <summary>Verifies an invalid range remains distinct and does not issue an unbounded request.</summary>
    [Fact]
    public void InvalidFilterRangeShowsErrorWithoutRequest()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<AccountCatalogState>();
        var account = new AccountResponse(Guid.NewGuid(), "Household Checking");
        var historyClient = new StubAccountTransactionsApiClient(new AccountTransactionHistoryResponse(account, []));
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient([account]));
        context.Services.AddSingleton<IAccountTransactionsApiClient>(historyClient);
        var component = context.Render<AccountsPage>(parameters => parameters.Add(page => page.AccountId, account.Id));
        component.WaitForElement(".transaction-filters");

        component.Find("#filter-minimum-amount").Change("20");
        component.Find("#filter-maximum-amount").Change("10");
        component.Find(".transaction-filters").Submit();

        Assert.Multiple(
            () => Assert.Equal("Minimum amount must not exceed Maximum amount.", component.Find("#transaction-filter-error").TextContent),
            () => Assert.Equal("alert", component.Find("#transaction-filter-error").GetAttribute("role")),
            () => Assert.Equal(1, historyClient.GetCalls));
    }

    private sealed class StubAccountsApiClient(params IReadOnlyList<AccountResponse>[] responses) : IAccountsApiClient
    {
        private readonly Queue<IReadOnlyList<AccountResponse>> responses = new(responses);

        public int CreateCalls { get; private set; }

        public int ListCalls { get; private set; }

        public CreateAccountRequest? LastRequest { get; private set; }

        public Task<AccountCreationResult> CreateAsync(
            CreateAccountRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CreateCalls++;
            this.LastRequest = request;
            return Task.FromResult(AccountCreationResult.Success);
        }

        public Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ListCalls++;
            return Task.FromResult(this.responses.Dequeue());
        }
    }

    private sealed class StubAccountTransactionsApiClient(params object?[] responses) : IAccountTransactionsApiClient
    {
        private readonly Queue<object?> responses = new(responses);

        public int GetCalls { get; private set; }

        public AccountTransactionFilterRequest? LastFilters { get; private set; }

        public Task<AccountTransactionHistoryResponse?> GetAsync(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            return this.GetAsync(accountId, new AccountTransactionFilterRequest(), cancellationToken);
        }

        public Task<AccountTransactionHistoryResponse?> GetAsync(
            Guid accountId,
            AccountTransactionFilterRequest filters,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.GetCalls++;
            this.LastFilters = filters;
            if (this.responses.Count == 0)
            {
                return Task.FromResult<AccountTransactionHistoryResponse?>(null);
            }

            var response = this.responses.Dequeue();
            return response is Exception exception
                ? Task.FromException<AccountTransactionHistoryResponse?>(exception)
                : Task.FromResult((AccountTransactionHistoryResponse?)response);
        }
    }

    private sealed class StubGlobalSettingsApiClient : IGlobalSettingsApiClient
    {
        public Task<GlobalSettingsResponse> GetAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new GlobalSettingsResponse("USD", "workbench-dark"));
        }

        public Task<GlobalSettingsResponse> UpdateCurrencyAsync(
            string displayCurrency,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new GlobalSettingsResponse(displayCurrency, "workbench-dark"));
        }

        public Task<GlobalSettingsResponse> UpdateThemeAsync(
            string theme,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new GlobalSettingsResponse("USD", theme));
        }
    }

    private sealed class DeferredAccountTransactionsApiClient : IAccountTransactionsApiClient
    {
        public List<HistoryRequest> Requests { get; } = [];

        public Task<AccountTransactionHistoryResponse?> GetAsync(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            var request = new HistoryRequest(accountId);
            this.Requests.Add(request);
            return request.Completion.Task;
        }
    }

    private sealed record HistoryRequest(Guid AccountId)
    {
        public TaskCompletionSource<AccountTransactionHistoryResponse?> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
