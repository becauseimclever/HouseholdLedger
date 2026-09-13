// <copyright file="PaySchedulesPage.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Pages;

using System.Globalization;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Formatting;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;

/// <summary>Presents recurring income schedule management.</summary>
public partial class PaySchedulesPage : ComponentBase, IDisposable
{
    private static readonly (PayPeriodCadence Value, string Label)[] CadenceOptions =
    [
        (PayPeriodCadence.Weekly, "Weekly"), (PayPeriodCadence.Biweekly, "Biweekly"),
        (PayPeriodCadence.Semimonthly, "Semimonthly"), (PayPeriodCadence.FourWeekly, "Four-week"),
        (PayPeriodCadence.Monthly, "Monthly"),
    ];

    private readonly CancellationTokenSource lifetimeCancellation = new();
    private readonly Dictionary<Guid, string> resumeDateTexts = [];
    private IReadOnlyList<AccountResponse> accounts = [];
    private IReadOnlyList<PayScheduleResponse> schedules = [];
    private List<AllocationInput> allocations = [new()];
    private bool accountsLoadError;
    private bool isLoading = true;
    private bool isMutating;
    private bool isSaving;
    private bool loadError;
    private Guid? editingScheduleId;
    private string nameText = string.Empty;
    private string firstPayDateText = string.Empty;
    private string cadenceText = PayPeriodCadence.Biweekly.ToString();
    private string secondPayDayText = string.Empty;
    private string netIncomeText = string.Empty;
    private string? allocationError;
    private string? firstPayDateError;
    private string? nameError;
    private string? netIncomeError;
    private string? saveError;
    private string? saveStatus;
    private string? secondPayDayError;

    private static CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    [Inject]
    private IAccountsApiClient AccountsApi { get; set; } = null!;

    [Inject]
    private IPaySchedulesApiClient PaySchedulesApi { get; set; } = null!;

    [Inject]
    private TimeProvider Clock { get; set; } = null!;

    [CascadingParameter]
    private GlobalSettingsState? DisplayCurrency { get; set; }

    private bool IsSemimonthly => this.cadenceText == PayPeriodCadence.Semimonthly.ToString();

    private string CurrentCurrencyCode => this.DisplayCurrency?.CurrentCode ?? MoneyFormatter.DefaultCurrencyCode;

    private string CurrentCurrencyLabel => MoneyFormatter.GetDisplayLabel(this.CurrentCurrencyCode);

    private decimal AllocationTotal => this.allocations.Sum(allocation => PaySchedulesPage.TryParseAmount(allocation.AmountText, out var amount) ? amount : 0m);

