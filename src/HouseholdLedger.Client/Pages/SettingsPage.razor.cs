// <copyright file="SettingsPage.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Pages;

using System.Text.Json;
using HouseholdLedger.Client.Formatting;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;

/// <summary>Allows the global display currency to be configured.</summary>
public partial class SettingsPage : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private string selectedCurrency = "USD";
    private string? saveError;
    private string? saveStatus;
    private bool hasUnsavedSelection;
    private bool isSaving;
    private string selectedTheme = ThemeCatalog.DefaultTheme;
    private string? themeSaveError;
    private string? themeSaveStatus;
    private bool hasUnsavedThemeSelection;
    private bool isSavingTheme;

    /// <summary>Gets or sets the global display-currency state.</summary>
    [Inject]
    public GlobalSettingsState CurrencyState { get; set; } = null!;

    private bool IsDirty => !string.Equals(
        this.selectedCurrency,
        this.CurrencyState.CurrentCode,
        StringComparison.Ordinal);

    private bool IsThemeDirty => !string.Equals(
        this.selectedTheme,
        this.CurrencyState.CurrentTheme,
        StringComparison.Ordinal);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.CurrencyState.Changed -= this.OnCurrencyStateChanged;
        this.lifetimeCancellation.Cancel();
        this.lifetimeCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        this.selectedCurrency = this.CurrencyState.CurrentCode;
        this.selectedTheme = this.CurrencyState.CurrentTheme;
        this.CurrencyState.Changed += this.OnCurrencyStateChanged;
        await this.CurrencyState.EnsureLoadedAsync(this.lifetimeCancellation.Token);
        this.selectedCurrency = this.CurrencyState.CurrentCode;
        this.selectedTheme = this.CurrencyState.CurrentTheme;
    }

    private static bool IsApiFailure(Exception exception) =>
        exception is HttpRequestException or JsonException or NotSupportedException
        || exception is OperationCanceledException;

    private void ChangeCurrency(ChangeEventArgs eventArgs)
    {
        this.selectedCurrency = eventArgs.Value?.ToString() ?? string.Empty;
        this.hasUnsavedSelection = true;
        this.saveError = null;
        this.saveStatus = null;
    }

    private void ChangeTheme(ChangeEventArgs eventArgs)
    {
        this.selectedTheme = eventArgs.Value?.ToString() ?? string.Empty;
        this.hasUnsavedThemeSelection = true;
        this.themeSaveError = null;
        this.themeSaveStatus = null;
    }

    private void OnCurrencyStateChanged()
    {
        if (!this.hasUnsavedSelection && this.CurrencyState.IsLoaded)
        {
            this.selectedCurrency = this.CurrencyState.CurrentCode;
        }

        if (!this.hasUnsavedThemeSelection && this.CurrencyState.IsLoaded)
        {
            this.selectedTheme = this.CurrencyState.CurrentTheme;
        }

        _ = this.InvokeAsync(this.StateHasChanged);
    }

    private async Task RetryAsync()
    {
        await this.CurrencyState.ReloadAsync(this.lifetimeCancellation.Token);
        if (this.CurrencyState.IsLoaded)
        {
            this.selectedCurrency = this.CurrencyState.CurrentCode;
            this.selectedTheme = this.CurrencyState.CurrentTheme;
            this.hasUnsavedSelection = false;
            this.hasUnsavedThemeSelection = false;
        }
    }

    private async Task SaveAsync()
    {
        if (!this.IsDirty || this.isSaving)
        {
            return;
        }

        this.isSaving = true;
        this.saveError = null;
        this.saveStatus = null;

        try
        {
            this.selectedCurrency = await this.CurrencyState.SaveCurrencyAsync(
                this.selectedCurrency,
                this.lifetimeCancellation.Token);
            this.hasUnsavedSelection = false;
            this.saveStatus = $"Display currency saved as {MoneyFormatter.GetDisplayName(this.selectedCurrency)}.";
        }
        catch (Exception exception) when (IsApiFailure(exception))
        {
            this.saveError = "The display currency could not be saved. Try again.";
        }
        finally
        {
            this.isSaving = false;
        }
    }

    private async Task SaveThemeAsync()
    {
        if (!this.IsThemeDirty || this.isSavingTheme)
        {
            return;
        }

        this.isSavingTheme = true;
        this.themeSaveError = null;
        this.themeSaveStatus = null;

        try
        {
            this.selectedTheme = await this.CurrencyState.SaveThemeAsync(
                this.selectedTheme,
                this.lifetimeCancellation.Token);
            this.hasUnsavedThemeSelection = false;
            this.themeSaveStatus = $"Theme saved as {ThemeCatalog.GetDisplayName(this.selectedTheme)}.";
        }
        catch (Exception exception) when (IsApiFailure(exception))
        {
            this.themeSaveError = "The theme could not be saved. Try again.";
        }
        finally
        {
            this.isSavingTheme = false;
        }
    }
}
