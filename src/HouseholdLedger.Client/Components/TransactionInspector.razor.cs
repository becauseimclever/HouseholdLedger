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
    private bool isLoading;
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

    private void OnSelectedDateChanged(DateOnly ledgerDate)
    {
        this.requestCancellation?.Cancel();
        this.requestCancellation?.Dispose();
        this.requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.lifetimeCancellation.Token);
        this.transactions = [];
        this.loadError = false;
        this.saveError = null;
        _ = this.LoadAsync(ledgerDate, this.requestCancellation.Token);
    }

    private async Task LoadAsync(DateOnly ledgerDate, CancellationToken cancellationToken)
    {
        this.isLoading = true;
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
        try
        {
            var saved = await this.TransactionsApi.CreateAsync(
                ledgerDate,
                new CreateExpenseTransactionRequest(amount, this.classification),
                this.lifetimeCancellation.Token);
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
            await this.LoadAsync(ledgerDate, this.lifetimeCancellation.Token);
        }
        catch (HttpRequestException)
        {
            this.saveError = "The expense could not be saved. Try again.";
        }
        finally
        {
            this.isSaving = false;
        }
    }

    private void UpdateAmount(ChangeEventArgs args)
    {
        this.amountText = args.Value?.ToString() ?? string.Empty;
    }

    private void UpdateClassification(ChangeEventArgs args)
    {
        this.classification = args.Value?.ToString() ?? string.Empty;
    }
}
