// <copyright file="TransactionInspector.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Components;

using System.Globalization;
using System.Text.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Formatting;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Presents selected-day transactions and the first expense form.
/// </summary>
public partial class TransactionInspector : ComponentBase, IDisposable
{
    private static readonly string[] Classifications = ["Necessities", "Optional", "Culture", "Unexpected"];
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private CancellationTokenSource? requestCancellation;
    private IReadOnlyList<AccountResponse> accounts = [];
    private string accountId = string.Empty;
    private string? accountError;
    private string amountText = string.Empty;
    private string descriptionText = string.Empty;
    private string? descriptionError;
    private string classification = string.Empty;
    private string? amountError;
    private string? classificationError;
    private string? saveError;
    private Guid? editingTransactionId;
    private string editAccountId = string.Empty;
    private string? editAccountError;
    private Guid? removingTransactionId;
    private string editAmountText = string.Empty;
    private string editClassification = string.Empty;
    private string editDescriptionText = string.Empty;
    private string? editDescriptionError;
    private string? editAmountError;
    private string? editClassificationError;
    private string? mutationError;
    private string? mutationStatus;
    private bool isLoading;
    private bool isLoadingAccounts;
    private bool isMutating;
    private bool isSaving;
    private bool loadError;
    private bool accountLoadError;
    private bool incomeLoadError;
    private bool isLoadingIncome;
    private Guid? editingReceiptId;
    private Guid? removingIncomeReceiptId;
    private string receiptEditDateText = string.Empty;
    private string receiptEditAmountText = string.Empty;
    private List<ReceiptAllocationInput> receiptEditAllocations = [];
    private string? incomeMutationError;
    private string? incomeMutationStatus;
    private IReadOnlyList<IncomeReceiptResponse> incomeReceipts = [];
    private IReadOnlyList<PayScheduleResponse> pendingIncome = [];
    private IReadOnlyList<PayScheduleResponse> schedulesForSelectedDate = [];
    private IReadOnlyDictionary<Guid, string> scheduleNames = new Dictionary<Guid, string>();
    private IReadOnlyList<ExpenseTransactionResponse> transactions = [];

    /// <summary>Gets or sets the selected-date state.</summary>
    [Inject]
    private SelectedDateState SelectedDate { get; set; } = null!;

    /// <summary>Gets or sets the account catalog API client.</summary>
    [Inject]
    private IAccountsApiClient AccountsApi { get; set; } = null!;

    /// <summary>Gets or sets the transaction API client.</summary>
    [Inject]
    private ITransactionsApiClient TransactionsApi { get; set; } = null!;

    /// <summary>Gets or sets the confirmed income receipt API client.</summary>
    [Inject]
    private IIncomeReceiptsApiClient IncomeReceiptsApi { get; set; } = null!;

    /// <summary>Gets or sets the service provider used for optional income receipt data.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    /// <summary>Gets or sets the clock used to identify future scheduled income.</summary>
    [Inject]
    private TimeProvider Clock { get; set; } = null!;

    /// <summary>Gets or sets the global display-currency state.</summary>
    [CascadingParameter]
    private GlobalSettingsState? DisplayCurrency { get; set; }

    private string CurrentCurrencyCode =>
        this.DisplayCurrency?.CurrentCode ?? MoneyFormatter.DefaultCurrencyCode;

    private string CurrentCurrencyLabel => MoneyFormatter.GetDisplayLabel(this.CurrentCurrencyCode);