    private string NetIncomePreview => PaySchedulesPage.TryParseAmount(this.netIncomeText, out var amount) ? this.FormatCurrency(amount) : this.CurrentCurrencyLabel;

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
            _ = this.DisplayCurrency.EnsureLoadedAsync(this.lifetimeCancellation.Token);
        }

        this.firstPayDateText = DateOnly.FromDateTime(this.Clock.GetLocalNow().DateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await this.LoadAsync();
    }

    private static bool TryParseAmount(string value, out decimal amount) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

    private static string GetAllocationId(string prefix, Guid id) => $"{prefix}-{id:N}";

    private static string GetScheduleId(string prefix, Guid id) => $"{prefix}-{id:N}";

    private static string GetCadenceLabel(PayPeriodCadence cadence) => CadenceOptions.Single(option => option.Value == cadence).Label;

    private static string GetAllocationSummary(PayScheduleResponse schedule) => $"{schedule.Allocations.Count} {(schedule.Allocations.Count == 1 ? "allocation" : "allocations")}";

    private static DateOnly GetNextPayDate(PayScheduleResponse schedule) => schedule.FirstPayDate;

    private string FormatCurrency(decimal amount) => MoneyFormatter.Format(amount, this.CurrentCurrencyCode);

    private void RefreshCurrency() => _ = this.InvokeAsync(this.StateHasChanged);

    private bool IsAccountChosenByAnotherAllocation(Guid accountId, Guid allocationId) => this.allocations.Any(allocation => allocation.Id != allocationId && allocation.AccountId == accountId.ToString());

    private string GetResumeDateText(Guid scheduleId) => this.resumeDateTexts.TryGetValue(scheduleId, out var date) ? date : DateOnly.FromDateTime(this.Clock.GetLocalNow().DateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private async Task LoadAsync()
    {
        this.isLoading = true;
        this.loadError = false;
        this.accountsLoadError = false;
        try
        {
            await Task.WhenAll(this.LoadAccountsAsync(), this.LoadSchedulesAsync());
        }
        finally
        {
            this.isLoading = false;
        }
    }

    private async Task LoadAccountsAsync()
    {
        try
        {
            this.accounts = await this.AccountsApi.ListAsync(this.lifetimeCancellation.Token);
        }
        catch (HttpRequestException)
        {
            this.accounts = [];
            this.accountsLoadError = true;
        }
    }

    private async Task LoadSchedulesAsync()
    {
        try
        {
            this.schedules = await this.PaySchedulesApi.ListAsync(this.lifetimeCancellation.Token);
        }
        catch (HttpRequestException)
        {
            this.schedules = [];
            this.loadError = true;
        }
    }

    private void UpdateName(ChangeEventArgs args)
    {
        this.nameText = args.Value?.ToString() ?? string.Empty;
        this.nameError = null;
    }

    private void UpdateFirstPayDate(ChangeEventArgs args)
    {
        this.firstPayDateText = args.Value?.ToString() ?? string.Empty;
        this.firstPayDateError = null;
    }

    private void UpdateCadence(ChangeEventArgs args)
    {
        this.cadenceText = args.Value?.ToString() ?? string.Empty;
        this.secondPayDayError = null;
    }

    private void UpdateSecondPayDay(ChangeEventArgs args)
    {
        this.secondPayDayText = args.Value?.ToString() ?? string.Empty;
        this.secondPayDayError = null;
    }

    private void UpdateNetIncome(ChangeEventArgs args)
    {
        this.netIncomeText = args.Value?.ToString() ?? string.Empty;
        this.netIncomeError = null;
        this.allocationError = null;
    }

    private void UpdateAllocationAccount(Guid id, ChangeEventArgs args)
    {
        this.allocations.Single(allocation => allocation.Id == id).AccountId = args.Value?.ToString() ?? string.Empty;
        this.allocationError = null;
    }

    private void UpdateAllocationAmount(Guid id, ChangeEventArgs args)
    {
        this.allocations.Single(allocation => allocation.Id == id).AmountText = args.Value?.ToString() ?? string.Empty;
        this.allocationError = null;
    }

    private void UpdateResumeDate(Guid id, ChangeEventArgs args) => this.resumeDateTexts[id] = args.Value?.ToString() ?? string.Empty;

    private void AddAllocation() => this.allocations.Add(new AllocationInput());

    private void RemoveAllocation(Guid id)
    {
        if (this.allocations.Count > 1)
        {
            this.allocations.RemoveAll(allocation => allocation.Id == id);
        }
    }

    private bool TryBuildRequest(out CreatePayScheduleRequest? request)
    {
        request = null;
        this.nameError = null;
        this.firstPayDateError = null;
        this.netIncomeError = null;
        this.secondPayDayError = null;
        this.allocationError = null;
        if (string.IsNullOrWhiteSpace(this.nameText))
        {
            this.nameError = "Enter a schedule name.";
        }

        if (!DateOnly.TryParseExact(this.firstPayDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var firstPayDate))
        {
            this.firstPayDateError = "Enter a first pay date.";
        }

        if (!PaySchedulesPage.TryParseAmount(this.netIncomeText, out var netIncome) || netIncome <= 0)
        {
            this.netIncomeError = "Enter a positive net income.";
        }

        int? secondPayDay = null;
        if (this.IsSemimonthly && (!int.TryParse(this.secondPayDayText, CultureInfo.InvariantCulture, out var day) || day is < 1 or > 31 || day == firstPayDate.Day))
        {
            this.secondPayDayError = "Enter a distinct second pay day from 1 through 31.";
        }
        else if (this.IsSemimonthly)
        {
            secondPayDay = int.Parse(this.secondPayDayText, CultureInfo.InvariantCulture);
        }

        var requestAllocations = new List<IncomeAllocationRequest>();
        foreach (var allocation in this.allocations)
        {
            if (!Guid.TryParse(allocation.AccountId, out var accountId) || !PaySchedulesPage.TryParseAmount(allocation.AmountText, out var amount) || amount <= 0)
            {
                this.allocationError = "Choose an account and enter a positive amount for every allocation.";
                break;
            }

            requestAllocations.Add(new IncomeAllocationRequest(accountId, amount));
        }

        if (this.allocationError is null && requestAllocations.Select(allocation => allocation.AccountId).Distinct().Count() != requestAllocations.Count)
        {
            this.allocationError = "Each allocation must use a different account.";
        }

        if (this.allocationError is null && requestAllocations.Sum(allocation => allocation.Amount) != netIncome)
        {
            this.allocationError = "Allocations must total the net income exactly.";
        }

        if (this.nameError is not null || this.firstPayDateError is not null || this.netIncomeError is not null || this.secondPayDayError is not null || this.allocationError is not null)
        {
            return false;
        }

        request = new CreatePayScheduleRequest(this.nameText.Trim(), firstPayDate, Enum.Parse<PayPeriodCadence>(this.cadenceText), netIncome, requestAllocations, secondPayDay);
        return true;
    }

    private async Task SaveAsync()
    {
        if (!this.TryBuildRequest(out var request))
        {
            return;
        }

        this.isSaving = true;
        this.saveError = null;
        this.saveStatus = null;
        try
        {
            if (this.editingScheduleId is Guid scheduleId)
            {
                await this.PaySchedulesApi.ReviseAsync(scheduleId, new RevisePayScheduleRequest(request!.Name, request.FirstPayDate, request.Cadence, request.NetIncome, request.Allocations, request.SecondMonthlyPayDay), this.lifetimeCancellation.Token);
            }
            else
            {
                await this.PaySchedulesApi.CreateAsync(request!, this.lifetimeCancellation.Token);
            }

            await this.LoadSchedulesAsync();
            this.ResetForm();
            this.saveStatus = "Pay schedule saved.";
        }
        catch (HttpRequestException)
        {
            this.saveError = "Pay schedule could not be saved.";
        }
        finally
        {
            this.isSaving = false;
        }
    }

    private void BeginEdit(PayScheduleResponse schedule)
    {
        this.editingScheduleId = schedule.Id;
        this.nameText = schedule.Name;
        this.firstPayDateText = schedule.FirstPayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        this.cadenceText = schedule.Cadence.ToString();
        this.secondPayDayText = schedule.SecondMonthlyPayDay?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        this.netIncomeText = schedule.NetIncome.ToString(CultureInfo.InvariantCulture);
        this.allocations = schedule.Allocations.Select(allocation => new AllocationInput { AccountId = allocation.AccountId.ToString(), AmountText = allocation.Amount.ToString(CultureInfo.InvariantCulture) }).ToList();
        this.saveError = null;
        this.saveStatus = null;
    }

    private void CancelEdit() => this.ResetForm();

    private void ResetForm()
    {
        this.editingScheduleId = null;
        this.nameText = string.Empty;
        this.secondPayDayText = string.Empty;
        this.netIncomeText = string.Empty;
        this.allocations = [new()];
    }

    private async Task PauseAsync(Guid id)
    {
        await this.MutateAsync(() => this.PaySchedulesApi.PauseAsync(id, this.lifetimeCancellation.Token), "Pay schedule paused.");
    }

    private async Task ResumeAsync(PayScheduleResponse schedule)
    {
        if (!DateOnly.TryParseExact(this.GetResumeDateText(schedule.Id), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var resumeDate))
        {
            this.saveError = "Enter a resume date.";
            return;
        }

        await this.MutateAsync(() => this.PaySchedulesApi.ResumeAsync(schedule.Id, new ResumePayScheduleRequest(resumeDate), this.lifetimeCancellation.Token), "Pay schedule resumed without backfilling receipts.");
    }

    private async Task MutateAsync(Func<Task<bool>> action, string status)
    {
        this.isMutating = true;
        this.saveError = null;
        this.saveStatus = null;
        try
        {
            if (!await action())
            {
                this.saveError = "This pay schedule is no longer available.";
            }
            else
            {
                await this.LoadSchedulesAsync();
                this.saveStatus = status;
            }
        }
        catch (HttpRequestException)
        {
            this.saveError = "Pay schedule could not be updated.";
        }
        finally
        {
            this.isMutating = false;
        }
    }

    private sealed class AllocationInput
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string AccountId { get; set; } = string.Empty;

        public string AmountText { get; set; } = string.Empty;
    }
}
