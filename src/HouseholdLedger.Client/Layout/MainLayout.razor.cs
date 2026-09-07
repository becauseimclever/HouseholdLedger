// <copyright file="MainLayout.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Layout;

using System.Text.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Provides the application-wide navigation and content frame.
/// </summary>
public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private IReadOnlyList<AccountResponse> accounts = [];
    private long accountRequestVersion;
    private bool accountsBranchExpanded;
    private bool accountsLoadError;
    private bool accountsLoading = true;
    private bool inspectorExpanded = true;
    private bool navigationExpanded = true;

    private bool AccountRouteSelected => this.CurrentPath.StartsWith("accounts/", StringComparison.OrdinalIgnoreCase);

    private string AccountsToggleLabel => this.accountsBranchExpanded ? "Collapse accounts" : "Expand accounts";

    private string CurrentPath => this.Navigation.ToBaseRelativePath(this.Navigation.Uri).Split('?', '#')[0].Trim('/');

    private bool InspectorAvailable => string.IsNullOrEmpty(this.Navigation.ToBaseRelativePath(this.Navigation.Uri).Split('?', '#')[0].Trim('/'));

    private string InspectorToggleLabel => this.inspectorExpanded ? "Collapse inspector" : "Expand inspector";

    private string NavigationToggleLabel => this.navigationExpanded ? "Collapse navigation" : "Expand navigation";

    /// <summary>Gets or sets the account API client.</summary>
    [Inject]
    private IAccountsApiClient AccountsApiClient { get; set; } = null!;

    /// <summary>Gets or sets the shared account-catalog state.</summary>
    [Inject]
    private AccountCatalogState AccountCatalogState { get; set; } = null!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    /// <summary>Gets or sets the global display-currency state.</summary>
    [CascadingParameter]
    private GlobalSettingsState? DisplayCurrency { get; set; }

    /// <summary>Gets or sets the selected-date state.</summary>
    [Inject]
    private SelectedDateState SelectedDate { get; set; } = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.lifetimeCancellation.Cancel();
        this.lifetimeCancellation.Dispose();
        this.AccountCatalogState.Changed -= this.RefreshAccounts;
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed -= this.RefreshCurrency;
        }

        this.Navigation.LocationChanged -= this.HandleLocationChanged;
        this.SelectedDate.Changed -= this.OpenInspector;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        this.accountsBranchExpanded = this.AccountRouteSelected;
        this.AccountCatalogState.Changed += this.RefreshAccounts;
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed += this.RefreshCurrency;
            _ = this.DisplayCurrency.EnsureLoadedAsync(this.lifetimeCancellation.Token);
        }

        this.Navigation.LocationChanged += this.HandleLocationChanged;
        this.SelectedDate.Changed += this.OpenInspector;
        _ = this.LoadAccountsAsync();
    }

    private static bool IsApiFailure(Exception exception) =>
        exception is HttpRequestException or JsonException or NotSupportedException
        || exception is OperationCanceledException;

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        _ = sender;
        _ = args;

        if (this.AccountRouteSelected)
        {
            this.accountsBranchExpanded = true;
        }

        _ = this.InvokeAsync(this.StateHasChanged);
    }

    private async Task LoadAccountsAsync()
    {
        var requestVersion = ++this.accountRequestVersion;
        this.accountsLoading = true;
        this.accountsLoadError = false;

        try
        {
            var response = await this.AccountsApiClient.ListAsync(this.lifetimeCancellation.Token);
            if (requestVersion == this.accountRequestVersion)
            {
                this.accounts = response;
            }
        }
        catch (Exception exception) when (IsApiFailure(exception))
        {
            if (requestVersion == this.accountRequestVersion
                && !this.lifetimeCancellation.IsCancellationRequested)
            {
                this.accounts = [];
                this.accountsLoadError = true;
            }
        }
        finally
        {
            if (requestVersion == this.accountRequestVersion
                && !this.lifetimeCancellation.IsCancellationRequested)
            {
                this.accountsLoading = false;
                await this.InvokeAsync(this.StateHasChanged);
            }
        }
    }

    private void OpenInspector(DateOnly ledgerDate)
    {
        _ = ledgerDate;
        this.inspectorExpanded = true;
        _ = this.InvokeAsync(this.StateHasChanged);
    }

    private void RefreshAccounts()
    {
        _ = this.InvokeAsync(this.LoadAccountsAsync);
    }

    private void RefreshCurrency()
    {
        _ = this.InvokeAsync(this.StateHasChanged);
    }

    private void ToggleAccounts()
    {
        this.accountsBranchExpanded = !this.accountsBranchExpanded;
    }

    private void ToggleInspector()
    {
        this.inspectorExpanded = !this.inspectorExpanded;
    }

    private void ToggleNavigation()
    {
        this.navigationExpanded = !this.navigationExpanded;
    }
}
