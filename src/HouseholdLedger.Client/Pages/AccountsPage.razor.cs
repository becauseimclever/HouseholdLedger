// <copyright file="AccountsPage.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Pages;

using System.Globalization;
using System.Text.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Formatting;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;

/// <summary>Presents the authoritative account catalog and creation form.</summary>
public partial class AccountsPage : ComponentBase, IDisposable
{
    private static readonly CultureInfo DateCulture = CultureInfo.GetCultureInfo("en-US");
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private CancellationTokenSource? historyCancellation;
    private IReadOnlyList<AccountResponse> accounts = [];
    private AccountTransactionHistoryResponse? history;
    private long historyRequestVersion;
    private bool accountsLoaded;
    private bool historyLoadError;
    private bool historyNotFound;
    private bool isLoading = true;
    private bool isLoadingHistory;
    private bool isSaving;
    private bool loadError;
    private AccountTransactionFilterRequest filters = new();
    private string classificationText = string.Empty;
    private string fromText = string.Empty;
    private string maximumAmountText = string.Empty;
    private string minimumAmountText = string.Empty;
    private string nameText = string.Empty;
    private string? nameError;
    private string searchText = string.Empty;
    private string? saveError;
    private string? saveStatus;
    private string? filterError;
    private string toText = string.Empty;

    /// <summary>Gets or sets the account API client.</summary>
    [Inject]
    public IAccountsApiClient AccountsApiClient { get; set; } = null!;

    /// <summary>Gets or sets the account transaction API client.</summary>
    [Inject]
    public IAccountTransactionsApiClient AccountTransactionsApiClient { get; set; } = null!;

    /// <summary>Gets or sets the shared account-catalog state.</summary>
    [Inject]
    public AccountCatalogState AccountCatalogState { get; set; } = null!;

    /// <summary>Gets or sets the global display-currency state.</summary>
    [CascadingParameter]
    public GlobalSettingsState? DisplayCurrency { get; set; }

    /// <summary>Gets or sets the navigation service.</summary>
    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    /// <summary>Gets or sets the route-selected account identifier.</summary>
    [Parameter]
    public Guid? AccountId { get; set; }

    /// <summary>Gets or sets the inclusive first date supplied by the URL.</summary>
    [SupplyParameterFromQuery(Name = "from")]
    public string? FromQuery { get; set; }

    /// <summary>Gets or sets the inclusive final date supplied by the URL.</summary>
    [SupplyParameterFromQuery(Name = "to")]
    public string? ToQuery { get; set; }

    /// <summary>Gets or sets the classification supplied by the URL.</summary>
    [SupplyParameterFromQuery(Name = "classification")]
    public string? ClassificationQuery { get; set; }

    /// <summary>Gets or sets the minimum amount supplied by the URL.</summary>
    [SupplyParameterFromQuery(Name = "minimumAmount")]
    public string? MinimumAmountQuery { get; set; }

    /// <summary>Gets or sets the maximum amount supplied by the URL.</summary>
    [SupplyParameterFromQuery(Name = "maximumAmount")]
    public string? MaximumAmountQuery { get; set; }

    /// <summary>Gets or sets the search text supplied by the URL.</summary>
    [SupplyParameterFromQuery(Name = "search")]
    public string? SearchQuery { get; set; }

    private string CurrentCurrencyCode =>
        this.DisplayCurrency?.CurrentCode ?? MoneyFormatter.DefaultCurrencyCode;

