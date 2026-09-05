// <copyright file="AccountsPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Pages;
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
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient(Array.Empty<AccountResponse>()));

        var component = context.Render<AccountsPage>();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("No accounts yet.", component.Find(".accounts-load-state").TextContent),
            () => Assert.Empty(component.FindAll("article.account-card")),
            () => Assert.Single(component.FindAll(".new-account-card")),
            () => Assert.Equal("Name", component.Find("label[for='account-name']").TextContent),
            () => Assert.Equal("submit", component.Find("button").GetAttribute("type"))));
    }

    /// <summary>Verifies persisted accounts remain in backend order and are not premature links.</summary>
    [Fact]
    public void PopulatedCatalogRendersAccountCardsInBackendOrder()
    {
        using var context = new BunitContext();
        var accounts = new AccountResponse[]
        {
            new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Cash Wallet"),
            new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Household Checking"),
        };
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient(accounts));

        var component = context.Render<AccountsPage>();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal(2, component.FindAll("article.account-card").Count),
            () => Assert.Equal("Cash Wallet", component.FindAll("article.account-card h2")[0].TextContent),
            () => Assert.Equal("Household Checking", component.FindAll("article.account-card h2")[1].TextContent),
            () => Assert.Empty(component.FindAll("article.account-card a"))));
    }

    /// <summary>Verifies blank names fail locally without an API mutation.</summary>
    [Fact]
    public void BlankNameShowsAccessibleFieldErrorWithoutCallingApi()
    {
        using var context = new BunitContext();
        var apiClient = new StubAccountsApiClient(Array.Empty<AccountResponse>());
        context.Services.AddSingleton<IAccountsApiClient>(apiClient);
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
        var created = new AccountResponse(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "Household Checking");
        var apiClient = new StubAccountsApiClient(Array.Empty<AccountResponse>(), [created]);
        context.Services.AddSingleton<IAccountsApiClient>(apiClient);
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
            () => Assert.Equal("Account created.", component.Find(".success-state").TextContent)));
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
}
