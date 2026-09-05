// <copyright file="TransactionInspector.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Components;

using System.Globalization;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Presents selected-day transactions and the first expense form.
/// </summary>
public partial class TransactionInspector : ComponentBase, IDisposable
{
    private static readonly string[] Classifications = ["Necessities", "Optional", "Culture", "Unexpected"];
    private static readonly CultureInfo UsdCulture = CultureInfo.GetCultureInfo("en-US");
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private CancellationTokenSource? requestCancellation;
    private string amountText = string.Empty;
    private string classification = string.Empty;
    private string? amountError;
    private string? classificationError;
    private string? saveError;
    private Guid? editingTransactionId;
    private Guid? removingTransactionId;
    private string editAmountText = string.Empty;
    private string editClassification = string.Empty;
    private string? editAmountError;
    private string? editClassificationError;
    private string? mutationError;
    private string? mutationStatus;
    private bool isLoading;
    private bool isMutating;
    private bool isSaving;
    private bool loadError;
    private IReadOnlyList<ExpenseTransactionResponse> transactions = [];

    /// <summary>Gets or sets the selected-date state.</summary>
    [Inject]
    private SelectedDateState SelectedDate { get; set; } = null!;

    /// <summary>Gets or sets the transaction API client.</summary>
    [Inject]
    private ITransactionsApiClient TransactionsApi { get; set; } = null!;

    private string HasAmountError => this.amountError is null ? "false" : "true";

    private string HasClassificationError => this.classificationError is null ? "false" : "true";

    /// <inheritdoc/>
    public void Dispose()
    {
        this.SelectedDate.Changed -= this.OnSelectedDateChanged;
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
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && this.SelectedDate.Value is DateOnly ledgerDate)
        {
            this.requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.lifetimeCancellation.Token);
            await this.LoadAsync(ledgerDate, this.requestCancellation.Token);
        }
    }

    private static string GetElementId(string prefix, Guid transactionId) => $"{prefix}-{transactionId:N}";

    private void OnSelectedDateChanged(DateOnly ledgerDate)
    {
        this.requestCancellation?.Cancel();
        this.requestCancellation?.Dispose();
        this.requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.lifetimeCancellation.Token);
        this.transactions = [];
        this.loadError = false;
        this.saveError = null;
        this.isSaving = false;
        this.isMutating = false;
        this.ResetMutationState();
        _ = this.LoadAsync(ledgerDate, this.requestCancellation.Token);
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
        this.amountError = null;
        this.classificationError = null;
        this.saveError = null;

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

        if (this.amountError is not null || this.classificationError is not null || this.SelectedDate.Value is not DateOnly ledgerDate)
        {
            return;
        }

        this.isSaving = true;
        var cancellationToken = this.requestCancellation?.Token ?? this.lifetimeCancellation.Token;
        try
        {
            var saved = await this.TransactionsApi.CreateAsync(
                ledgerDate,
                new CreateExpenseTransactionRequest(amount, this.classification),
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
        this.editAmountText = transaction.Amount.ToString("0.00", CultureInfo.CurrentCulture);
        this.editClassification = transaction.Classification;
        this.ClearMutationMessages();
    }

    private void CancelEdit()
    {
        this.editingTransactionId = null;
        this.ClearMutationMessages();
    }

    private async Task ReviseAsync()
    {
        this.editAmountError = null;
        this.editClassificationError = null;
        this.mutationError = null;
        this.mutationStatus = null;

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

        if (this.editAmountError is not null
            || this.editClassificationError is not null
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
                new UpdateExpenseTransactionRequest(amount, this.editClassification),
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
        this.editAmountText = string.Empty;
        this.editClassification = string.Empty;
        this.ClearMutationMessages();
    }

    private void ClearMutationMessages()
    {
        this.editAmountError = null;
        this.editClassificationError = null;
        this.mutationError = null;
        this.mutationStatus = null;
    }

    private void UpdateAmount(ChangeEventArgs args)
    {
        this.amountText = args.Value?.ToString() ?? string.Empty;
    }

    private void UpdateClassification(ChangeEventArgs args)
    {
        this.classification = args.Value?.ToString() ?? string.Empty;
    }

    private void UpdateEditAmount(ChangeEventArgs args)
    {
        this.editAmountText = args.Value?.ToString() ?? string.Empty;
    }

    private void UpdateEditClassification(ChangeEventArgs args)
    {
        this.editClassification = args.Value?.ToString() ?? string.Empty;
    }
}
