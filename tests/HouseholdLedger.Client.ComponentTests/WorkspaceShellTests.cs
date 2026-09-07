// <copyright file="WorkspaceShellTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Layout;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Verifies the accessible workspace shell around the calendar surface.
/// </summary>
public sealed class WorkspaceShellTests
{
    /// <summary>
    /// Verifies that the expanded shell presents independently controlled, neutral panes.
    /// </summary>
    [Fact]
    public void ExpandedShellHasAccessibleNeutralPanesAndIndependentNativeToggles()
    {
        using var context = new BunitContext();
        RegisterShellServices(context, new StubAccountsApiClient([]));

        var component = context.Render<MainLayout>();
        var navigationToggle = component.Find("button[aria-controls='workspace-navigation']");
        var inspectorToggle = component.Find("button[aria-controls='workspace-inspector']");

        Assert.Multiple(
            () => Assert.Equal("/", component.Find("a.workspace-name").GetAttribute("href")),
            () => Assert.Equal("HouseholdLedger home", component.Find("a.workspace-name").GetAttribute("aria-label")),
            () => Assert.Single(component.FindAll("nav#workspace-navigation")),
            () => Assert.Single(component.FindAll("section#workspace-main")),
            () => Assert.Single(component.FindAll("aside#workspace-inspector")),
            () => Assert.Equal("© 2026 HouseholdLedger", component.Find(".workspace-copyright").TextContent),
            () => Assert.Equal("/open-source-notices", component.Find("footer .workspace-auxiliary-link").GetAttribute("href")),
            () => Assert.Equal("Navigation", component.Find("#navigation-heading").TextContent),
            () => Assert.Equal("Inspector", component.Find("#inspector-heading").TextContent),
            () => Assert.Equal("No calendar item selected", component.Find(".inspector-empty-state").TextContent),
            () => Assert.Equal(3, component.FindAll("#workspace-navigation a").Count),
            () => Assert.Equal("Home", component.FindAll("#workspace-navigation .navigation-label")[0].TextContent),
            () => Assert.Equal("/", component.FindAll("#workspace-navigation a")[0].GetAttribute("href")),
            () => Assert.Equal("Accounts", component.FindAll("#workspace-navigation .navigation-label")[1].TextContent),
            () => Assert.Equal("/accounts", component.FindAll("#workspace-navigation a")[1].GetAttribute("href")),
            () => Assert.Equal("Settings", component.FindAll("#workspace-navigation .navigation-label")[2].TextContent),
            () => Assert.Equal("/settings", component.FindAll("#workspace-navigation a")[2].GetAttribute("href")),
            () => Assert.Empty(component.FindAll(".workspace-toolbar button[aria-controls='workspace-navigation']")),
            () => Assert.Single(component.FindAll("#workspace-navigation button[aria-controls='workspace-navigation']")),
            () => Assert.Equal("button", navigationToggle.GetAttribute("type")),
            () => Assert.Equal("true", navigationToggle.GetAttribute("aria-expanded")),
            () => Assert.Equal("Collapse navigation", navigationToggle.GetAttribute("aria-label")),
            () => Assert.Equal("button", inspectorToggle.GetAttribute("type")),
            () => Assert.Equal("true", inspectorToggle.GetAttribute("aria-expanded")),
            () => Assert.Equal("Collapse inspector", inspectorToggle.GetAttribute("aria-label")),
            () => Assert.Equal(2, component.FindAll("button .pane-toggle-tooltip[role='tooltip']").Count),
            () => Assert.Equal(2, component.FindAll("button.pane-toggle > span[aria-hidden='true']").Count));

        navigationToggle.Click();

        Assert.Multiple(
            () => Assert.False(component.Find("#workspace-navigation").HasAttribute("hidden")),
            () => Assert.Contains("navigation-pane-collapsed", component.Find("#workspace-navigation").ClassList),
            () => Assert.Equal("Navigation", component.Find("#workspace-navigation").GetAttribute("aria-label")),
            () => Assert.Empty(component.FindAll("#navigation-heading")),
            () => Assert.Equal(3, component.FindAll("#workspace-navigation a").Count),
            () => Assert.All(component.FindAll("#workspace-navigation a"), link => Assert.NotNull(link.GetAttribute("aria-label"))),
            () => Assert.All(component.FindAll("#workspace-navigation .navigation-label"), label => Assert.Contains("navigation-label", label.ClassList)),
            () => Assert.Equal("false", navigationToggle.GetAttribute("aria-expanded")),
            () => Assert.Equal("Expand navigation", navigationToggle.GetAttribute("aria-label")),
            () => Assert.False(component.Find("#workspace-inspector").HasAttribute("hidden")),
            () => Assert.Equal("true", inspectorToggle.GetAttribute("aria-expanded")));

        navigationToggle.Click();

        Assert.Multiple(
            () => Assert.DoesNotContain("navigation-pane-collapsed", component.Find("#workspace-navigation").ClassList),
            () => Assert.Equal("Navigation", component.Find("#navigation-heading").TextContent),
            () => Assert.Equal(3, component.FindAll("#workspace-navigation a").Count),
            () => Assert.Equal("true", navigationToggle.GetAttribute("aria-expanded")),
            () => Assert.Equal("Collapse navigation", navigationToggle.GetAttribute("aria-label")));

        inspectorToggle.Click();

        Assert.Multiple(
            () => Assert.False(component.Find("#workspace-navigation").HasAttribute("hidden")),
            () => Assert.False(component.Find("#workspace-inspector").HasAttribute("hidden")),
            () => Assert.Contains("inspector-pane-collapsed", component.Find("#workspace-inspector").ClassList),
            () => Assert.Empty(component.FindAll("#inspector-heading")),
            () => Assert.Empty(component.FindAll(".inspector-empty-state")),
            () => Assert.Equal("false", inspectorToggle.GetAttribute("aria-expanded")),
            () => Assert.Equal("Expand inspector", inspectorToggle.GetAttribute("aria-label")));
    }

