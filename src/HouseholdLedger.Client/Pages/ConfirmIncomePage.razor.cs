// <copyright file="ConfirmIncomePage.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Pages;

using System.Globalization;
using System.Net;
using System.Text.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Formatting;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

/// <summary>Explicitly confirms an actual receipt and its account destinations.</summary>
public partial class ConfirmIncomePage : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private readonly List<AllocationInput> allocations = [new()];
    private IReadOnlyList<AccountResponse> accounts = [];
    private string receivedDateText = string.Empty;
    private string amountText = string.Empty;
    private string? error;
    private string? status;
    private string savedReviewLink = "/";
    private bool isConfirmed;
    private bool isLoading;
    private bool accountsLoadError;
    private bool isSaving;
    private int formVersion;
    private Guid? requestId;
    private Guid? appliedScheduleId;
    private string? scheduleSuggestionError;
    private string? scheduleSuggestionStatus;
    private bool hasUnsavedChanges;

    /// <summary>Gets or sets the suggested receipt year.</summary>
    [Parameter]
    public int Year { get; set; }

    /// <summary>Gets or sets the suggested receipt month.</summary>
    [Parameter]
    public int Month { get; set; }

    /// <summary>Gets or sets the suggested receipt day.</summary>
    [Parameter]
    public int Day { get; set; }

    /// <summary>Gets or sets the optional schedule used to prefill receipt suggestions.</summary>
    [SupplyParameterFromQuery]
    public Guid? ScheduleId { get; set; }

    [Inject]
    private IIncomeReceiptsApiClient IncomeApi { get; set; } = null!;

    [Inject]
    private IAccountsApiClient AccountsApi { get; set; } = null!;

    [Inject]
    private TimeProvider Clock { get; set; } = null!;

    [Inject]
    private IPaySchedulesApiClient PaySchedulesApi { get; set; } = null!;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = null!;

    [Inject]
    private SelectedDateState SelectedDate { get; set; } = null!;

    [CascadingParameter]
    private GlobalSettingsState? DisplayCurrency { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(this.Clock.GetLocalNow().DateTime);

    private string TodayText => this.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private string CurrencyCode => this.DisplayCurrency?.CurrentCode ?? MoneyFormatter.DefaultCurrencyCode;

    private string CurrencyLabel => MoneyFormatter.GetDisplayLabel(this.CurrencyCode);

    private bool HasUnsavedChanges => this.hasUnsavedChanges;

    private string AllocationFeedback
    {
        get
        {
            decimal total = 0;
            foreach (var allocation in this.allocations)
            {
                if (!MoneyInput.TryParse(allocation.AmountText, out var amount) || amount <= 0)
                {
                    return "Enter a positive amount for every account allocation.";
                }

                total += amount;
            }

            return MoneyInput.TryParse(this.amountText, out var received)
                ? $"Allocated: {this.FormatCurrency(total)} of {this.FormatCurrency(received)}. Remaining: {this.FormatCurrency(received - total)}."
                : "Enter a valid received amount to reconcile account allocations.";
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed -= this.RefreshCurrency;
        }

        this.lifetimeCancellation.Cancel();
        this.lifetimeCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed += this.RefreshCurrency;
        }

        await this.LoadAccountsAsync();
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        this.formVersion++;
        this.status = null;
        this.error = null;
        this.isConfirmed = false;
        this.hasUnsavedChanges = false;
        this.requestId = null;
        this.appliedScheduleId = null;
        this.scheduleSuggestionError = null;
        this.scheduleSuggestionStatus = null;
        this.amountText = string.Empty;
        this.allocations.Clear();
        this.allocations.Add(new());
        if (this.Year is >= 1 and <= 9999 && this.Month is >= 1 and <= 12
            && this.Day >= 1 && this.Day <= DateTime.DaysInMonth(this.Year, this.Month))
        {
            this.receivedDateText = new DateOnly(this.Year, this.Month, this.Day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        else
        {
            this.receivedDateText = string.Empty;
            this.error = "Choose a valid actual received date.";
        }

        if (this.ScheduleId is Guid scheduleId)
        {
            await this.ApplyScheduleSuggestionAsync(scheduleId);
        }
    }

    private string FormatCurrency(decimal amount) => MoneyFormatter.Format(amount, this.CurrencyCode);

    private void RefreshCurrency() => _ = this.InvokeAsync(this.StateHasChanged);

    private async Task LoadAccountsAsync()
    {
        this.isLoading = true;
        this.accountsLoadError = false;
        try
        {
            this.accounts = await this.AccountsApi.ListAsync(this.lifetimeCancellation.Token);
        }
        catch (OperationCanceledException) when (this.lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException)
        {
            this.accountsLoadError = true;
        }
        finally
        {
            this.isLoading = false;
        }
    }

    private void ClearFeedback()
    {
        this.error = null;
        this.status = null;
        this.isConfirmed = false;
    }

    private async Task ConfirmUnsavedNavigationAsync(LocationChangingContext context)
    {
        if (this.hasUnsavedChanges
            && !await this.JavaScript.InvokeAsync<bool>("confirm", "Leave this page and discard the unconfirmed receipt details?"))
        {
            context.PreventNavigation();
        }
    }

    private async Task ApplyScheduleSuggestionAsync(Guid scheduleId)
    {
        if (this.appliedScheduleId == scheduleId)
        {
            return;
        }

        this.appliedScheduleId = scheduleId;
        try
        {
            var schedules = await this.PaySchedulesApi.ListAsync(this.lifetimeCancellation.Token);
            var schedule = schedules.FirstOrDefault(item => item.Id == scheduleId);
            if (schedule is null)
            {
                this.scheduleSuggestionError = "The selected pay schedule is unavailable. Enter the actual receipt details manually.";
                return;
            }

            this.amountText = MoneyInput.Format(schedule.NetIncome);
            this.allocations.Clear();
            this.allocations.AddRange(schedule.Allocations.Select(allocation => new AllocationInput
            {
                AccountId = allocation.AccountId.ToString(),
                AmountText = MoneyInput.Format(allocation.Amount),
            }));
            if (this.allocations.Count == 0)
            {
                this.allocations.Add(new());
            }

            this.scheduleSuggestionStatus = "Suggested amounts and account destinations were copied from the schedule. Verify the actual receipt; nothing was confirmed or sent.";
            this.hasUnsavedChanges = true;
        }
        catch (OperationCanceledException) when (this.lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            this.scheduleSuggestionError = "The pay schedule suggestion could not be loaded. Enter the actual receipt details manually.";
        }
    }

    private void UpdateDate(ChangeEventArgs args)
    {
        this.receivedDateText = args.Value?.ToString() ?? string.Empty;
        this.MarkReceiptEdited();
        this.ClearFeedback();
    }

    private void UpdateAmount(ChangeEventArgs args)
    {
        this.amountText = args.Value?.ToString() ?? string.Empty;
        this.MarkReceiptEdited();
        this.ClearFeedback();
    }

    private void UpdateAccount(AllocationInput allocation, ChangeEventArgs args)
    {
        allocation.AccountId = args.Value?.ToString() ?? string.Empty;
        this.MarkReceiptEdited();
        this.ClearFeedback();
    }

    private void UpdateAllocation(AllocationInput allocation, ChangeEventArgs args)
    {
        allocation.AmountText = args.Value?.ToString() ?? string.Empty;
        this.MarkReceiptEdited();
        this.ClearFeedback();
    }

    private void UpdateConfirmation(ChangeEventArgs args) => this.isConfirmed = args.Value is true;

    private void AddAllocation()
    {
        this.allocations.Add(new());
        this.MarkReceiptEdited();
        this.ClearFeedback();
    }

    private void RemoveAllocation(AllocationInput allocation)
    {
        if (this.allocations.Count > 1)
        {
            this.allocations.Remove(allocation);
            this.MarkReceiptEdited();
            this.ClearFeedback();
        }
    }

    private bool TryBuildRequest(out ConfirmIncomeReceiptRequest? request)
    {
        request = null;
        if (!this.isConfirmed)
        {
            this.error = "Confirm that this money has actually been received.";
            return false;
        }

        if (!DateOnly.TryParseExact(this.receivedDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var receivedDate) || receivedDate > this.Today)
        {
            this.error = "Enter the actual received date, not a future expected date.";
            return false;
        }

        if (!MoneyInput.TryParse(this.amountText, out var amount) || amount <= 0)
        {
            this.error = "Enter a positive received amount with at most two decimal places, no greater than 9,999,999,999,999,999.99.";
            return false;
        }

        var destinations = new List<IncomeAllocationRequest>();
        foreach (var allocation in this.allocations)
        {
            if (!Guid.TryParse(allocation.AccountId, out var accountId) || !this.accounts.Any(account => account.Id == accountId)
                || !MoneyInput.TryParse(allocation.AmountText, out var allocationAmount) || allocationAmount <= 0)
            {
                this.error = "Choose an existing account and a positive valid amount for every allocation.";
                return false;
            }

            destinations.Add(new IncomeAllocationRequest(accountId, allocationAmount));
        }

        if (destinations.Select(allocation => allocation.AccountId).Distinct().Count() != destinations.Count)
        {
            this.error = "Use each account only once.";
            return false;
        }

        if (destinations.Count == 0 || destinations.Sum(allocation => allocation.Amount) != amount)
        {
            this.error = "Account allocations must total the received amount exactly.";
            return false;
        }

        request = new ConfirmIncomeReceiptRequest(receivedDate, amount, destinations);
        return true;
    }

    private async Task ConfirmAsync()
    {
        if (this.isSaving || this.accountsLoadError || this.isLoading)
        {
            return;
        }

        if (!this.TryBuildRequest(out var request) || request is null)
        {
            return;
        }

        var version = this.formVersion;
        this.isSaving = true;
        this.requestId ??= Guid.NewGuid();
        this.error = null;
        this.status = null;
        try
        {
            await this.IncomeApi.ConfirmAsync(request, this.requestId.Value, this.lifetimeCancellation.Token);
            this.SelectedDate.NotifyIncomeReceiptsChanged(request.ReceivedDate);
            if (version == this.formVersion && !this.lifetimeCancellation.IsCancellationRequested)
            {
                this.status = "Received income confirmed. No schedule, expense or monthly plan was changed.";
                this.savedReviewLink = $"/months/{request.ReceivedDate.Year}/{request.ReceivedDate.Month}";
                this.hasUnsavedChanges = false;
                this.requestId = null;
                this.isConfirmed = false;
                this.amountText = string.Empty;
                this.allocations.Clear();
                this.allocations.Add(new());
            }
        }
        catch (OperationCanceledException) when (this.lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            if (version == this.formVersion)
            {
                this.error = exception is HttpRequestException { StatusCode: HttpStatusCode.BadRequest }
                    ? "The server rejected this receipt. Check the actual date, amount and account allocations. Your entries are preserved."
                    : "Confirmation could not be verified. Your entries are preserved; retrying unchanged details uses the same request identifier.";
            }
        }
        catch (JsonException)
        {
            if (version == this.formVersion)
            {
                this.error = "The receipt response could not be read. Your entries are preserved; retrying unchanged details uses the same request identifier.";
            }
        }
        finally
        {
            this.isSaving = false;
        }
    }

    private void MarkReceiptEdited()
    {
        this.hasUnsavedChanges = true;
        this.requestId = null;
    }

    private sealed class AllocationInput
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string AccountId { get; set; } = string.Empty;

        public string AmountText { get; set; } = string.Empty;
    }
}