    private string TodayText => DateOnly.FromDateTime(this.Clock.GetLocalNow().DateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private string HasAmountError => this.amountError is null ? "false" : "true";

    private string HasAccountError => this.accountError is null ? "false" : "true";

    private string HasClassificationError => this.classificationError is null ? "false" : "true";

    /// <inheritdoc/>
    public void Dispose()
    {
        this.SelectedDate.Changed -= this.OnSelectedDateChanged;
        this.SelectedDate.IncomeReceiptsChanged -= this.OnIncomeReceiptsChanged;
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed -= this.RefreshCurrency;
        }

        this.requestCancellation?.Cancel();
        this.requestCancellation?.Dispose();
        this.lifetimeCancellation.Cancel();
        this.lifetimeCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        this.SelectedDate.Changed += this.OnSelectedDateChanged;
        this.SelectedDate.IncomeReceiptsChanged += this.OnIncomeReceiptsChanged;
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed += this.RefreshCurrency;
            _ = this.DisplayCurrency.EnsureLoadedAsync(this.lifetimeCancellation.Token);
        }
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && this.SelectedDate.Value is DateOnly ledgerDate)
        {
            this.requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.lifetimeCancellation.Token);
            await this.LoadSelectionAsync(ledgerDate, this.requestCancellation.Token);
        }
    }

    private static string GetElementId(string prefix, Guid transactionId) => $"{prefix}-{transactionId:N}";

    private static bool IsDueOn(PayScheduleResponse schedule, DateOnly date)
    {
        if (schedule.Cadence is PayPeriodCadence.Weekly or PayPeriodCadence.Biweekly or PayPeriodCadence.FourWeekly)
        {
            var interval = schedule.Cadence == PayPeriodCadence.Weekly ? 7 : schedule.Cadence == PayPeriodCadence.Biweekly ? 14 : 28;
            return date >= schedule.FirstPayDate && (date.DayNumber - schedule.FirstPayDate.DayNumber) % interval == 0;
        }

        return date >= schedule.FirstPayDate && (date.Day == Math.Min(schedule.FirstPayDate.Day, DateTime.DaysInMonth(date.Year, date.Month)) || (schedule.Cadence == PayPeriodCadence.Semimonthly && date.Day == Math.Min(schedule.SecondMonthlyPayDay!.Value, DateTime.DaysInMonth(date.Year, date.Month))));
    }

    private static string? NormalizeDescription(string description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static void UpdateReceiptAccount(ReceiptAllocationInput allocation, ChangeEventArgs args) =>
        allocation.AccountId = args.Value?.ToString() ?? string.Empty;

    private static void UpdateReceiptAllocation(ReceiptAllocationInput allocation, ChangeEventArgs args) =>
        allocation.AmountText = args.Value?.ToString() ?? string.Empty;

    private Task RetryIncomeAsync()
    {
        return this.SelectedDate.Value is DateOnly ledgerDate
            ? this.LoadIncomeAsync(ledgerDate, this.requestCancellation?.Token ?? this.lifetimeCancellation.Token)
            : Task.CompletedTask;
    }

    private void OnIncomeReceiptsChanged(DateOnly date)
    {
        if (this.SelectedDate.Value == date && !this.isMutating)
        {
            _ = this.InvokeAsync(async () =>
            {
                await this.LoadIncomeAsync(date, this.lifetimeCancellation.Token);
                this.StateHasChanged();
            });
        }
    }

    private void RefreshCurrency() => _ = this.InvokeAsync(this.StateHasChanged);

    private void OnSelectedDateChanged(DateOnly ledgerDate)
    {
        this.requestCancellation?.Cancel();
        this.requestCancellation?.Dispose();
        this.requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.lifetimeCancellation.Token);
        this.accounts = [];
        this.transactions = [];
        this.incomeReceipts = [];
        this.pendingIncome = [];
        this.schedulesForSelectedDate = [];
        this.accountId = string.Empty;
        this.descriptionText = string.Empty;
        this.descriptionError = null;
        this.accountError = null;
        this.accountLoadError = false;
        this.loadError = false;
        this.incomeLoadError = false;
        this.saveError = null;
        this.ResetIncomeMutationState();
        this.isSaving = false;
        this.isMutating = false;
        this.ResetMutationState();
        _ = this.LoadSelectionAsync(ledgerDate, this.requestCancellation.Token);
    }

    private Task LoadSelectionAsync(DateOnly ledgerDate, CancellationToken cancellationToken) =>
        Task.WhenAll(
            this.LoadAccountsAsync(ledgerDate, cancellationToken),
            this.LoadAsync(ledgerDate, cancellationToken),
            this.LoadIncomeAsync(ledgerDate, cancellationToken));

    private string GetScheduleName(Guid? scheduleId) => scheduleId is { } id ? this.scheduleNames.GetValueOrDefault(id, "Income receipt") : "Confirmed receipt";

    private void RefreshPendingIncome(DateOnly ledgerDate)
    {
        this.pendingIncome = this.schedulesForSelectedDate
            .Where(schedule => !schedule.IsPaused
                && ledgerDate >= DateOnly.FromDateTime(this.Clock.GetLocalNow().DateTime)
                && IsDueOn(schedule, ledgerDate)
                && !this.incomeReceipts.Any(receipt => receipt.ScheduleId == schedule.Id))
            .ToArray();
    }

    private string GetAccountName(Guid accountId) => this.accounts.FirstOrDefault(account => account.Id == accountId)?.Name ?? accountId.ToString();

    private async Task LoadIncomeAsync(DateOnly ledgerDate, CancellationToken cancellationToken)
    {
        var paySchedulesApi = this.Services.GetService(typeof(IPaySchedulesApiClient)) as IPaySchedulesApiClient;
        if (paySchedulesApi is null)
        {
            this.incomeLoadError = true;
            return;
        }

        this.isLoadingIncome = true;
        this.incomeLoadError = false;
        try
        {
            var receiptsTask = paySchedulesApi.ListReceiptsAsync(ledgerDate, ledgerDate, cancellationToken);
            var schedulesTask = paySchedulesApi.ListAsync(cancellationToken);
            await Task.WhenAll(receiptsTask, schedulesTask);
            if (!cancellationToken.IsCancellationRequested && this.SelectedDate.Value == ledgerDate)
            {
                this.incomeReceipts = await receiptsTask;
                var schedules = await schedulesTask;
                this.schedulesForSelectedDate = schedules;
                this.scheduleNames = schedules.ToDictionary(schedule => schedule.Id, schedule => schedule.Name);
                this.RefreshPendingIncome(ledgerDate);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            if (this.SelectedDate.Value == ledgerDate)
            {
                this.incomeLoadError = true;
            }
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested && this.SelectedDate.Value == ledgerDate)
            {
                this.isLoadingIncome = false;
                await this.InvokeAsync(this.StateHasChanged);
            }
        }
    }

    private async Task LoadAccountsAsync(DateOnly ledgerDate, CancellationToken cancellationToken)
    {
        this.isLoadingAccounts = true;
        this.accountLoadError = false;
        await this.InvokeAsync(this.StateHasChanged);

        try
        {
            var loaded = await this.AccountsApi.ListAsync(cancellationToken);
            if (!cancellationToken.IsCancellationRequested && this.SelectedDate.Value == ledgerDate)
            {
                this.accounts = loaded;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            if (this.SelectedDate.Value == ledgerDate)
            {
                this.accounts = [];
                this.accountLoadError = true;
            }
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested && this.SelectedDate.Value == ledgerDate)
            {
                this.isLoadingAccounts = false;
                await this.InvokeAsync(this.StateHasChanged);
            }
        }
    }

    private async Task RetryAccountsAsync()
    {
        if (this.SelectedDate.Value is DateOnly ledgerDate)
        {
            var cancellationToken = this.requestCancellation?.Token ?? this.lifetimeCancellation.Token;
            await this.LoadAccountsAsync(ledgerDate, cancellationToken);
        }
    }

    private async Task LoadAsync(DateOnly ledgerDate, CancellationToken cancellationToken)
    {
        this.isLoading = true;
        this.loadError = false;
        await this.InvokeAsync(this.StateHasChanged);

        try
        {
            var loaded = await this.TransactionsApi.ListAsync(ledgerDate, cancellationToken);
            if (!cancellationToken.IsCancellationRequested && this.SelectedDate.Value == ledgerDate)
            {
                this.transactions = loaded;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            if (this.SelectedDate.Value == ledgerDate)
            {
                this.loadError = true;
            }
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested && this.SelectedDate.Value == ledgerDate)
            {
                this.isLoading = false;
                await this.InvokeAsync(this.StateHasChanged);
            }
        }
    }

    private async Task SaveAsync()
    {
        this.accountError = null;
        this.amountError = null;
        this.classificationError = null;
        this.descriptionError = null;
        this.saveError = null;

        if (!Guid.TryParse(this.accountId, out var selectedAccountId)
            || !this.accounts.Any(account => account.Id == selectedAccountId))
        {
            this.accountError = "Choose an account.";
        }

        if (!decimal.TryParse(this.amountText, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount)
            || amount <= 0
            || decimal.Round(amount, 2) != amount)
        {
            this.amountError = "Enter a positive amount with no more than two decimal places.";
        }

        if (!Classifications.Contains(this.classification, StringComparer.Ordinal))
        {
            this.classificationError = "Choose a classification.";
        }

        if (this.descriptionText.Length > 200)
        {
            this.descriptionError = "Description must be 200 characters or fewer.";
        }

        if (this.accountError is not null
            || this.amountError is not null
            || this.classificationError is not null
            || this.descriptionError is not null
            || this.SelectedDate.Value is not DateOnly ledgerDate)
        {
            return;
        }

        this.isSaving = true;
        var cancellationToken = this.requestCancellation?.Token ?? this.lifetimeCancellation.Token;
        try
        {
            var saved = await this.TransactionsApi.CreateAsync(
                ledgerDate,
                new CreateExpenseTransactionRequest(selectedAccountId, amount, this.classification, NormalizeDescription(this.descriptionText)),
                cancellationToken);
            if (!saved)
            {
                this.saveError = "The expense was not saved. Check the entered values.";
                return;
            }

            if (this.SelectedDate.Value != ledgerDate)
            {
                return;
            }

            this.amountText = string.Empty;
            this.descriptionText = string.Empty;
            this.accountId = string.Empty;
            this.classification = string.Empty;
            await this.LoadAsync(ledgerDate, cancellationToken);
            this.SelectedDate.NotifyTransactionsChanged(ledgerDate);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            this.saveError = "The expense could not be saved. Try again.";
        }
        finally
        {
            if (this.SelectedDate.Value == ledgerDate)
            {
                this.isSaving = false;
            }
        }
    }

    private void BeginEdit(ExpenseTransactionResponse transaction)
    {
        this.removingTransactionId = null;
        this.editingTransactionId = transaction.Id;
        this.editAccountId = transaction.AccountId.ToString();
        this.editAmountText = transaction.Amount.ToString("0.00", CultureInfo.CurrentCulture);
        this.editClassification = transaction.Classification;
        this.editDescriptionText = transaction.Description ?? string.Empty;
        this.ClearMutationMessages();
    }

    private void CancelEdit()
    {
        this.editingTransactionId = null;
        this.ClearMutationMessages();
    }

    private async Task ReviseAsync()
    {
        this.editAccountError = null;
        this.editAmountError = null;
        this.editClassificationError = null;
        this.editDescriptionError = null;
        this.mutationError = null;
        this.mutationStatus = null;

        if (!Guid.TryParse(this.editAccountId, out var selectedAccountId)
            || !this.accounts.Any(account => account.Id == selectedAccountId))
        {
            this.editAccountError = "Choose an account.";
        }

        if (!decimal.TryParse(this.editAmountText, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount)
            || amount <= 0
            || decimal.Round(amount, 2) != amount)
        {
            this.editAmountError = "Enter a positive amount with no more than two decimal places.";
        }

        if (!Classifications.Contains(this.editClassification, StringComparer.Ordinal))
        {
            this.editClassificationError = "Choose a classification.";
        }

        if (this.editDescriptionText.Length > 200)
        {
            this.editDescriptionError = "Description must be 200 characters or fewer.";
        }

        if (this.editAccountError is not null
            || this.editAmountError is not null
            || this.editClassificationError is not null
            || this.editDescriptionError is not null
            || this.editingTransactionId is not Guid transactionId
            || this.SelectedDate.Value is not DateOnly ledgerDate)
        {
            return;
        }

        this.isMutating = true;
        var cancellationToken = this.requestCancellation?.Token ?? this.lifetimeCancellation.Token;
        try
        {
            var result = await this.TransactionsApi.ReviseAsync(
                ledgerDate,
                transactionId,
                new UpdateExpenseTransactionRequest(selectedAccountId, amount, this.editClassification, NormalizeDescription(this.editDescriptionText)),
                cancellationToken);
            if (this.SelectedDate.Value != ledgerDate)
            {
                return;
            }

            if (result == TransactionMutationResult.Invalid)
            {
                this.mutationError = "The expense was not changed. Check the entered values.";
                return;
            }

            this.editingTransactionId = null;
            this.mutationStatus = result == TransactionMutationResult.NotFound
                ? "The expense no longer exists."
                : "Expense updated.";
            await this.LoadAsync(ledgerDate, cancellationToken);
            this.SelectedDate.NotifyTransactionsChanged(ledgerDate);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            this.mutationError = "The expense could not be changed. Try again.";
        }
        finally
        {
            if (this.SelectedDate.Value == ledgerDate)
            {
                this.isMutating = false;
            }
        }
    }

    private void BeginRemove(Guid transactionId)
    {
        this.editingTransactionId = null;
        this.removingTransactionId = transactionId;
        this.ClearMutationMessages();
    }

    private void CancelRemove()
    {
        this.removingTransactionId = null;
        this.ClearMutationMessages();
    }

    private async Task ConfirmRemoveAsync()
    {
        if (this.removingTransactionId is not Guid transactionId
            || this.SelectedDate.Value is not DateOnly ledgerDate)
        {
            return;
        }

        this.isMutating = true;
        this.mutationError = null;
        this.mutationStatus = null;
        var cancellationToken = this.requestCancellation?.Token ?? this.lifetimeCancellation.Token;
        try
        {
            var result = await this.TransactionsApi.RemoveAsync(
                ledgerDate,
                transactionId,
                cancellationToken);
            if (this.SelectedDate.Value != ledgerDate)
            {
                return;
            }

            this.removingTransactionId = null;
            this.mutationStatus = result == TransactionMutationResult.NotFound
                ? "The expense no longer exists."
                : "Expense removed.";
            await this.LoadAsync(ledgerDate, cancellationToken);
            this.SelectedDate.NotifyTransactionsChanged(ledgerDate);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            this.mutationError = "The expense could not be removed. Try again.";
        }
        finally
        {
            if (this.SelectedDate.Value == ledgerDate)
            {
                this.isMutating = false;
            }
        }
    }

    private void ResetMutationState()
    {
        this.editingTransactionId = null;
        this.removingTransactionId = null;
        this.editAccountId = string.Empty;
        this.editAmountText = string.Empty;
        this.editClassification = string.Empty;
        this.editDescriptionText = string.Empty;
        this.editDescriptionError = null;
        this.ClearMutationMessages();
    }

    private void ClearMutationMessages()
    {
        this.editAccountError = null;
        this.editAmountError = null;
        this.editClassificationError = null;
        this.editDescriptionError = null;
        this.mutationError = null;
        this.mutationStatus = null;
    }

    private void UpdateAmount(ChangeEventArgs args)
    {
        this.amountText = args.Value?.ToString() ?? string.Empty;
    }

    private void UpdateAccount(ChangeEventArgs args)
    {
        this.accountId = args.Value?.ToString() ?? string.Empty;
    }

    private void UpdateClassification(ChangeEventArgs args)
    {
        this.classification = args.Value?.ToString() ?? string.Empty;
    }

    private void UpdateDescription(ChangeEventArgs args)
    {
        this.descriptionText = args.Value?.ToString() ?? string.Empty;
        this.descriptionError = null;
    }

    private void UpdateEditAmount(ChangeEventArgs args)
    {
        this.editAmountText = args.Value?.ToString() ?? string.Empty;
    }

    private void UpdateEditAccount(ChangeEventArgs args)
    {
        this.editAccountId = args.Value?.ToString() ?? string.Empty;
    }

    private void UpdateEditClassification(ChangeEventArgs args)
    {
        this.editClassification = args.Value?.ToString() ?? string.Empty;
    }

    private void UpdateEditDescription(ChangeEventArgs args)
    {
        this.editDescriptionText = args.Value?.ToString() ?? string.Empty;
        this.editDescriptionError = null;
    }

    private void BeginReceiptEdit(IncomeReceiptResponse receipt)
    {
        this.removingIncomeReceiptId = null;
        this.editingReceiptId = receipt.Id;
        this.receiptEditDateText = receipt.PayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        this.receiptEditAmountText = MoneyInput.Format(receipt.NetIncome);
        this.receiptEditAllocations = receipt.Allocations.Select(allocation => new ReceiptAllocationInput
        {
            AccountId = allocation.AccountId.ToString(),
            AmountText = MoneyInput.Format(allocation.Amount),
        }).ToList();
        this.incomeMutationError = null;
        this.incomeMutationStatus = null;
    }

    private void CancelReceiptEdit()
    {
        this.editingReceiptId = null;
        this.receiptEditAllocations = [];
        this.incomeMutationError = null;
    }

    private void BeginRemoveIncome(Guid receiptId)
    {
        this.editingReceiptId = null;
        this.removingIncomeReceiptId = receiptId;
        this.incomeMutationError = null;
        this.incomeMutationStatus = null;
    }

    private void CancelRemoveIncome()
    {
        this.removingIncomeReceiptId = null;
        this.incomeMutationError = null;
    }

    private void UpdateReceiptDate(ChangeEventArgs args) =>
        this.receiptEditDateText = args.Value?.ToString() ?? string.Empty;

    private void UpdateReceiptAmount(ChangeEventArgs args) =>
        this.receiptEditAmountText = args.Value?.ToString() ?? string.Empty;

    private void AddReceiptAllocation() => this.receiptEditAllocations.Add(new());

    private void RemoveReceiptAllocation(ReceiptAllocationInput allocation)
    {
        if (this.receiptEditAllocations.Count > 1)
        {
            this.receiptEditAllocations.Remove(allocation);
        }
    }

    private async Task ReviseIncomeAsync()
    {
        this.incomeMutationError = null;
        this.incomeMutationStatus = null;
        if (this.editingReceiptId is not Guid receiptId
            || this.SelectedDate.Value is not DateOnly selectedDate)
        {
            return;
        }

        if (!DateOnly.TryParseExact(this.receiptEditDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var receivedDate)
            || receivedDate > DateOnly.FromDateTime(this.Clock.GetLocalNow().DateTime))
        {
            this.incomeMutationError = "Enter a valid actual received date that is not in the future.";
            return;
        }

        if (!MoneyInput.TryParse(this.receiptEditAmountText, out var amount) || amount <= 0)
        {
            this.incomeMutationError = "Enter a positive amount with at most two decimal places.";
            return;
        }

        var allocations = new List<IncomeAllocationRequest>();
        foreach (var allocation in this.receiptEditAllocations)
        {
            if (!Guid.TryParse(allocation.AccountId, out var accountId)
                || !this.accounts.Any(account => account.Id == accountId)
                || !MoneyInput.TryParse(allocation.AmountText, out var allocationAmount)
                || allocationAmount <= 0)
            {
                this.incomeMutationError = "Choose an existing account and a positive amount for every allocation.";
                return;
            }

            allocations.Add(new IncomeAllocationRequest(accountId, allocationAmount));
        }

        if (allocations.Count == 0
            || allocations.Select(allocation => allocation.AccountId).Distinct().Count() != allocations.Count
            || allocations.Sum(allocation => allocation.Amount) != amount)
        {
            this.incomeMutationError = "Use each account once and make account allocations total the receipt exactly.";
            return;
        }

        var originalDate = this.incomeReceipts.FirstOrDefault(receipt => receipt.Id == receiptId)?.PayDate ?? selectedDate;
        var cancellationToken = this.requestCancellation?.Token ?? this.lifetimeCancellation.Token;
        this.isMutating = true;
        try
        {
            var updatedReceipt = await this.IncomeReceiptsApi.ReviseAsync(
                receiptId,
                new ConfirmIncomeReceiptRequest(receivedDate, amount, allocations),
                cancellationToken);
            this.incomeReceipts = receivedDate == selectedDate
                ? [.. this.incomeReceipts.Where(receipt => receipt.Id != receiptId), updatedReceipt]
                : this.incomeReceipts.Where(receipt => receipt.Id != receiptId).ToArray();
            this.RefreshPendingIncome(selectedDate);
            this.editingReceiptId = null;
            this.receiptEditAllocations = [];
            this.incomeMutationStatus = originalDate == receivedDate
                ? "Confirmed income receipt corrected."
                : $"Receipt date changed from {originalDate.ToString("D", CultureInfo.CurrentCulture)} to {receivedDate.ToString("D", CultureInfo.CurrentCulture)}. Calendar and monthly actuals were refreshed.";
            this.SelectedDate.NotifyIncomeReceiptsChanged(originalDate);
            if (receivedDate != originalDate)
            {
                this.SelectedDate.NotifyIncomeReceiptsChanged(receivedDate);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            this.incomeMutationError = "The confirmed income receipt could not be corrected. Try again.";
        }
        finally
        {
            this.isMutating = false;
        }
    }

    private async Task ConfirmRemoveIncomeAsync()
    {
        if (this.removingIncomeReceiptId is not Guid receiptId
            || this.SelectedDate.Value is not DateOnly selectedDate)
        {
            return;
        }

        this.isMutating = true;
        this.incomeMutationError = null;
        this.incomeMutationStatus = null;
        var cancellationToken = this.requestCancellation?.Token ?? this.lifetimeCancellation.Token;
        try
        {
            var result = await this.IncomeReceiptsApi.RemoveAsync(receiptId, cancellationToken);
            this.incomeReceipts = this.incomeReceipts.Where(receipt => receipt.Id != receiptId).ToArray();
            this.RefreshPendingIncome(selectedDate);
            this.removingIncomeReceiptId = null;
            this.incomeMutationStatus = result == TransactionMutationResult.NotFound
                ? "The confirmed income receipt no longer exists."
                : "Confirmed income receipt removed.";
            this.SelectedDate.NotifyIncomeReceiptsChanged(selectedDate);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            this.incomeMutationError = "The confirmed income receipt could not be removed. Try again.";
        }
        finally
        {
            this.isMutating = false;
        }
    }

    private void ResetIncomeMutationState()
    {
        this.editingReceiptId = null;
        this.removingIncomeReceiptId = null;
        this.receiptEditAllocations = [];
        this.incomeMutationError = null;
        this.incomeMutationStatus = null;
    }

    private sealed class ReceiptAllocationInput
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string AccountId { get; set; } = string.Empty;

        public string AmountText { get; set; } = string.Empty;
    }
}