    private string CurrentCurrencyLabel => MoneyFormatter.GetDisplayLabel(this.CurrentCurrencyCode);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.historyCancellation?.Cancel();
        this.historyCancellation?.Dispose();
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed -= this.RefreshCurrency;
        }

        this.lifetimeCancellation.Cancel();
        this.lifetimeCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed += this.RefreshCurrency;
            _ = this.DisplayCurrency.EnsureLoadedAsync(this.lifetimeCancellation.Token);
        }
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (!this.accountsLoaded)
        {
            await this.LoadAsync();
            this.accountsLoaded = true;
        }

        this.RestoreFiltersFromQuery();
        await this.LoadHistoryAsync();
    }

    private static bool TryParseDate(string value, out DateOnly? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        result = parsed;
        return true;
    }

    private static bool TryParseAmount(string value, out decimal? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
        {
            return false;
        }

        result = parsed;
        return true;
    }

    private static string FormatDateInput(object? value)
    {
        if (value is DateTime dateTime)
        {
            return DateOnly.FromDateTime(dateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (value is DateOnly dateOnly)
        {
            return dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var text = value?.ToString() ?? string.Empty;
        return DateOnly.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : text;
    }

    private static bool IsApiFailure(Exception exception) =>
        exception is HttpRequestException or JsonException or NotSupportedException
        || exception is OperationCanceledException;

    private string FormatCurrency(decimal amount) => MoneyFormatter.Format(
        amount,
        this.DisplayCurrency?.CurrentCode ?? MoneyFormatter.DefaultCurrencyCode);

    private void RefreshCurrency() => _ = this.InvokeAsync(this.StateHasChanged);

    private bool IsCreateDisabled() => this.isLoading || this.loadError || this.isSaving;

    private bool HasActiveFilters() =>
        this.filters.From is not null
        || this.filters.To is not null
        || this.filters.Classification is not null
        || this.filters.MinimumAmount is not null
        || this.filters.MaximumAmount is not null
        || this.filters.Search is not null;

    private void UpdateName(ChangeEventArgs eventArgs)
    {
        this.nameText = eventArgs.Value?.ToString() ?? string.Empty;
        this.nameError = null;
    }

    private void UpdateFrom(ChangeEventArgs eventArgs) => this.fromText = FormatDateInput(eventArgs.Value);

    private void UpdateTo(ChangeEventArgs eventArgs) => this.toText = FormatDateInput(eventArgs.Value);

    private void UpdateClassification(ChangeEventArgs eventArgs) =>
        this.classificationText = eventArgs.Value?.ToString() ?? string.Empty;

    private void UpdateMinimumAmount(ChangeEventArgs eventArgs) =>
        this.minimumAmountText = eventArgs.Value?.ToString() ?? string.Empty;

    private void UpdateMaximumAmount(ChangeEventArgs eventArgs) =>
        this.maximumAmountText = eventArgs.Value?.ToString() ?? string.Empty;

    private void UpdateSearch(ChangeEventArgs eventArgs) => this.searchText = eventArgs.Value?.ToString() ?? string.Empty;

    private async Task LoadAsync()
    {
        this.isLoading = true;
        this.loadError = false;

        try
        {
            this.accounts = await this.AccountsApiClient.ListAsync(this.lifetimeCancellation.Token);
        }
        catch (Exception exception) when (IsApiFailure(exception))
        {
            this.accounts = [];
            this.loadError = true;
        }
        finally
        {
            this.isLoading = false;
        }
    }

    private async Task LoadHistoryAsync()
    {
        this.historyCancellation?.Cancel();
        this.historyCancellation?.Dispose();
        this.historyCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.lifetimeCancellation.Token);
        var requestVersion = ++this.historyRequestVersion;
        this.history = null;
        this.historyLoadError = false;
        this.historyNotFound = false;
        this.isLoadingHistory = this.AccountId is not null;

        if (this.AccountId is not Guid accountId)
        {
            return;
        }

        try
        {
            var response = await this.AccountTransactionsApiClient.GetAsync(
                accountId,
                this.filters,
                this.historyCancellation.Token);
            if (requestVersion != this.historyRequestVersion)
            {
                return;
            }

            this.history = response;
            this.historyNotFound = response is null;
        }
        catch (Exception exception) when (IsApiFailure(exception))
        {
            if (requestVersion == this.historyRequestVersion
                && !this.historyCancellation.IsCancellationRequested)
            {
                this.historyLoadError = true;
            }
        }
        finally
        {
            if (requestVersion == this.historyRequestVersion)
            {
                this.isLoadingHistory = false;
            }
        }
    }

    private Task ApplyFiltersAsync()
    {
        if (!this.TryCreateFilters(out var normalizedFilters))
        {
            return Task.CompletedTask;
        }

        this.filters = normalizedFilters;
        this.SetQueryValues(normalizedFilters);
        this.Navigation.NavigateTo(this.CreateFilterUri(normalizedFilters));
        return Task.CompletedTask;
    }

    private Task ClearFiltersAsync()
    {
        this.filterError = null;
        this.filters = new AccountTransactionFilterRequest();
        this.SetQueryValues(this.filters);
        this.Navigation.NavigateTo($"/accounts/{this.AccountId}");
        return Task.CompletedTask;
    }

    private void RestoreFiltersFromQuery()
    {
        this.fromText = this.FromQuery ?? string.Empty;
        this.toText = this.ToQuery ?? string.Empty;
        this.classificationText = this.ClassificationQuery ?? string.Empty;
        this.minimumAmountText = this.MinimumAmountQuery ?? string.Empty;
        this.maximumAmountText = this.MaximumAmountQuery ?? string.Empty;
        this.searchText = this.SearchQuery ?? string.Empty;
        this.TryCreateFilters(out this.filters);
    }

    private bool TryCreateFilters(out AccountTransactionFilterRequest normalizedFilters)
    {
        this.filterError = null;
        normalizedFilters = new AccountTransactionFilterRequest();
        if (!TryParseDate(this.fromText, out var from)
            || !TryParseDate(this.toText, out var to))
        {
            this.filterError = "Enter dates in YYYY-MM-DD format.";
            return false;
        }

        if (from > to)
        {
            this.filterError = "From date must be on or before To date.";
            return false;
        }

        if (!TryParseAmount(this.minimumAmountText, out var minimumAmount)
            || !TryParseAmount(this.maximumAmountText, out var maximumAmount))
        {
            this.filterError = "Enter non-negative amounts using plain numbers.";
            return false;
        }

        if (minimumAmount > maximumAmount)
        {
            this.filterError = "Minimum amount must not exceed Maximum amount.";
            return false;
        }

        var classification = string.IsNullOrWhiteSpace(this.classificationText)
            ? null
            : this.classificationText;
        if (classification is not null
            && classification is not ("Necessities" or "Optional" or "Culture" or "Unexpected"))
        {
            this.filterError = "Choose a supported classification.";
            return false;
        }

        var search = string.IsNullOrWhiteSpace(this.searchText) ? null : this.searchText.Trim();
        if (search?.Length > 100)
        {
            this.filterError = "Search must be 100 characters or fewer.";
            return false;
        }

        normalizedFilters = new AccountTransactionFilterRequest
        {
            From = from,
            To = to,
            Classification = classification,
            MinimumAmount = minimumAmount,
            MaximumAmount = maximumAmount,
            Search = search,
        };
        return true;
    }

    private void SetQueryValues(AccountTransactionFilterRequest normalizedFilters)
    {
        this.FromQuery = this.fromText = normalizedFilters.From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
        this.ToQuery = this.toText = normalizedFilters.To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
        this.ClassificationQuery = this.classificationText = normalizedFilters.Classification ?? string.Empty;
        this.MinimumAmountQuery = this.minimumAmountText = normalizedFilters.MinimumAmount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        this.MaximumAmountQuery = this.maximumAmountText = normalizedFilters.MaximumAmount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        this.SearchQuery = this.searchText = normalizedFilters.Search ?? string.Empty;
    }

    private string CreateFilterUri(AccountTransactionFilterRequest normalizedFilters)
    {
        var uri = $"/accounts/{this.AccountId}";
        var values = new Dictionary<string, object?>
        {
            ["from"] = normalizedFilters.From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["to"] = normalizedFilters.To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["classification"] = normalizedFilters.Classification,
            ["minimumAmount"] = normalizedFilters.MinimumAmount?.ToString(CultureInfo.InvariantCulture),
            ["maximumAmount"] = normalizedFilters.MaximumAmount?.ToString(CultureInfo.InvariantCulture),
            ["search"] = normalizedFilters.Search,
        };
        return this.Navigation.GetUriWithQueryParameters(uri, values);
    }

    private async Task CreateAsync()
    {
        this.nameError = null;
        this.saveError = null;
        this.saveStatus = null;
        var trimmedName = this.nameText.Trim();
        if (trimmedName.Length == 0)
        {
            this.nameError = "Enter an account name.";
            return;
        }

        if (trimmedName.Length > 100)
        {
            this.nameError = "Account names cannot exceed 100 characters.";
            return;
        }

        this.isSaving = true;
        try
        {
            var result = await this.AccountsApiClient.CreateAsync(
                new CreateAccountRequest(trimmedName),
                this.lifetimeCancellation.Token);
            if (result == AccountCreationResult.Invalid)
            {
                this.nameError = "Use a unique account name between 1 and 100 characters.";
                return;
            }

            this.accounts = await this.AccountsApiClient.ListAsync(this.lifetimeCancellation.Token);
            this.AccountCatalogState.NotifyChanged();
            this.nameText = string.Empty;
            this.saveStatus = "Account created.";
        }
        catch (Exception exception) when (IsApiFailure(exception))
        {
            this.saveError = "The account could not be saved. Try again.";
        }
        finally
        {
            this.isSaving = false;
        }
    }
}