    /// <summary>Verifies the workspace name is a native Home link with current-page semantics.</summary>
    [Fact]
    public void WorkspaceNameLinksHomeAndExposesCurrentState()
    {
        using var context = new BunitContext();
        RegisterShellServices(context, new StubAccountsApiClient([]));
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/settings");
        var component = context.Render<MainLayout>();
        var workspaceName = component.Find("a.workspace-name");

        Assert.Null(workspaceName.GetAttribute("aria-current"));
        navigation.NavigateTo(workspaceName.GetAttribute("href")!);

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.EndsWith("/", navigation.Uri, StringComparison.Ordinal),
            () => Assert.Equal("page", component.Find("a.workspace-name").GetAttribute("aria-current")),
            () => Assert.Equal("page", component.Find(".workspace-navigation-link[href='/']").GetAttribute("aria-current"))));
    }

    /// <summary>Verifies exactly one primary destination is current on owned routes.</summary>
    /// <param name="route">The current route.</param>
    /// <param name="expectedLabel">The expected current navigation label.</param>
    [Theory]
    [InlineData("/", "Home")]
    [InlineData("/accounts", "Accounts")]
    [InlineData("/accounts/10000000-0000-0000-0000-000000000001", "Household Checking")]
    public void NavigationExposesExactlyOneCurrentDestination(string route, string expectedLabel)
    {
        using var context = new BunitContext();
        RegisterShellServices(
            context,
            new StubAccountsApiClient([new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Household Checking")]));
        context.Services.GetRequiredService<NavigationManager>().NavigateTo(route);

        var component = context.Render<MainLayout>();
        component.WaitForAssertion(() =>
        {
            var current = Assert.Single(component.FindAll("#workspace-navigation a[aria-current='page']"));
            Assert.Equal(expectedLabel, current.GetAttribute("aria-label"));
        });
    }

    /// <summary>Verifies that only pages with inspector content expose the shared pane and its control.</summary>
    [Fact]
    public void InspectorIsAvailableOnlyOnPagesThatNeedIt()
    {
        using var context = new BunitContext();
        RegisterShellServices(context, new StubAccountsApiClient([]));
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var component = context.Render<MainLayout>();

        Assert.Multiple(
            () => Assert.Single(component.FindAll("button[aria-controls='workspace-inspector']")),
            () => Assert.False(component.Find("#workspace-inspector").HasAttribute("hidden")));

        navigation.NavigateTo("/accounts");

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Empty(component.FindAll("button[aria-controls='workspace-inspector']")),
            () => Assert.Empty(component.FindAll("#workspace-inspector"))));

        navigation.NavigateTo("/");

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Single(component.FindAll("button[aria-controls='workspace-inspector']")),
            () => Assert.False(component.Find("#workspace-inspector").HasAttribute("hidden"))));
    }

    /// <summary>Verifies Accounts navigation and disclosure remain independent native controls.</summary>
    [Fact]
    public void AccountsLinkAndDisclosureAreIndependentAndOrdered()
    {
        using var context = new BunitContext();
        var accounts = new AccountResponse[]
        {
            new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Cash Wallet"),
            new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Household Checking"),
        };
        RegisterShellServices(context, new StubAccountsApiClient(accounts));
        var component = context.Render<MainLayout>();
        component.WaitForAssertion(() => Assert.Equal(1, context.Services.GetRequiredService<StubAccountsApiClient>().ListCalls));

        var accountsLink = component.Find(".accounts-navigation-heading a");
        var disclosure = component.Find("button[aria-controls='accounts-navigation-list']");
        Assert.Multiple(
            () => Assert.Equal("/accounts", accountsLink.GetAttribute("href")),
            () => Assert.Equal("Accounts", accountsLink.GetAttribute("aria-label")),
            () => Assert.Equal("button", disclosure.GetAttribute("type")),
            () => Assert.Equal("Expand accounts", disclosure.GetAttribute("aria-label")),
            () => Assert.Equal("false", disclosure.GetAttribute("aria-expanded")),
            () => Assert.Empty(component.FindAll("#accounts-navigation-list")));

        disclosure.Click();

        var accountLinks = component.FindAll(".account-navigation-link");
        Assert.Multiple(
            () => Assert.Equal("true", disclosure.GetAttribute("aria-expanded")),
            () => Assert.Equal("Collapse accounts", disclosure.GetAttribute("aria-label")),
            () => Assert.Equal(2, accountLinks.Count),
            () => Assert.StartsWith("Cash Wallet", accountLinks[0].TextContent.Trim(), StringComparison.Ordinal),
            () => Assert.Equal("/accounts/10000000-0000-0000-0000-000000000002", accountLinks[0].GetAttribute("href")),
            () => Assert.StartsWith("Household Checking", accountLinks[1].TextContent.Trim(), StringComparison.Ordinal),
            () => Assert.Equal("Household Checking", accountLinks[1].QuerySelector(".account-navigation-name")!.GetAttribute("title")),
            () => Assert.Equal("/accounts/10000000-0000-0000-0000-000000000001", accountLinks[1].GetAttribute("href")));

        context.Services.GetRequiredService<NavigationManager>().NavigateTo(accountsLink.GetAttribute("href")!);

        Assert.Multiple(
            () => Assert.EndsWith("/accounts", context.Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal),
            () => Assert.Equal("true", disclosure.GetAttribute("aria-expanded")));

        component.Find("button[aria-controls='workspace-navigation']").Click();
        component.Find("button[aria-controls='workspace-navigation']").Click();

        Assert.Equal(
            "true",
            component.Find("button[aria-controls='accounts-navigation-list']").GetAttribute("aria-expanded"));
    }

    /// <summary>Verifies direct account navigation expands and marks the matching hierarchy.</summary>
    [Fact]
    public void DirectAccountRouteExpandsAndMarksCurrentHierarchy()
    {
        using var context = new BunitContext();
        var account = new AccountResponse(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Household Checking");
        RegisterShellServices(context, new StubAccountsApiClient([account]));
        context.Services.GetRequiredService<NavigationManager>().NavigateTo($"/accounts/{account.Id}");

        var component = context.Render<MainLayout>();

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.Equal("true", component.Find("button[aria-controls='accounts-navigation-list']").GetAttribute("aria-expanded")),
            () => Assert.Contains("current-navigation-branch", component.Find(".accounts-navigation-branch").ClassList),
            () => Assert.Equal("page", component.Find(".account-navigation-link").GetAttribute("aria-current")),
            () => Assert.Equal("Current", component.Find(".current-account-marker").TextContent)));
    }

    /// <summary>Verifies catalog errors remain retryable and stale responses cannot replace current data.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task AccountCatalogStatesRetryAndRejectStaleResponses()
    {
        using var context = new BunitContext();
        var apiClient = new DeferredAccountsApiClient();
        RegisterShellServices(context, apiClient);
        var component = context.Render<MainLayout>();
        component.Find("button[aria-controls='accounts-navigation-list']").Click();
        Assert.Equal("Loading accounts", component.Find(".account-navigation-state").TextContent);

        apiClient.Requests[0].Completion.SetException(new HttpRequestException("Unavailable"));
        await component.InvokeAsync(() => Task.CompletedTask);
        component.WaitForElement(".account-navigation-error button");
        component.Find("button[aria-controls='workspace-navigation']").Click();
        var retry = component.Find(".account-navigation-error button");
        Assert.Multiple(
            () => Assert.Equal("Try loading accounts again", retry.GetAttribute("aria-label")),
            () => Assert.Equal("Try loading accounts again", retry.GetAttribute("title")),
            () => Assert.Equal(3, component.FindAll("#workspace-navigation a").Count));
        retry.Click();
        component.WaitForAssertion(() => Assert.Equal(2, apiClient.Requests.Count));

        var latest = new AccountResponse(Guid.NewGuid(), "Latest account");
        context.Services.GetRequiredService<AccountCatalogState>().NotifyChanged();
        component.WaitForAssertion(() => Assert.Equal(3, apiClient.Requests.Count));
        apiClient.Requests[2].Completion.SetResult([latest]);
        await component.InvokeAsync(() => Task.CompletedTask);
        component.WaitForAssertion(() => Assert.Contains("Latest account", component.Find(".account-navigation-link").TextContent, StringComparison.Ordinal));

        apiClient.Requests[1].Completion.SetResult([new(Guid.NewGuid(), "Obsolete account")]);
        await component.InvokeAsync(() => Task.CompletedTask);

        Assert.DoesNotContain("Obsolete account", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies pane collapse does not reset the transient Accounts branch state.</summary>
    [Fact]
    public void PaneCollapsePreservesAccountsBranchState()
    {
        using var context = new BunitContext();
        RegisterShellServices(context, new StubAccountsApiClient([]));
        var component = context.Render<MainLayout>();
        component.Find("button[aria-controls='accounts-navigation-list']").Click();

        component.Find("button[aria-controls='workspace-navigation']").Click();
        component.Find("button[aria-controls='workspace-navigation']").Click();

        Assert.Multiple(
            () => Assert.Equal("true", component.Find("button[aria-controls='accounts-navigation-list']").GetAttribute("aria-expanded")),
            () => Assert.Equal("No accounts yet.", component.Find(".account-navigation-state").TextContent));
    }

    /// <summary>Verifies collapsed navigation retains named icon links and exact current state.</summary>
    [Fact]
    public void CollapsedNavigationRetainsIconLinksForExpandedAccountHierarchy()
    {
        using var context = new BunitContext();
        var account = new AccountResponse(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Household Checking");
        RegisterShellServices(context, new StubAccountsApiClient([account]));
        context.Services.GetRequiredService<NavigationManager>().NavigateTo($"/accounts/{account.Id}");
        var component = context.Render<MainLayout>();

        component.WaitForElement(".account-navigation-link");
        component.Find("button[aria-controls='workspace-navigation']").Click();
        var disclosure = component.Find("button[aria-controls='accounts-navigation-list']");

        Assert.Multiple(
            () => Assert.Equal(4, component.FindAll("#workspace-navigation a").Count),
            () => Assert.Equal(4, component.FindAll("#workspace-navigation .navigation-icon").Count),
            () => Assert.Equal("true", disclosure.GetAttribute("aria-expanded")),
            () => Assert.Equal("Collapse accounts", disclosure.GetAttribute("aria-label")),
            () => Assert.Equal("Collapse accounts", disclosure.GetAttribute("title")),
            () => Assert.Equal("page", component.Find(".account-navigation-link").GetAttribute("aria-current")),
            () => Assert.Equal("Household Checking", component.Find(".account-navigation-link").GetAttribute("aria-label")),
            () => Assert.Equal("Household Checking", component.Find(".account-navigation-link").GetAttribute("title")));

        disclosure.Click();

        Assert.Multiple(
            () => Assert.Empty(component.FindAll(".account-navigation-link")),
            () => Assert.Equal("false", disclosure.GetAttribute("aria-expanded")),
            () => Assert.Equal("Expand accounts", disclosure.GetAttribute("aria-label")));
    }

    /// <summary>Verifies selecting a date reveals a collapsed inspector without changing navigation.</summary>
    [Fact]
    public void SelectedDateAutomaticallyRevealsCollapsedInspector()
    {
        using var context = new BunitContext();
        RegisterShellServices(context, new StubAccountsApiClient([]));
        context.Services.AddSingleton<ITransactionsApiClient>(new StubTransactionsApiClient());
        var component = context.Render<MainLayout>();
        component.Find("button[aria-controls='workspace-navigation']").Click();
        component.Find("button[aria-controls='workspace-inspector']").Click();

        context.Services.GetRequiredService<SelectedDateState>().Select(new DateOnly(2026, 9, 6));

        component.WaitForAssertion(() => Assert.Multiple(
            () => Assert.DoesNotContain("inspector-pane-collapsed", component.Find("#workspace-inspector").ClassList),
            () => Assert.Equal("true", component.Find("button[aria-controls='workspace-inspector']").GetAttribute("aria-expanded")),
            () => Assert.Contains("navigation-pane-collapsed", component.Find("#workspace-navigation").ClassList)));
    }

    private static void RegisterShellServices(BunitContext context, IAccountsApiClient accountsApiClient)
    {
        context.Services.AddScoped<AccountCatalogState>();
        context.Services.AddScoped<SelectedDateState>();
        context.Services.AddSingleton(accountsApiClient);
        if (accountsApiClient is StubAccountsApiClient stub)
        {
            context.Services.AddSingleton(stub);
        }
    }

    private sealed class StubAccountsApiClient(IReadOnlyList<AccountResponse> accounts) : IAccountsApiClient
    {
        public int ListCalls { get; private set; }

        public Task<AccountCreationResult> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ListCalls++;
            return Task.FromResult(accounts);
        }
    }

    private sealed class DeferredAccountsApiClient : IAccountsApiClient
    {
        public List<AccountRequest> Requests { get; } = [];

        public Task<AccountCreationResult> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken)
        {
            var request = new AccountRequest();
            this.Requests.Add(request);
            return request.Completion.Task;
        }
    }

    private sealed class StubTransactionsApiClient : ITransactionsApiClient
    {
        public Task<bool> CreateAsync(
            DateOnly ledgerDate,
            CreateExpenseTransactionRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTransactionResponse>> ListAsync(
            DateOnly ledgerDate,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ExpenseTransactionResponse>>([]);

        public Task<TransactionMutationResult> RemoveAsync(
            DateOnly ledgerDate,
            Guid transactionId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TransactionMutationResult> ReviseAsync(
            DateOnly ledgerDate,
            Guid transactionId,
            UpdateExpenseTransactionRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed record AccountRequest
    {
        public TaskCompletionSource<IReadOnlyList<AccountResponse>> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
