// <copyright file="MonthlyBudgetPage.razor.cs" company="HouseholdLedger">
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

/// <summary>Edits monthly intention and displays authoritative actuals.</summary>
public partial class MonthlyBudgetPage : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private readonly PlanField[] fields =
    [
        new("expected-income", "Expected take-home income"),
        new("intended-savings", "Intended savings"),
        new("necessities", "Necessities"),
        new("optional", "Optional"),
        new("culture", "Culture"),
        new("unexpected", "Unexpected"),
    ];

    private MonthlyBudgetPlanResponse? plan;
    private MonthlyBudgetReviewResponse? review;
    private MonthlyReflectionResponse? reflection;
    private int requestVersion;
    private int reviewRequestVersion;
    private bool isLoading;
    private bool planLoadError;
    private bool reviewLoadError;
    private bool isSaving;
    private bool isPlanDirty;
    private bool isReflectionDirty;
    private bool reflectionIsLoading;
    private bool reflectionLoadError;
    private bool isSavingReflection;
    private string? saveError;
    private string? saveStatus;
    private string whatWorked = string.Empty;
    private string nextMonthIntention = string.Empty;
    private string? reflectionSaveError;
    private string? reflectionSaveStatus;
    private int reflectionRequestVersion;

    /// <summary>Gets or sets the calendar year.</summary>
    [Parameter]
    public int Year { get; set; }

    /// <summary>Gets or sets the calendar month.</summary>
    [Parameter]
    public int Month { get; set; }

    [Inject]
    private IMonthlyBudgetApiClient BudgetApi { get; set; } = null!;

    [Inject]
    private IMonthlyReflectionApiClient ReflectionApi { get; set; } = null!;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = null!;

    [Inject]
    private SelectedDateState SelectedDate { get; set; } = null!;

    [CascadingParameter]
    private GlobalSettingsState? DisplayCurrency { get; set; }

    private bool IsValidMonth => this.Year is >= 1 and <= 9999 && this.Month is >= 1 and <= 12;

    private DateOnly MonthDate => new(this.Year, this.Month, 1);

    private string MonthLabel => this.MonthDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

    private string PreviousMonthLink => this.MonthDate == DateOnly.MinValue ? "/" : GetMonthLink(this.MonthDate.AddMonths(-1));

    private string NextMonthLink => this.Year == 9999 && this.Month == 12 ? "/" : GetMonthLink(this.MonthDate.AddMonths(1));

    private string CurrencyCode => this.DisplayCurrency?.CurrentCode ?? MoneyFormatter.DefaultCurrencyCode;

    private string CurrencyLabel => MoneyFormatter.GetDisplayLabel(this.CurrencyCode);

    private bool HasUnsavedChanges => this.isPlanDirty || this.isReflectionDirty;

    private decimal? Remaining => this.TryGetAmounts(out var amounts) ? amounts[0] - amounts.Skip(1).Sum() : null;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.SelectedDate.TransactionsChanged -= this.OnTransactionsChanged;
        this.SelectedDate.IncomeReceiptsChanged -= this.OnTransactionsChanged;
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
        this.SelectedDate.TransactionsChanged += this.OnTransactionsChanged;
        this.SelectedDate.IncomeReceiptsChanged += this.OnTransactionsChanged;
        if (this.DisplayCurrency is not null)
        {
            this.DisplayCurrency.Changed += this.RefreshCurrency;
        }
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync() => await this.LoadAsync();

    private static string? NormalizeReflection(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static string GetMonthLink(DateOnly date) => $"/months/{date.Year}/{date.Month}";

    private string FormatCurrency(decimal amount) => MoneyFormatter.Format(amount, this.CurrencyCode);

    private string FormatPlanned(decimal? amount) => this.review?.LastRevisedAt is null || amount is null ? "No saved plan" : this.FormatCurrency(amount.Value);

    private void RefreshCurrency() => _ = this.InvokeAsync(this.StateHasChanged);

    private void OnTransactionsChanged(DateOnly date)
    {
        if (date.Year == this.Year && date.Month == this.Month)
        {
            _ = this.InvokeAsync(async () =>
            {
                await this.RefreshReviewAsync();
                this.StateHasChanged();
            });
        }
    }

    private bool TryGetAmounts(out decimal[] amounts)
    {
        amounts = new decimal[this.fields.Length];
        for (var index = 0; index < this.fields.Length; index++)
        {
            if (!MoneyInput.TryParse(this.fields[index].Text, out amounts[index]))
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateField(PlanField field, ChangeEventArgs args)
    {
        field.Text = args.Value?.ToString() ?? string.Empty;
        this.isPlanDirty = true;
        this.saveError = null;
        this.saveStatus = null;
    }

    private async Task LoadAsync()
    {
        var version = ++this.requestVersion;
        this.plan = null;
        this.review = null;
        this.reflection = null;
        this.saveError = null;
        this.saveStatus = null;
        this.isPlanDirty = false;
        this.isReflectionDirty = false;
        this.reflectionSaveError = null;
        this.reflectionSaveStatus = null;
        this.whatWorked = string.Empty;
        this.nextMonthIntention = string.Empty;
        this.planLoadError = false;
        this.reviewLoadError = false;
        foreach (var field in this.fields)
        {
            field.Text = "0.00";
        }

        if (!this.IsValidMonth)
        {
            return;
        }

        this.isLoading = true;
        await Task.WhenAll(this.LoadPlanAsync(version, this.Year, this.Month), this.LoadReviewAsync(version, this.Year, this.Month), this.LoadReflectionAsync());
        if (version == this.requestVersion)
        {
            this.isLoading = false;
        }
    }

    private async Task LoadPlanAsync(int version, int year, int month)
    {
        try
        {
            var loaded = await this.BudgetApi.GetPlanAsync(year, month, this.lifetimeCancellation.Token);
            if (version != this.requestVersion || this.lifetimeCancellation.IsCancellationRequested)
            {
                return;
            }

            this.plan = loaded;
            this.isPlanDirty = false;
            if (loaded is not null)
            {
                decimal[] values = [loaded.ExpectedIncome, loaded.IntendedSavings, loaded.Necessities, loaded.Optional, loaded.Culture, loaded.Unexpected];
                for (var index = 0; index < values.Length; index++)
                {
                    this.fields[index].Text = MoneyInput.Format(values[index]);
                }
            }
        }
        catch (OperationCanceledException) when (this.lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException)
        {
            if (version == this.requestVersion)
            {
                this.planLoadError = true;
            }
        }
    }

    private async Task LoadReviewAsync(int version, int year, int month)
    {
        var reviewVersion = ++this.reviewRequestVersion;
        try
        {
            var loaded = await this.BudgetApi.GetReviewAsync(year, month, this.lifetimeCancellation.Token);
            if (version == this.requestVersion && reviewVersion == this.reviewRequestVersion && !this.lifetimeCancellation.IsCancellationRequested)
            {
                this.review = loaded;
                this.reviewLoadError = false;
            }
        }
        catch (OperationCanceledException) when (this.lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException)
        {
            if (version == this.requestVersion && reviewVersion == this.reviewRequestVersion)
            {
                this.reviewLoadError = true;
            }
        }
    }

    private async Task RefreshReviewAsync() => await this.LoadReviewAsync(this.requestVersion, this.Year, this.Month);

    private async Task SaveAsync()
    {
        if (this.isSaving)
        {
            return;
        }

        if (!this.TryGetAmounts(out var values) || this.Remaining != 0)
        {
            this.saveError = "Enter valid amounts and reconcile the plan exactly before saving.";
            return;
        }

        var version = this.requestVersion;
        var year = this.Year;
        var month = this.Month;
        this.isSaving = true;
        this.saveError = null;
        this.saveStatus = null;
        try
        {
            var saved = await this.BudgetApi.SavePlanAsync(year, month, new MonthlyBudgetPlanRequest(values[0], values[1], values[2], values[3], values[4], values[5]), this.lifetimeCancellation.Token);
            if (version == this.requestVersion && !this.lifetimeCancellation.IsCancellationRequested)
            {
                this.plan = saved;
                this.isPlanDirty = false;
                this.saveStatus = "Monthly plan saved. Recorded income and expenses have not changed.";
                await this.LoadReviewAsync(version, year, month);
            }
        }
        catch (OperationCanceledException) when (this.lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            if (version == this.requestVersion)
            {
                this.saveError = exception is HttpRequestException { StatusCode: HttpStatusCode.BadRequest }
                    ? "The server rejected this plan. Check the amounts and exact reconciliation. Your entries are preserved."
                    : "The monthly plan could not be saved. Your entries are preserved; try again.";
            }
        }
        catch (JsonException)
        {
            if (version == this.requestVersion)
            {
                this.saveError = "The save response could not be read. Your entries are preserved. Reload to check whether the plan was saved.";
            }
        }
        finally
        {
            this.isSaving = false;
        }
    }

    private async Task LoadReflectionAsync()
    {
        var version = ++this.reflectionRequestVersion;
        this.reflectionIsLoading = true;
        this.reflectionLoadError = false;
        try
        {
            var loaded = await this.ReflectionApi.GetAsync(this.Year, this.Month, this.lifetimeCancellation.Token);
            if (version != this.reflectionRequestVersion || this.lifetimeCancellation.IsCancellationRequested)
            {
                return;
            }

            this.reflection = loaded;
            this.whatWorked = loaded?.WhatWorked ?? string.Empty;
            this.nextMonthIntention = loaded?.NextMonthIntention ?? string.Empty;
            this.isReflectionDirty = false;
        }
        catch (OperationCanceledException) when (this.lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException)
        {
            if (version == this.reflectionRequestVersion)
            {
                this.reflectionLoadError = true;
            }
        }
        finally
        {
            if (version == this.reflectionRequestVersion && !this.lifetimeCancellation.IsCancellationRequested)
            {
                this.reflectionIsLoading = false;
            }
        }
    }

    private async Task ConfirmUnsavedNavigationAsync(LocationChangingContext context)
    {
        if (this.HasUnsavedChanges
            && !await this.JavaScript.InvokeAsync<bool>("confirm", "Leave this page and discard unsaved monthly plan or reflection changes?"))
        {
            context.PreventNavigation();
        }
    }

    private void UpdateWhatWorked(ChangeEventArgs args)
    {
        this.whatWorked = args.Value?.ToString() ?? string.Empty;
        this.isReflectionDirty = true;
        this.reflectionSaveError = null;
        this.reflectionSaveStatus = null;
    }

    private void UpdateNextMonthIntention(ChangeEventArgs args)
    {
        this.nextMonthIntention = args.Value?.ToString() ?? string.Empty;
        this.isReflectionDirty = true;
        this.reflectionSaveError = null;
        this.reflectionSaveStatus = null;
    }

    private async Task SaveReflectionAsync()
    {
        if (this.isSavingReflection)
        {
            return;
        }

        if (this.whatWorked.Length > 1000 || this.nextMonthIntention.Length > 1000)
        {
            this.reflectionSaveError = "Each reflection answer must be 1,000 characters or fewer.";
            return;
        }

        var version = this.reflectionRequestVersion;
        this.isSavingReflection = true;
        this.reflectionSaveError = null;
        this.reflectionSaveStatus = null;
        try
        {
            var request = new MonthlyReflectionRequest(
                NormalizeReflection(this.whatWorked),
                NormalizeReflection(this.nextMonthIntention));
            var saved = await this.ReflectionApi.SaveAsync(this.Year, this.Month, request, this.lifetimeCancellation.Token);
            if (version == this.reflectionRequestVersion && !this.lifetimeCancellation.IsCancellationRequested)
            {
                this.reflection = saved;
                this.whatWorked = saved.WhatWorked ?? string.Empty;
                this.nextMonthIntention = saved.NextMonthIntention ?? string.Empty;
                this.isReflectionDirty = false;
                this.reflectionSaveStatus = "Monthly reflection saved separately from the spending plan.";
            }
        }
        catch (OperationCanceledException) when (this.lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException)
        {
            if (version == this.reflectionRequestVersion)
            {
                this.reflectionSaveError = "The monthly reflection could not be saved. Your entries are preserved; try again.";
            }
        }
        finally
        {
            this.isSavingReflection = false;
        }
    }

    private sealed class PlanField(string id, string label)
    {
        public string Id { get; } = id;

        public string Label { get; } = label;

        public string Text { get; set; } = "0.00";
    }
}
